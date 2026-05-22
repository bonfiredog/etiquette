using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityTracery;
using TMPro;
using UnityEditor;
using Leguar.TotalJSON;
using System;

public class textGenerationControl : MonoBehaviour
{
    public string startingGrammarName;
    public bool IsTimedText;
    public float timedTextTimerTotal;
    public string[] timedTextGrammars;
    public bool generated;
    public bool oneShotText;

    [HideInInspector] public GameObject gc;
    [HideInInspector] public traceGrammarControl gcScript;
    [HideInInspector] public TextMeshPro myText;
    [HideInInspector] public TextAsset currentGrammarJSON;
    [HideInInspector] public TraceryGrammar currentGrammar;

    private float timedTextTimer;
    private int currentTimedGrammar = 0;
    private string wordListToParse;
    public string grammarParse;

    private StationScheduler ss;
    private timeControl tc;

    // Shared cache across all instances — grammar only needs to be built once per name.
    private static Dictionary<string, TraceryGrammar> grammarCache = new Dictionary<string, TraceryGrammar>();


    void Start()
    {
        if (generated == false)
        {
            gc = GameObject.Find("grammarController");
            gcScript = gc.GetComponent<traceGrammarControl>();
            myText = gameObject.GetComponent<TextMeshPro>();
            ss = GameObject.Find("stationScheduleController").GetComponent<StationScheduler>();

            if (gameObject.tag != "centralarch")
                setGrammarForObject(startingGrammarName);

            if (IsTimedText == true)
                timedTextTimer = timedTextTimerTotal;
        }

        if (oneShotText == true)
        {
            setGrammarForObject(startingGrammarName);
            generateTextFromGrammar(myText);
        }
    }


    void Update()
    {
        if (IsTimedText == true)
        {
            if (currentTimedGrammar < timedTextGrammars.Length - 1)
            {
                if (timedTextTimer > 0)
                {
                    timedTextTimer -= 1;
                }
                else
                {
                    currentTimedGrammar += 1;
                    setGrammarForObject(timedTextGrammars[currentTimedGrammar]);
                    generateTextFromGrammar(myText);
                    timedTextTimer = timedTextTimerTotal;
                }
            }
        }
    }


    // Use this to initially set or change the current grammar.
    public void setGrammarForObject(string grammarName, traceGrammarControl cachedGcScript = null)
    {
        // Use passed-in reference if available, avoiding a Find() call.
        if (cachedGcScript != null)
            gcScript = cachedGcScript;
        else if (gcScript == null)
        {
            gc = GameObject.Find("grammarController");
            gcScript = gc.GetComponent<traceGrammarControl>();
        }

        // Return cached grammar if we've already built this one.
        if (grammarCache.TryGetValue(grammarName, out var cached))
        {
            currentGrammar = cached;
            return;
        }

        var grammarFile = gcScript.FindJsonFileByName(gcScript.GrammarFiles, grammarName);

        if (grammarFile == null)
        {
            Debug.LogWarning($"[textGenerationControl] Could not find grammar file: '{grammarName}'");
            return;
        }

        wordListToParse = removeCurlyBraces(gcScript.wordListString);
        var grammarToParse = removeCurlyBraces(grammarFile.text);
        var finalGrammarString = buildGrammarString(wordListToParse, grammarToParse);

        if (string.IsNullOrEmpty(finalGrammarString))
        {
            Debug.LogWarning($"[textGenerationControl] Grammar string was empty for: '{grammarName}'");
            return;
        }

        try
        {
            currentGrammar = new TraceryGrammar(finalGrammarString);
            grammarCache[grammarName] = currentGrammar;
        }
        catch (Exception e)
        {
            Debug.LogError($"[textGenerationControl] Failed to parse grammar '{grammarName}': {e.Message}");
            Debug.LogError($"Offending string: {finalGrammarString}");
        }
    }


    public void generateTextFromGrammar(TextMeshPro myText, StationScheduler cachedSs = null)
    {
        // Use passed-in reference if available, avoiding a Find() call.
        if (cachedSs != null)
            ss = cachedSs;
        else if (ss == null)
            ss = GameObject.Find("stationScheduleController").GetComponent<StationScheduler>();

        if (ss == null)
        {
            Debug.LogWarning("[textGenerationControl] StationScheduler not found.");
            return;
        }

        if (currentGrammar == null)
        {
            Debug.LogWarning("[textGenerationControl] currentGrammar is null — was setGrammarForObject called?");
            return;
        }

        switch (startingGrammarName)
        {
            case "tracknamegrammar":
            case "trackgrammar":
                grammarParse = "#origin#";
                break;

            case "backgrammar":
            case "middlegrammar":
            case "closegrammar":
                var urbanDensity = Math.Clamp(ss.currentUrbanDensity, 1, 100);
                int urbanCount = (int)Math.Round((10.0 / 100) * urbanDensity);
                int ruralCount = 10 - urbanCount;

                var parts = new List<string>();
                for (int x = 0; x < urbanCount; x++) parts.Add("\"#urbanlines#\"");
                for (int x = 0; x < ruralCount; x++) parts.Add("\"#rurallines#\"");
                string ruralUrbanOrigin = "[" + string.Join(", ", parts) + "]";

                grammarParse = buildContextPush(true, ruralUrbanOrigin);
                break;

            default:
                grammarParse = buildContextPush(false, null);
                break;
        }

        myText.text = currentGrammar.Parse(grammarParse);
    }


    // Builds the Tracery context-push string, shared between the default and rural/urban cases.
    private string buildContextPush(bool includeRuralUrban, string ruralUrbanOrigin)
    {
        var result =
              "[current_person_type:"    + currentGrammar.Parse(ss.appropriateperson)       + "]"
            + "[current_locations:"      + currentGrammar.Parse(ss.appropriatelocs)          + "]"
            + "[current_timeofday:"      + currentGrammar.Parse(ss.currenttod)               + "]"
            + "[current_season:"         + currentGrammar.Parse(ss.currentseason)            + "]"
            + "[current_month:"          + currentGrammar.Parse(ss.currentmonth)             + "]"
            + "[current_mealtime:"       + currentGrammar.Parse(ss.currentmealtime)          + "]"
            + "[current_terrain:"        + currentGrammar.Parse(ss.currentterrain)           + "]"
            + "[current_buildings:"      + currentGrammar.Parse(ss.appropriatebuildings)     + "]"
            + "[current_buildings_rare:" + currentGrammar.Parse(ss.appropriatebuildingsrare) + "]"
            + "[current_weather_types:"  + currentGrammar.Parse(ss.currentweather)           + "]"
            + "[current_station:"        + currentGrammar.Parse(ss.nextStationName)          + "]"
             + "[current_vehicles:" + currentGrammar.Parse(ss.current_vehicles) + "]"
            + "[current_vehicles_rare:" + currentGrammar.Parse(ss.current_vehicles_rare) + "]"
            + "[current_county:"         + currentGrammar.Parse(ss.currentCounty)            + "]"
            + "[current_time_discrepancy:" + currentGrammar.Parse(ss.timediff.ToString())    + "]";

        if (includeRuralUrban && ruralUrbanOrigin != null)
            result += "[origin:" + ruralUrbanOrigin + "]";

        result += "#origin#";
        return result;
    }


    // Safely removes the outer curly braces from a JSON string.
    public string removeCurlyBraces(string originalString)
    {
        if (originalString != null && originalString.Length >= 2)
            return originalString.Substring(1, originalString.Length - 2);
        return "";
    }


    // Builds a valid JSON grammar string, gracefully handling empty word lists or grammar bodies.
    private string buildGrammarString(string wordList, string grammar)
    {
        bool hasWordList = !string.IsNullOrEmpty(wordList);
        bool hasGrammar  = !string.IsNullOrEmpty(grammar);

        if (hasWordList && hasGrammar)  return "{" + wordList + ", " + grammar + "}";
        else if (hasWordList)           return "{" + wordList + "}";
        else if (hasGrammar)            return "{" + grammar + "}";
        else                            return "";
    }
}