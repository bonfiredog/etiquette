using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class startingStation : MonoBehaviour
{
    
    public GameObject dataObject;
    public GameObject stationGen;
    

    private string thisStationName;
    private bool hasSet = false;
    private dataTest data;
    private StationScheduler ss;
    TextMeshPro myText;
    stationMove myMove;
    generateStation gen;
    private string[] typesOfAwn =  {"W", "U", "I", "H", "M", "V"};


    // Start is called before the first frame update
    void Start()
    {
        //Initial Values
        data = dataObject.GetComponent<dataTest>();
        ss = GameObject.Find("stationScheduleController").GetComponent<StationScheduler>();
        myText = gameObject.transform.Find("station_name_text").GetComponent<TextMeshPro>();
        myMove = gameObject.GetComponent<stationMove>();
        gen = stationGen.GetComponent<generateStation>();
    }

    void Update()
    {
        if (hasSet == false)
        {
            //Get the station name and other properties (as it won't be done by the generator).
            thisStationName = data.stationData.GetJSON("1").GetString("stationName");
            myText.text = thisStationName;

             var myflagging = gameObject.transform.Find("stationflagging").GetComponent<TextMeshPro>();
             var myawning = gameObject.transform.Find("stationawning").GetComponent<TextMeshPro>();

        //Set its awning type & generate.

        string myAwn = typesOfAwn[Random.Range(0, typesOfAwn.Length)];
        myflagging.text = "";
        myawning.text = "";
        for (var i = 0; i < 400; i++) {
            myflagging.text += myAwn;
             myawning.text += myAwn;
        }
    


            //Set a speed and topSpeed (as it won't be created by the generator).
            myMove.speed = 0;
            myMove.topspeed = (800 / 100) * 250;
            hasSet = true;

            //Set the size of arches and line (as this won't be done by the generator)
            float thisStationSize = data.stationData.GetJSON("1").GetFloat("stationSize");

            gen.setStationArchAndLine(gameObject, thisStationSize);
        }

    }
}
