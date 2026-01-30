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
        Debug.Log($"Object Check: {data}");
        thisStationText.text = data.stationData.GetJSON(stationArrayNumber.ToString()).GetString("stationName");

        //Set its number of arches, and size of its line, based on the size.
        float size = data.stationData.GetJSON(stationArrayNumber.ToString()).GetFloat("stationSize");

        setStationArchAndLine(thisStation, size);

    }

    public void setStationArchAndLine(GameObject thisStation, float size)
    {

        //Set the line's x scale as a multiplier of the size.
        var linequad = thisStation.transform.Find("Quad");
        var awnline = thisStation.transform.Find("AWN");
        var backcube = thisStation.transform.Find("Cube");
        
        var flagging = thisStation.transform.Find("stationflagging").GetComponent<RectTransform>();
         
        var awning = thisStation.transform.Find("stationawning").GetComponent<RectTransform>();
        var yscale = thisStation.transform.localScale.y;
        var zscale = thisStation.transform.localScale.z;
        var xscale = thisStation.transform.localScale.x;



        backcube.transform.localScale = new Vector3 (backcube.transform.localScale.x, backcube.transform.localScale.y, backcube.transform.localScale.z * size);
        linequad.transform.localScale = new Vector3(200 * size, 5, 20);
        awnline.transform.localScale = new Vector3(200 * size, 5, 20);
        if (size > 1) {
        flagging.sizeDelta = new Vector2(170 * size, 5);
        awning.sizeDelta = new Vector2(170 * size, 5);
        } else {
        flagging.sizeDelta = new Vector2(20,5);
        awning.sizeDelta = new Vector2(20,5);
        }

        //Add the number of arches corresponding to the size.
        //Firstly, make sure we are adding an even number.

        //Firstly, add one to make sure there are enough.
        size += 1;

        if (size % 2 == 0)
        {
           finalsize = size;
        }
        else
        {
           finalsize = size + 1;
        }

    
            //There are finalsize number of slots: we fill them one at a time using i.

            //Firstly, work out how many to create on each side.
            var sideCount = finalsize / 2;
        

            //Firstly, create all the arches on the left side.
            for (int k = 1; k < sideCount; k++ )
            {
                //Firstly, create a new arch, make it a child of the station and place it in the 'default' position.
                GameObject thisArch = Instantiate(archObject);
                      
                
                thisArch.transform.SetParent(thisStation.transform);
                thisArch.transform.localPosition = new Vector3(0, 84.8f, 0);
                thisArch.transform.localScale = new Vector3(thisArch.transform.localScale.x * 3.5f, thisArch.transform.localScale.y * 3.5f, thisArch.transform.localScale.z * 3.5f);
                Quaternion localRot = Quaternion.Euler(0f, 90f, 0f);     
                thisArch.transform.localRotation = localRot;

                //Set a movement amount.
                float moveAmount = 120f;

                //Move the arch according to the current k value.
                thisArch.transform.localPosition += new Vector3(0, 0, -moveAmount * k);

            }

            //Firstly, create all the arches on the left side.
            for (int m = 1; m < sideCount; m++)
            {
                //Firstly, create a new arch, make it a child of the station and place it in the 'default' position.
                GameObject thisArch = Instantiate(archObject);
                thisArch.transform.SetParent(thisStation.transform);
                thisArch.transform.localPosition = new Vector3(0, 84.8f, 0);
                 thisArch.transform.localScale = new Vector3(thisArch.transform.localScale.x * 3.5f, thisArch.transform.localScale.y * 3.5f, thisArch.transform.localScale.z * 3.5f);
            Quaternion localRot = Quaternion.Euler(0f, 90f, 0f);
                thisArch.transform.localRotation = localRot;

                //Set a movement amount.
                float moveAmount = 120f;

                //Move the arch according to the current k value.
                thisArch.transform.localPosition += new Vector3(0, 0, moveAmount * m);

            }
        }

}
