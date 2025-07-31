using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class StationScheduler : MonoBehaviour
{

    [HideInInspector]
    public int nextStationMinStay;
    [HideInInspector]
    public int nextStationMaxStay;
    [HideInInspector]
    public int lastStation = 0;
    private int nextStationDelayChance;

    public float lastStationLong;
    public int nextStation = 1;
    public string nextStationName = "";
    public float milesToNextStation;
    public float nextStationMilesTotal;
    public float currentStationTimer = 0;
    public float nextStationLong;
    public int nextStationStop;
    public int nextStationSize;
    public GameObject stationgen;
    public bool hasGenNonStopStation = false;
    public float nextStationDelayTimer;
    public float delayTimer;
    public GameObject delayObject;
    public GameObject dataObject;
    public int currentUrbanDensity;
    public string currentTerrain = "coast";
    public int secondsSlowDown;
    public float currentMilePoint;
    public int nextStationSpeedMod;
    public string lastStationName = "Penzance";
    public bool ending = false;
    public string currentterrain = "coast";
    public string stationlast = "Penzance";
    public string currentseason = "spring";
    public string currentmonth = "May";
    public string nextmonth = "june";
    public string appropriatelocs = "#rural_locations#";
    public string currenttod = "dusk";
    public string currentmealtime = "supper";
    public string appropriateperson = "#person_roles_rural#";
    public string appropriatebuildings = "#rural_buildings#";
    private float weatherTimer = 10000;
    public string currentweather = "clear";


    TrainControl tc;
    private dataTest data;
    private bool loadInitialData = false;
    private generateStation genscript;


    //===============================================================================================

    // Start is called before the first frame update
    void Start()
    {
        //Initial Values
        tc = GameObject.Find("trainController").GetComponent<TrainControl>();
        data = dataObject.GetComponent<dataTest>();
        genscript = stationgen.GetComponent<generateStation>();
        updateGrammarVariables();

    }


    //===============================================================================================

    // Update is called once per frame
    void Update()
    {
        updateGrammarVariables();

        //Load in the data for the first time from the JSON, to get the initial station data.
        if (loadInitialData == false)
        {
            getStationData(nextStation);
            currentTerrain = getStationDataPointString(nextStation, "terrain");
            currentUrbanDensity = getStationDataPointInt(nextStation, "urbanDensity");
            //Zero the miles to the next station (we're already there.)
            milesToNextStation = 0;
            //Set the CurrentStationTimer to something set, for the first station. We want to get underway!
            currentStationTimer = 5000;

            //Grab the initial longitude values from the database, and set the trains current longitude to the first station's longitude.
            nextStationLong = float.Parse(data.stationData.GetJSON(nextStation.ToString()).GetString("longitude"));
            lastStationLong = nextStationLong;
            tc.currentLong = nextStationLong;

            loadInitialData = true;
        }

        //DOCKED AT STATION
        //When docked, count down the station timer. When it's at 0, removed the docked status.
        if (tc.docked == true && ending == false)
        {
            if (currentStationTimer > 0)
            {
                currentStationTimer -= 1 * Time.deltaTime;
            }
            else
            {



                //Get the next station data, and place the last station data into the correct variables.
                lastStation = nextStation;
                
                lastStationLong = nextStationLong;

                nextStation += 1;
                getStationData(nextStation);

                tc.docked = false;
                Debug.Log("Leaving station...");
          
            }
        }

        //UNDOCKED AND TRAVELLING
        if (tc.docked == false)
        {
            if (nextStationStop == 1)
            {
                if (milesToNextStation > 0)
            {
              
                    //Count down the miles to the next station based on the train's current speed.
                    milesToNextStation -= tc.trainCurrentSpeed * Time.deltaTime;

                          

                        //Check whether, at current speed, in this frame, it would take slowdownTime to reach 0 miles.
                        if (tc.docking == false)
                        {
                            if (milesToNextStation <= tc.secondsToSlowGentle)
                            {
                                tc.trainSlowDownStartSpeed = tc.trainCurrentSpeed;
                                tc.docking = true;
                                Debug.Log("Slowing down for the station...");
                                tc.decelerationStartTime = Time.time;
                                tc.decelerationTime = (2 * tc.secondsToSlowGentle) / tc.trainSlowDownStartSpeed;
                                //Generate a station.
                                genscript.generateAStation(nextStation, genscript.stationTopSpeed);
                            }
                        }

                }
                else
                {
                    //If we are at mile 0 (i.e. we are at the station, make sure we are docked, and start the station timer.
                    tc.docked = true;
                    tc.docking = false;
                    Debug.Log($"We are at {nextStationName}!");
                    currentStationTimer = UnityEngine.Random.Range(nextStationMinStay, nextStationMaxStay);
                }
            } else if (nextStationStop == 0)
            {
                //If not stopping...
                if (milesToNextStation > 0)
                {
                    //Count down the miles to the next station based on the train's current speed.
                    milesToNextStation -= tc.trainCurrentSpeed * Time.deltaTime;


                    if (milesToNextStation <= tc.secondsToSlowGentle && hasGenNonStopStation == false)
                    {                   
                        //Generate a station, but don't slow down!
                        genscript.generateAStation(nextStation, tc.trainTopSpeed);
                        hasGenNonStopStation = true;
                    }
                } else
                {
                    //If we are at mile 0, don't stop, but set the station to the next station. 
                    //Get the next station data, and place the last station data into the correct variables.
                    lastStation = nextStation;
                    lastStationLong = nextStationLong;
                    hasGenNonStopStation = false;

                    nextStation += 1;
                    getStationData(nextStation);
                }
            }     
        }
        //Lock the miles (an arbitrarily huge number)
        milesToNextStation = Mathf.Clamp(milesToNextStation, 0, 10000000000);



        //DELAYING

        //If we are able to delay and not get in the way of any other functionality...
        if (milesToNextStation > (milesToNextStation * 0.25f) && tc.docked == false && tc.docking == false && tc.delaying == false)
        {
            //The timer goes down...
            if (nextStationDelayTimer > 0) {
                nextStationDelayTimer -= 1 * Time.deltaTime;
            } else
            {
            //We check for a random chance of a delay occurring. If it does, start delaying.
                var delayChance = UnityEngine.Random.Range(0, 100);
                if (delayChance <= nextStationDelayChance)
                {
                    tc.delaying = true;
                    delayTimer = UnityEngine.Random.Range(60, 2000);
                }
            }
        }
 

    }

    //================================ GETTING NEXT STATION DATA FROM JSON
    //Note that different data types need different methods. String and int should be enough.
private void getStationData(int station)
    {
        nextStationName = getStationDataPointString(station, "stationName");       
        milesToNextStation = getStationDataPointInt(station, "distanceFromLastStation");
        nextStationMilesTotal = milesToNextStation;
        nextStationMinStay = getStationDataPointInt(station, "minStationStay");
        nextStationMaxStay = getStationDataPointInt(station, "maxStationStay");
        nextStationSize = getStationDataPointInt(station, "stationSize");
        nextStationLong = float.Parse(getStationDataPointString(station, "longitude"));
        nextStationStop = getStationDataPointInt(station, "willStop");
        nextStationSpeedMod = getStationDataPointInt(station, "speedMod");
        tc.trainTopSpeed = (tc.trainTopSpeedOriginal / 100) * nextStationSpeedMod;
        lastStationName = getStationDataPointString(lastStation, "stationName");
        currentTerrain = getStationDataPointString(nextStation, "terrain");
        currentUrbanDensity = getStationDataPointInt(nextStation, "urbanDensity");
        nextStationDelayChance = getStationDataPointInt(station, "delayChance");
        nextStationDelayTimer = UnityEngine.Random.Range(milesToNextStation * 0.25f, milesToNextStation * 0.75f);

       
    }

public string getStationDataPointString(int station, string keyname)
{
        return data.stationData.GetJSON(station.ToString()).GetString(keyname);

}

    public int getStationDataPointInt(int station, string keyname)
    {
        return data.stationData.GetJSON(station.ToString()).GetInt(keyname);
    }

    public void updateGrammarVariables()
    {
        currentterrain = currentTerrain;
        stationlast = lastStationName;


        if (weatherTimer > 0)
        {
            weatherTimer -= 1 * Time.deltaTime;
        }
        else
        {
            if (currentseason == "Winter")
            {
                currentweather = "rain, thunderbanks, clouds, hail, snow, wind, clear, stormclouds, high clouds, low clouds, sleet, showers, sun, gales";
            }
            else if (currentseason == "Spring")
            {
                currentweather = "rain, thunderbanks, clouds, wind, clear, stormclouds, high clouds, low clouds, showers, sun, gales";
            }
            else if (currentseason == "Summer")
            {
                currentweather = "rain, thunderbanks, clouds, wind, clear, stormclouds, high clouds, low clouds, showers, sun, gales";
            }
            else if (currentseason == "Autumn")
            {
                currentweather = "rain, thunderbanks, clouds, wind, clear, stormclouds, high clouds, low clouds, showers, sun, gales, sleet, hail";
            }
            weatherTimer = UnityEngine.Random.Range(1000, 10000);
        }

        if (currentUrbanDensity < 25)
        {
            appropriateperson = "#person_role_country#";
            if (currentterrain != "coast")
            {
                appropriatelocs = "#rural_locations#";
                appropriatebuildings = "#rural_buildings#";
            }
            else
            {
                appropriatelocs = "#coastal_locations#";
                appropriatebuildings = "#coastal_buildings";
            }
        }
        else if (currentUrbanDensity >= 25 && currentUrbanDensity < 50)
        {
            appropriateperson = "#person_role_town#";

            if (currentterrain != "coast")
            {
                appropriatelocs = "#town_locations#";
                appropriatebuildings = "#town_buildings#";
            }
            else
            {
                appropriatelocs = "#coastal_locations#";
                appropriatebuildings = "#coastal_buildings";
            }
        }
        else
        {
            appropriateperson = "#person_role_city#";
            if (currentterrain != "coast")
            {
                appropriatelocs = "#city_locations#";
                appropriatebuildings = "#city_buildings#";
            }
            else
            {
                appropriatelocs = "#coastal_locations#";
                appropriatebuildings = "#coastal_buildings";
            }
        }



        //Updating variables
        switch (DateTime.Now.Month)
        {
            case 1:
                currentmonth = "January";
                currentseason = "Winter";
                nextmonth = "February";
                break;

            case 2:
                currentmonth = "February";
                currentseason = "Winter";
                nextmonth = "March";
                break;

            case 3:
                currentmonth = "March";
                currentseason = "Winter";
                nextmonth = "April";
                break;

            case 4:
                currentmonth = "April";
                currentseason = "Spring";
                nextmonth = "May";
                break;

            case 5:
                currentmonth = "May";
                currentseason = "Spring";
                nextmonth = "June";
                break;

            case 6:
                currentmonth = "June";
                currentseason = "Spring";
                nextmonth = "July";
                break;

            case 7:
                currentmonth = "July";
                currentseason = "Summer";
                nextmonth = "August";
                break;

            case 8:
                currentmonth = "August";
                currentseason = "Summer";
                nextmonth = "September";
                break;

            case 9:
                currentmonth = "September";
                currentseason = "Summer";
                nextmonth = "October";
                break;

            case 10:
                currentmonth = "October";
                currentseason = "Autumn";
                nextmonth = "November";
                break;

            case 11:
                currentmonth = "November";
                currentseason = "Autumn";
                nextmonth = "December";
                break;

            case 12:
                currentmonth = "December";
                currentseason = "Winter";
                nextmonth = "January";
                break;
        }

        if (DateTime.Now.Hour > 4 && DateTime.Now.Hour <= 11)
        {
            currenttod = "Morning";
            currentmealtime = "breakfast";
        }
        else if (DateTime.Now.Hour > 11 && DateTime.Now.Hour <= 14)
        {
            currenttod = "Day";
            currentmealtime = "luncheon";
        }
        else if (DateTime.Now.Hour > 14 && DateTime.Now.Hour <= 17)
        {
            currenttod = "Afternoon";
            currentmealtime = "tea";
        }
        else if (DateTime.Now.Hour > 17 && DateTime.Now.Hour <= 22)
        {
            currenttod = "Evening";
            currentmealtime = "dinner";
        }
        else if (DateTime.Now.Hour > 22 && DateTime.Now.Hour <= 24)
        {
            currenttod = "Night";
            currentmealtime = "supper";
        }
        else if (DateTime.Now.Hour > 0 && DateTime.Now.Hour < 4)
        {
            currenttod = "Night";
            currentmealtime = "breakfast";
        }
    }

}
