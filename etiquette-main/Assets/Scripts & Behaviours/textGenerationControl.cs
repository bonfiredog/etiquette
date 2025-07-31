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

    private float timedTextTimer;
    private string finalGrammarText;
    private int currentTimedGrammar = 0;
    private string wordListToParse;
    public string grammarParse;



    private StationScheduler ss;
    private timeControl tc;


     // Start is called before the first frame update
    void Start()
    {
        if (generated == false)
        {
            //Initial Values
            gc = GameObject.Find("grammarController");
            gcScript = gc.GetComponent<traceGrammarControl>();
            myText = gameObject.GetComponent<TextMeshPro>();
            ss = GameObject.Find("stationScheduleController").GetComponent<StationScheduler>();
            

            //Setting Grammar To Parse ========================================|

            if (gameObject.tag != "centralarch")
            {
                //Find the JSON file we want by its filename in the grammar files, and save it as our current grammar.
                var currentGrammarJSON = gcScript.FindJsonFileByName(gcScript.GrammarFiles, startingGrammarName).text;

                //Remove the curly braces from both strings, and reattached with new, enclosing curly braces.
                wordListToParse = removeCurlyBraces(gcScript.wordListString);
                var grammarToParse = removeCurlyBraces(currentGrammarJSON);
                var finalGrammarString = "{" + wordListToParse + ", " + grammarToParse + "}";

                currentGrammar = new TraceryGrammar(finalGrammarString);

            }
            //====================================================|


            
            if (IsTimedText == true)
            {
                timedTextTimer = timedTextTimerTotal;
            }
        }

    }

void Update()
{
      

        //Changing text on a timer.
        if (IsTimedText == true)
    {
        if (currentTimedGrammar < timedTextGrammars.Length - 1)
        {
                //Count down the timer...
            if (timedTextTimer > 0)
            {
                timedTextTimer -= 1;
            }
            else
            {
                    //When it is zero, replace the text with some generated from the next grammar in the array, and then reset the timer (as long as there are still grammars ahead of it)
                currentTimedGrammar += 1;
                setGrammarForObject(timedTextGrammars[currentTimedGrammar]);
                generateTextFromGrammar(myText);
                timedTextTimer = timedTextTimerTotal;
            }
        }
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
