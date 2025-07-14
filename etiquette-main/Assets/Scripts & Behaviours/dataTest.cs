using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Leguar.TotalJSON;

public class dataTest : MonoBehaviour
{
    public TextAsset stationDB;
    [HideInInspector]
    string stationDBString;
    public JSON stationData;

    // Start is called before the first frame update
    void Start()
    {
        //Take the JSON & convert it into a formatted string, and then use the TotalJSON Library to convert it into a C# JSON Object.
        stationDBString = stationDB.ToString();
        ParseJSONString(stationDBString);
    }

    private void ParseJSONString(string theString)
    {
        stationData = JSON.ParseString(theString);
        stationData.SetProtected();
        stationData.DebugInEditor("stationData");
    }

}
    

