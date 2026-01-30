using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityTracery;
using TMPro;
using UnityEditor;
using Leguar.TotalJSON;
using System;

public class textGenOneShot : MonoBehaviour
{
    public string startingGrammarName;
    public bool generated;

    [HideInInspector]
    public GameObject gc;
    [HideInInspector]
    public traceGrammarControl gcScript;
    [HideInInspector]
    public TextMeshPro myText;
    [HideInInspector]
    public TextAsset currentGrammarJSON;
    [HideInInspector]
    public TraceryGrammar currentGrammar;

    private string finalGrammarText;
    private string wordListToParse;
    public string grammarParse;
    private StationScheduler ss;
    private timeControl tc;
    private bool amGenerated = false;

    void Start()
    {
            //Initial Values
            gc = GameObject.Find("grammarController");
            gcScript = gc.GetComponent<traceGrammarControl>();
            myText = gameObject.GetComponent<TextMeshPro>();
            ss = GameObject.Find("stationScheduleController").GetComponent<StationScheduler>();
            

        
            }
            //====================================================|          
             

void Update() {

    if (amGenerated == false) {
        //Setting Grammar To Parse ========================================|

                //Find the JSON file we want by its filename in the grammar files, and save it as our current grammar.
                currentGrammarJSON = gcScript.FindJsonFileByName(gcScript.GrammarFiles, startingGrammarName);
                string grammarString = currentGrammarJSON.ToString();

                //Remove the curly braces from both strings, and reattached with new, enclosing curly braces.
                wordListToParse = removeCurlyBraces(gcScript.wordListString);
                var grammarToParse = removeCurlyBraces(grammarString);
                var finalGrammarString = "{" + wordListToParse + ", " + grammarToParse + "}";

                currentGrammar = new TraceryGrammar(finalGrammarString);



            // AAAAAND GENERATE
                setGrammarForObject(startingGrammarName);
                generateTextFromGrammar(myText);
                Debug.Log("DONE");
        amGenerated = true;
        }

}

    //Use this to initial set or change the current grammar.
    public void setGrammarForObject(string grammarName)
    {
        gc = GameObject.Find("grammarController");
        gcScript = gc.GetComponent<traceGrammarControl>();
        //Setting Grammar To Parse ========================================|

        //Find the JSON file we want by its filename in the grammar files, and save it as our current grammar.
        var currentGrammarJSON = gcScript.FindJsonFileByName(gcScript.GrammarFiles, grammarName).text;

        //Remove the curly braces from both strings, and reattached with new, enclosing curly braces.
        wordListToParse = removeCurlyBraces(gcScript.wordListString);
        var grammarToParse = removeCurlyBraces(currentGrammarJSON);
        var finalGrammarString = "{" + wordListToParse + " " + grammarToParse + "}";

        currentGrammar = new TraceryGrammar(finalGrammarString);


        //====================================================|
    }

    public void generateTextFromGrammar(TextMeshPro myText)
    {
        var ssch = GameObject.Find("stationScheduleController").GetComponent<StationScheduler>();

        //Get the current variables that affect this, then add origin on the end.
        grammarParse = 
         "[current_timeofday:" + ssch.currenttod + "]" 
         + "[current_season:" + ssch.currentseason + "]"
        + "[current_month:" + ssch.currentmonth + "]"
        + "[next_month:" + ssch.nextmonth + "]"
        + "[current_mealtime:" + ssch.currentmealtime + "]"
        + "[appropriate_person:" + ssch.appropriateperson + "]"
        + "[current_terrain:" + ssch.currentterrain + "]"
        + "[station_last:" + ssch.stationlast + "]"
        + "[appropriate_locations:" + ssch.appropriatelocs + "]"
        + "[appropriate_buildings:" + ssch.appropriatebuildings + "]"
        + "[large_number:" + Math.Round(UnityEngine.Random.Range(10000f,1000000f)).ToString() + "]"
        + "#origin#";

            myText.text = currentGrammar.Parse(grammarParse);
        
       // myText.text = currentGrammar.Generate();
   

        }

   
    public string removeCurlyBraces(string originalString)
    {
        if (originalString.Length > 0) {
        return originalString.Substring(1, originalString.Length - 2);
        } else {
            return "";
        }
    }


}
