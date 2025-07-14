using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityTracery;

public class traceGrammarControl : MonoBehaviour
{
    public List<TextAsset> GrammarFiles;
    public TextAsset wordListJSON;
    [HideInInspector]
    public string wordListString;

    // Start is called before the first frame update
    void Start()
    {
        //Grammars are created in the text objects themselves: no need to create them here! This is just for storing all the grammars as a list.

        //The only thing that needs to be saved is the string of the master word list, so that it can be passed into any generated grammar.
        wordListString = wordListJSON.ToString();
    }


    public TextAsset FindJsonFileByName(List<TextAsset> grammarList, string filename)
    {
        foreach (TextAsset jsonFile in grammarList)
        {
            if (jsonFile.name == filename)
            {
                return jsonFile;
            }
        }

        // Return null if the file is not found
        return null;
    }


    //Load in a new grammar based on the JSON filename.
    public TraceryGrammar loadNewGrammar(List<TextAsset> grammarList, string filename)
    {
        foreach (TextAsset jsonFile in grammarList)
        {
            if (jsonFile.name == filename)
            {
                return new TraceryGrammar(jsonFile.text);
            }
        }

        // Return null if the file is not found
        return null;
    }

}
