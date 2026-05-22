using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityTracery;
using TMPro;
using Leguar.TotalJSON;
using System;

public class textGenOneShot : MonoBehaviour
{
    public string startingGrammarName;
    public bool generated;

    [HideInInspector] public GameObject gc;
    [HideInInspector] public traceGrammarControl gcScript;
    [HideInInspector] public TextMeshPro myText;
    [HideInInspector] public TextAsset currentGrammarJSON;
    [HideInInspector] public TraceryGrammar currentGrammar;

    private string wordListToParse;
    public string grammarParse;
    public StationScheduler ss;
    private TextFitController tfc;
    private string savedText;
    public int length;
    private string yardlength;

    // Shared cache across all instances — grammar only needs to be built once per name.
    private static Dictionary<string, TraceryGrammar> grammarCache = new Dictionary<string, TraceryGrammar>();


    void Start()
    {
        gc = GameObject.Find("grammarController");
        gcScript = gc.GetComponent<traceGrammarControl>();
        myText = gameObject.GetComponent<TextMeshPro>();
        ss = GameObject.Find("stationScheduleController").GetComponent<StationScheduler>();

        if (GetComponent<TextFitController>() != null)
            tfc = GetComponent<TextFitController>();

        GenerateText();
    }


    private void GenerateText()
    {
        SetGrammarForObject(startingGrammarName);

        if (startingGrammarName == "tunnelnamegrammar")
        {
            length = 10 * length;
            yardlength = length.ToString();
        }

        savedText = GenerateTextFromGrammar(myText);

        if (tfc != null)
            tfc.SetText(savedText);

        generated = true;
    }


    public void SetGrammarForObject(string grammarName, traceGrammarControl cachedGcScript = null)
    {
        // Use passed-in reference if available, avoiding a Find() call.
        if (cachedGcScript != null)
            gcScript = cachedGcScript;
        else if (gcScript == null)
        {
            gc = GameObject.Find("grammarController");
            gcScript = gc.GetComponent<traceGrammarControl>();
        }

        if (ss == null)
            ss = GameObject.Find("stationScheduleController").GetComponent<StationScheduler>();

        // Return cached grammar if we've already built this one.
        if (grammarCache.TryGetValue(grammarName, out var cached))
        {
            currentGrammar = cached;
            return;
        }

        var grammarFile = gcScript.FindJsonFileByName(gcScript.GrammarFiles, grammarName);

        if (grammarFile == null)
        {
            Debug.LogWarning($"[textGenOneShot] Could not find grammar file: '{grammarName}'");
            return;
        }

        wordListToParse = RemoveCurlyBraces(gcScript.wordListString);
        var grammarToParse = RemoveCurlyBraces(grammarFile.text);
        var finalGrammarString = BuildGrammarString(wordListToParse, grammarToParse);

        if (string.IsNullOrEmpty(finalGrammarString))
        {
            Debug.LogWarning($"[textGenOneShot] Grammar string was empty for: '{grammarName}'");
            return;
        }

        try
        {
            currentGrammar = new TraceryGrammar(finalGrammarString);
            grammarCache[grammarName] = currentGrammar;
        }
        catch (Exception e)
        {
            Debug.LogError($"[textGenOneShot] Failed to parse grammar '{grammarName}': {e.Message}");
            Debug.LogError($"Offending string: {finalGrammarString}");
        }
    }


    public string GenerateTextFromGrammar(TextMeshPro targetText, StationScheduler cachedSs = null)
    {
        // Use passed-in reference if available, avoiding a Find() call.
        if (cachedSs != null)
            ss = cachedSs;
        else if (ss == null)
            ss = GameObject.Find("stationScheduleController").GetComponent<StationScheduler>();

        if (ss == null)
        {
            Debug.LogWarning("[textGenOneShot] StationScheduler not found.");
            return "";
        }

        if (currentGrammar == null)
        {
            Debug.LogWarning("[textGenOneShot] currentGrammar is null — was SetGrammarForObject called?");
            return "";
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

                grammarParse = BuildContextPush(false, null)
                             + "[origin:" + ruralUrbanOrigin + "]"
                             + "#origin#";
                break;

            case "tunnelnamegrammar":
                grammarParse = BuildContextPush(false, null, includeTunnel: true)
                             + "#origin#";
                break;

            default:
                grammarParse = BuildContextPush(false, null) + "#origin#";
                break;
        }

        targetText.text = currentGrammar.Parse(grammarParse);
        return targetText.text;
    }


    // Builds the shared Tracery context-push string.
    private string BuildContextPush(bool includeRuralUrban, string ruralUrbanOrigin, bool includeTunnel = false)
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
            + "[current_county:"         + currentGrammar.Parse(ss.currentCounty)            + "]";

        if (includeTunnel)
            result += "[tunnel_length:" + currentGrammar.Parse(yardlength) + "]";
        else
            result += "[current_time_discrepancy:" + currentGrammar.Parse(ss.timediff.ToString()) + "]";

        if (includeRuralUrban && ruralUrbanOrigin != null)
            result += "[origin:" + ruralUrbanOrigin + "]";

        return result;
    }


    // Safely removes the outer curly braces from a JSON string.
    public string RemoveCurlyBraces(string originalString)
    {
        if (originalString != null && originalString.Length >= 2)
            return originalString.Substring(1, originalString.Length - 2);
        return "";
    }


    // Builds a valid JSON grammar string, gracefully handling empty word lists or grammar bodies.
    private string BuildGrammarString(string wordList, string grammar)
    {
        bool hasWordList = !string.IsNullOrEmpty(wordList);
        bool hasGrammar  = !string.IsNullOrEmpty(grammar);

        if (hasWordList && hasGrammar)  return "{" + wordList + ", " + grammar + "}";
        else if (hasWordList)           return "{" + wordList + "}";
        else if (hasGrammar)            return "{" + grammar + "}";
        else                            return "";
    }
}