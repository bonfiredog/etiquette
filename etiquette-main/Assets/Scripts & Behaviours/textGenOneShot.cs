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
    private TextFitController tfc;
    private bool amGenerated = false;
    private string savedtext;

    void Start()
    {
            //Initial Values
            gc = GameObject.Find("grammarController");
            gcScript = gc.GetComponent<traceGrammarControl>();
            myText = gameObject.GetComponent<TextMeshPro>();
            ss = GameObject.Find("stationScheduleController").GetComponent<StationScheduler>();
            if (GetComponent<TextFitController>() != null) {
            tfc = GetComponent<TextFitController>();
            }

        
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
               savedtext = generateTextFromGrammar(myText);

            //Resize if a crate

            if (GetComponent<TextFitController>() != null) {
                    tfc.SetText(savedtext);
               
            }



              
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
        var finalGrammarString = "{" + wordListToParse + ", " + grammarToParse + "}";

        currentGrammar = new TraceryGrammar(finalGrammarString);


        //====================================================|
    }

    public string generateTextFromGrammar(TextMeshPro myText)
    {
        var ssch = GameObject.Find("stationScheduleController").GetComponent<StationScheduler>();

        grammarParse = 
            "[current_person_type:" + currentGrammar.Parse(ssch.appropriateperson) + "]" 
          +  "[current_locations:" + currentGrammar.Parse(ssch.appropriatelocs) + "]"
         +   "[current_timeofday:" + currentGrammar.Parse(ssch.currenttod) + "]" 
        +    "[current_season:" + currentGrammar.Parse(ssch.currentseason) + "]" 
        +   "[current_month:" + currentGrammar.Parse(ssch.currentmonth) + "]" 
        +   "[current_mealtime:" + currentGrammar.Parse(ssch.currentmealtime) + "]"
        +   "[current_terrain:" + currentGrammar.Parse(ssch.currentterrain) + "]"
        +    "[current_buildings:" + currentGrammar.Parse(ssch.appropriatebuildings)+ "]"
        +    "[current_buildings_rare:" + currentGrammar.Parse(ssch.appropriatebuildingsrare)+ "]"
        + "[current_weather_types:" + currentGrammar.Parse(ssch.currentweather) + "]"
        + "#origin#";

            myText.text = currentGrammar.Parse(grammarParse);
            return myText.text;
    
   

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
