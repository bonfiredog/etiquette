using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class generateStation : MonoBehaviour
{
    public GameObject stationPrefab;
    public GameObject dataObject;
    public GameObject archObject;
    public float stationTopSpeed;
    [HideInInspector]
    public float distanceToSpeedMultiplier;

    private dataTest data;
    private TrainControl tc;
    private float finalsize;
    private string[] typesOfAwn =  {"W", "U", "I", "H", "M", "V"};
    private float modifier = 250;
    


    // Start is called before the first frame update
    void Start()
    {
        //Initial Values
        data = dataObject.GetComponent<dataTest>();
        tc = GameObject.Find("trainController").GetComponent<TrainControl>();
        stationTopSpeed = (stationTopSpeed / 100) * modifier;

        //Make sure that the generator is positioned the distance at which the train starts slowing down, + double the z value of the starting station, to make sure that it ends up in the centre.
        distanceToSpeedMultiplier = (stationTopSpeed / tc.trainTopSpeed);
        transform.position = new Vector3(transform.position.x, transform.position.y, -((tc.secondsToSlowGentle * distanceToSpeedMultiplier)) + (52f));
        

    }


    public void generateAStation(int stationArrayNumber, float topspeed)
    {
        //Create a station from the prefab.
        var thisStation = GameObject.Instantiate(stationPrefab);
        var myflagging = thisStation.transform.Find("stationflagging").GetComponent<TextMeshPro>();
        var myawning = thisStation.transform.Find("stationawning").GetComponent<TextMeshPro>();

        //Set its awning type & generate.

        string myAwn = typesOfAwn[Random.Range(0, typesOfAwn.Length)];
        myflagging.text = "";
        myawning.text = "";
        for (var i = 0; i < 400; i++) {
            myflagging.text += myAwn;
            myawning.text += myAwn;
        }
    



        //Set the station to my position.
        thisStation.transform.position = transform.position;
        thisStation.transform.position = new Vector3(transform.position.x, -412.658f, transform.position.z);
     

        //Set the station's name, and its speed.
        var thisStationText = thisStation.transform.Find("station_name_text").GetComponent<TextMeshPro>();
        var thisStationMove = thisStation.GetComponent<stationMove>();
        thisStationMove.topspeed = stationTopSpeed;
        //The line below is not triggering... an object isn't being 'got'.
      
        thisStationText.text = data.stationData.GetJSON(stationArrayNumber.ToString()).GetString("stationName");

        //Set its number of arches, and size of its line, based on the size.
        float size = data.stationData.GetJSON(stationArrayNumber.ToString()).GetFloat("stationSize");

        setStationArchAndLine(thisStation, size);

    }

  public void setStationArchAndLine(GameObject thisStation, float size)
    {
        // Find all child objects
        var linequad = thisStation.transform.Find("Quad");
        var awnline  = thisStation.transform.Find("AWN");
     
        var flagging = thisStation.transform.Find("stationflagging").GetComponent<RectTransform>();
        var awning   = thisStation.transform.Find("stationawning").GetComponent<RectTransform>();

        // Calculate arch count and span first, so everything else can use it.
        float moveAmount = 120f;
        float adjustedSize = size + 1;
        if (adjustedSize % 2 != 0) adjustedSize += 1;
        finalsize = adjustedSize;
        var sideCount = finalsize / 2;
        float archSpan = (sideCount - 1) * moveAmount * 2f;

       
        // Scale the line and awning quads to match size.
        linequad.transform.localScale = new Vector3(200 * size, 5, 20);
        awnline.transform.localScale  = new Vector3(200 * size, 5, 20);

        // Scale the flagging and awning rect transforms.
        if (size > 1)
        {
            flagging.sizeDelta = new Vector2(170 * size, 5);
            awning.sizeDelta   = new Vector2(170 * size, 5);
        }
        else
        {
            flagging.sizeDelta = new Vector2(20, 5);
            awning.sizeDelta   = new Vector2(20, 5);
        }

        // Spawn arches on the left side.
        for (int k = 1; k < sideCount; k++)
        {
            GameObject thisArch = Instantiate(archObject);
            thisArch.transform.SetParent(thisStation.transform);
            thisArch.transform.localPosition = new Vector3(0, 84.8f, 0);
            thisArch.transform.localScale    = new Vector3(
                thisArch.transform.localScale.x * 3.5f,
                thisArch.transform.localScale.y * 3.5f,
                thisArch.transform.localScale.z * 3.5f
            );
            thisArch.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            thisArch.transform.localPosition += new Vector3(0, 0, -moveAmount * k);
        }

        // Spawn arches on the right side.
        for (int m = 1; m < sideCount; m++)
        {
            GameObject thisArch = Instantiate(archObject);
            thisArch.transform.SetParent(thisStation.transform);
            thisArch.transform.localPosition = new Vector3(0, 84.8f, 0);
            thisArch.transform.localScale    = new Vector3(
                thisArch.transform.localScale.x * 3.5f,
                thisArch.transform.localScale.y * 3.5f,
                thisArch.transform.localScale.z * 3.5f
            );
            thisArch.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            thisArch.transform.localPosition += new Vector3(0, 0, moveAmount * m);
        }
    }

}
