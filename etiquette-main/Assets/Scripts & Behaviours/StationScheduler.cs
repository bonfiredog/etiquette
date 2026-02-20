using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

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
    private GameObject sec;
     private startEndController startcontrol;
     private float timediscrepMod;
     public GameObject timetable;
     private timetableproperties ttp;
     private float currentMiles;
     private float idealMiles;
     private float metragediscrep;
     private bool chancepassingtrain = false;
     private bool chanceaccident = false;
     public GameObject timecontroller;
     


    TrainControl tc;
    private dataTest data;
    private bool loadInitialData = false;
    private generateStation genscript;

    public class ClosestTimeResult
    {
        public int entryNumber;
        public string timeType; // "arrive" or "leave"
        public string timeValue;
        public TimeSpan difference;
    }

        [System.Serializable]
    public class ScheduleEntry
    {
        public string arrive;
        public string leave;
        public int miles;
    }

    [System.Serializable]
    public class ScheduleData
    {
        public ScheduleEntry[] entries;
    }


    //===============================================================================================

    // Start is called before the first frame update
    void Start()
    {
        //Initial Values
        tc = GameObject.Find("trainController").GetComponent<TrainControl>();
        data = dataObject.GetComponent<dataTest>();
        genscript = stationgen.GetComponent<generateStation>();
        updateGrammarVariables();
               sec = GameObject.Find("startEndController");
       startcontrol = sec.GetComponent<startEndController>();
        ttp = timetable.GetComponent<timetableproperties>();
        currentMiles = 0;
        nextStationDelayTimer = 50;

    }


    //===============================================================================================

    // Update is called once per frame
    void Update()
    {
       // updateGrammarVariables();

        //Keep track of the miles covered 
        currentMiles += tc.trainCurrentSpeed * Time.deltaTime;

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
        if (tc.docked == true && startcontrol.isStarted == true)
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



//======================================================================================
        //DELAYING
        //Keep track of the current time discrepancy (between 'local' and 'railway') - between 1 & 100.
        timediscrepMod = ((System.Math.Abs(tc.currentLong * 4)) / 25) * 100;

        //We can only TRY to delay the train if it is between stations (with a good buffer), and it is not already delaying.

         if (milesToNextStation > (milesToNextStation * 0.1f) && tc.docked == false && tc.docking == false && tc.delaying == false)
        {
        //Reduce the timer.
            if (nextStationDelayTimer > 0) {
                nextStationDelayTimer -= 1 * Time.deltaTime;
            } else {
            // When the timer reaches zero...
            //Chance Of A Train?
             int diceroll = UnityEngine.Random.Range(1, 101);
                  if (diceroll <= 10) {
             
                chancepassingtrain = true;
                  } else {
                    chancepassingtrain = false;
                  }

            //Chance Of An Accident

            //Youll need to work out the discerpancy of current miles vs. ideal miles. 
            //To get ideal miles, find which station from schedule.json is closest to the current time (which was the last station...)
            int entryNumber = FindClosestPastTime();
            int nextStationentryNumber = entryNumber + 1;
            DateTime lastStationLeave = ParseTimeString(GetEntry(entryNumber).leave);
            DateTime nextStationArrive = ParseTimeString(GetEntry(nextStationentryNumber).arrive);
            TimeSpan minuteDifference = nextStationArrive - lastStationLeave;
            TimeSpan sinceLastStation = DateTime.Now - lastStationLeave;
            //Find out how many miles past last station you SHOULD be, based on the time...
            float percentBetweenStations = (float)(sinceLastStation.TotalMinutes / minuteDifference.TotalMinutes) * 100f;
            idealMiles = GetEntry(entryNumber).miles + (((GetEntry(nextStationentryNumber).miles - GetEntry(entryNumber).miles) / 100) * percentBetweenStations);

            metragediscrep = idealMiles - currentMiles;


            //Randomly choose a number of sentences from sentences.json (a percentage of the total);
            //Add more for:
            //Time Discrep Mod
            //Metrage discrepancy
            //TOD (darkness is higher chance).
            //Weather (certain weathers have higher chance).

            //From those sentences... are trains mentioned anywhere? If so, we have an accident (and turn off chancepassingtrain)

            if (chanceaccident == true || chancepassingtrain == true) {
                    tc.delaying = true;
                    if (chanceaccident == true) {
                    tc.delaytype = "accident";
                    } else {
                     tc.delaytype = "train";
                    }

                    //Create texts at the appropriate distance to be perfectly aligned with window when the train stops.
            }
            //Reset Tmer
            chancepassingtrain = false;
            chanceaccident = false;
            nextStationDelayTimer = UnityEngine.Random.Range(20, 50);
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


public int FindClosestPastTime()
{
    // Path to the JSON file
    string path = Application.dataPath + "/schedule.json";

    if (!File.Exists(path))
    {
        Debug.LogError("schedule.json not found!");
        return -1;
    }

    // Read the JSON file
    string json = File.ReadAllText(path);
    ScheduleData scheduleData = JsonUtility.FromJson<ScheduleData>(json);

    DateTime now = DateTime.Now;
    TimeSpan smallestDifference = TimeSpan.MaxValue;
    int closestEntryNumber = -1;

    // Go through each entry
    for (int i = 0; i < scheduleData.entries.Length; i++)
    {
        ScheduleEntry entry = scheduleData.entries[i];

        // Check arrive time
        if (!string.IsNullOrEmpty(entry.arrive))
        {
            DateTime arriveTime = ParseTimeString(entry.arrive);
            TimeSpan difference = now - arriveTime;

            // Only consider if it's in the past (positive difference)
            if (difference.TotalSeconds > 0 && difference < smallestDifference)
            {
                smallestDifference = difference;
                closestEntryNumber = i;
            }
        }

        // Check leave time
        if (!string.IsNullOrEmpty(entry.leave))
        {
            DateTime leaveTime = ParseTimeString(entry.leave);
            TimeSpan difference = now - leaveTime;

            // Only consider if it's in the past (positive difference)
            if (difference.TotalSeconds > 0 && difference < smallestDifference)
            {
                smallestDifference = difference;
                closestEntryNumber = i;
            }
        }
    }

    if (closestEntryNumber == -1)
    {
        Debug.LogWarning("No past times found in schedule!");
        return -1;
    }

    Debug.Log($"Closest past time is in entry {closestEntryNumber} " +
              $"({smallestDifference.TotalMinutes:F1} minutes ago)");

    return closestEntryNumber;
}

private DateTime ParseTimeString(string timeString)
{
    // Expected format: "HH:mm" or "H:mm" (e.g., "09:30" or "9:30")
    string[] parts = timeString.Split(':');
    
    if (parts.Length != 2)
    {
        Debug.LogWarning($"Invalid time format: {timeString}");
        return DateTime.MinValue;
    }

    int hour, minute;
    if (int.TryParse(parts[0], out hour) && int.TryParse(parts[1], out minute))
    {
        if (hour >= 0 && hour <= 23 && minute >= 0 && minute <= 59)
        {
            return DateTime.Today.AddHours(hour).AddMinutes(minute);
        }
    }

    Debug.LogWarning($"Invalid time values: {timeString}");
    return DateTime.MinValue;
}

public ScheduleEntry GetEntry(int entryNumber)
{
    // Path to the JSON file
    string path = Application.dataPath + "/schedule.json";

    if (!File.Exists(path))
    {
        Debug.LogError("schedule.json not found!");
        return null;
    }

    // Read the JSON file
    string json = File.ReadAllText(path);
    ScheduleData scheduleData = JsonUtility.FromJson<ScheduleData>(json);

    // Validate entry number
    if (entryNumber < 0 || entryNumber >= scheduleData.entries.Length)
    {
        Debug.LogError($"Invalid entry number: {entryNumber}");
        return null;
    }

    return scheduleData.entries[entryNumber];
}
}