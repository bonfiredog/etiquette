using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;

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

    //Appropriate Variables
    public string currentterrain = "coast";
    public string stationlast = "Penzance";
    public string currentCounty = "Cornwall";
    public string currentseason = "spring";
    public string currentmonth = "May";
    public string nextmonth = "june";
    public string appropriatelocs = "#rural_locations#";
    public string currenttod = "dusk";
    public string currentmealtime = "supper";
    public string current_vehicles = "#sea_river_vehicles_home#";
     public string current_vehicles_rare = "#sea_river_vehicles_empire#";
    public string appropriateperson = "#person_role_rural#";
    public string appropriatebuildings = "#rural_buildings_common#";
    public string appropriatebuildingsrare = "#rural_buildings_rare#";
    private float variablesTimer = 20;
    public string currentweather = "clear";

    private GameObject sec;
     private startEndController startcontrol;
     private float timediscrepMod;
     public GameObject timetable;
     private rockingController rocker;
     private timetableproperties ttp;
     private float currentMiles;
     private float idealMiles;
     public bool delayTestToggle;
     private float metragediscrep;
     private bool chancepassingtrain = false;
     private bool chanceaccident = false;
     public GameObject timecontroller;
     public string delay = "none";
      string[] wordsToCheck = {    "train", "trains", "rail", "rails", "railway", "railways", "railroad", "railroads",
    "station", "stations", "platform", "platforms", "track", "tracks", "signal", "signals",
    "junction", "junctions", "depot", "depots", "yard", "yards", "crossing", "crossings",
    "locomotive", "locomotives", "engine", "engines", "carriage", "carriages", "coach", "coaches",
    "wagon", "wagons", "timetable", "timetables", "ticket", "tickets", "conductor", "conductors",
    "guard", "guards", "driver", "drivers", "metro", "subway", "underground", "tube", "tram", "trams",
    "light rail", "monorail", "level crossing", "level crossings", "carriage",
            "first class",
            "second class",
            "third class",
            "guardvan",
            "brakevan",
            "coach",
            "van",
            "truck",
            "wagon",
                "porter",
            "ticketmaster",
            "stationmaster",
            "goods porter",
            "attendant",
            "steward",
            "station lad",
            "station cat",
            "policeman",
               "ticket office",
            "waiting room",
            "platform",
            "storage room",
            "attendant's office",
            "station kitchen",
            "goods shed",
            "engine shed",
            "refreshment room",
            "tearoom",  "shunter",
            "signalman",
            "navvy",
            "flagman",
            "gateman",
            "pointsman",
            "wiper",
            "platelayer",
            "foreman",
            "fettler",
            "hostler",
            "wheel tapper",
             "fireman",
            "conductor",
            "brakeman",
            "secondman",
            "stoker",
            "boilerman",
            "engineman",
            "engine",
                "grab iron",
            "bogie",
            "chimney",
            "boiler",
            "tank",
            "coupling",
            "axle",
            "cab",
            "whistle",
            "firebox",
            "engine",
            "blastpipe",
            "fender",
            "bell",
            "bogie",
            "coupler",
            "cylinder",
            "backhead",
            "piston",
            "cab",
            "running gear",
            "lagging",
            "valve",
              "cutting",
            "siding",
            "bridge",
            "arch",
            "depot",
            "crossing",
            "section house",
            "headshunt",
            "pocket track",
            "passing loop",
            "yard",
            "signal nox",
            "workshop",
            "water crane",
            "water stop",
            "wye",
            "block post",
            "buffer stop",
            "coaling tower",
            "train shed",
            "goods shed",
            "water trough",
            "water tank",
            "flyover",
            "turntable",
            "abutment",
            "line",
            "tunnel",
            "pumping station",
            "shed",
            "embankment",
            "enginehouse",
            "carriageworks",
            "culvert",
             "telegraph pole",
            "signal",
            "point",
            "cess",
            "whistle post",
            "rail",
            "fishplate",
            "bell",
            "track",
            "water crane",
            "detonator",
            "point",
            "crosstie",
             "track",
            "railway bridge",
            "foot crossing",
            "level crossing",
            "tunnel",
            "underpass",
            "overpass"
             };
     


    TrainControl tc;
    private dataTest data;
    private bool loadInitialData = false;
    private generateStation genscript;
    private bool checkingdelay = false;

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

    [System.Serializable]
public class SentenceWrapper
{
    public Sentence[] sentences;
}

[System.Serializable]
public class Sentence
{
    public string text;
     public float score;
    public string source_file;
    public int start_char;
    public int end_char;
}

private createText genclose1;
private createText genclose2;
private createText genmiddle;
private createText genback;
private cameraControl cc;
public float timediff;


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
        rocker = GameObject.Find("Rocker").GetComponent<rockingController>();
        genclose1 = GameObject.Find("generator_close1").GetComponent<createText>();
        genclose2 = GameObject.Find("generator_close2").GetComponent<createText>();
        genmiddle = GameObject.Find("generator_middle").GetComponent<createText>();
        genback = GameObject.Find("generator_back").GetComponent<createText>();
        cc = GameObject.Find("Main Camera").GetComponent<cameraControl>();



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

           if (currentseason == "Winter")
            {
                currentweather = "#weather_types_winter#";
            }
            else if (currentseason == "Spring")
            {
                currentweather = "#weather_types_spring#";
                }
            else if (currentseason == "Summer")
            {
               currentweather = "#weather_types_summer#";
                }
            else if (currentseason == "Autumn")
            {
                currentweather = "#weather_types_autumn#";
                 }

                 updateGrammarVariables();
    }


    //===============================================================================================

    // Update is called once per frame
    void Update()
    {
       
        //Keep track of the miles covered 
        currentMiles += tc.trainCurrentSpeed * Time.deltaTime;
        timediff = tc.currentLong * 4;

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
            updateGrammarVariables();
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
                nextStationDelayTimer = UnityEngine.Random.Range(20, 50);
                rocker.SuddenJolt();
                Debug.Log("Leaving station...");
                updateGrammarVariables();
          
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
                                updateGrammarVariables();
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
                    rocker.SuddenJolt(rocker.suddenJoltStrength * 0.7f);
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
         if (milesToNextStation > (nextStationMilesTotal * 0.1f) && tc.docked == false && tc.docking == false && tc.delaying == false && checkingdelay == false && delayTestToggle == false)
        {
        //Reduce the timer.
            if (nextStationDelayTimer > 0) {
                nextStationDelayTimer -= 1 * Time.deltaTime;
            } else {
                checkingdelay = true;
                delayDecide(false);
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
        currentCounty = getStationDataPointString(station, "county");
       
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

        
        
        if (currentUrbanDensity < 25)
        {
            appropriateperson = "#person_role_country#";
            if (currentterrain != "coast")
            {
                appropriatelocs = "#rural_locations#";
                appropriatebuildings = "#rural_buildings_common#";
                appropriatebuildingsrare = "#rural_buildings_rare#";
                current_vehicles = "#road_vehicles_home#";
                current_vehicles_rare = "#road_vehicles_empire#";
            }
            else
            {
                appropriatelocs = "#coastal_locations#";
                appropriatebuildings = "#coastal_buildings#";
                appropriatebuildingsrare = "#rural_buildings_rare#";
                current_vehicles = "#sea_river_vehicles_home#";
                current_vehicles_rare = "#sea_river_vehicles_empire#";
            }
        }
        else if (currentUrbanDensity >= 25 && currentUrbanDensity < 50)
        {
            appropriateperson = "#person_role_town#";

            if (currentterrain != "coast")
            {
                appropriatelocs = "#town_locations#";
                appropriatebuildings = "#town_buildings_common#";
                appropriatebuildingsrare = "#town_buildings_rare#";
                 current_vehicles = "#road_vehicles_home#";
                current_vehicles_rare = "#road_vehicles_empire#";
            }
            else
            {
                appropriatelocs = "#coastal_locations#";
                appropriatebuildings = "#coastal_buildings#";
                appropriatebuildingsrare = "#town_buildings_rare#";
                current_vehicles = "#sea_river_vehicles_home#";
                current_vehicles_rare = "#sea_river_vehicles_empire#";
            }
        }
        else
        {
            appropriateperson = "#person_role_city#";
            if (currentterrain != "coast")
            {
                appropriatelocs = "#city_locations#";
                appropriatebuildings = "#city_buildings_common#";
                appropriatebuildingsrare = "#city_buildings_rare#";
                 current_vehicles = "#road_vehicles_home#";
                current_vehicles_rare = "#road_vehicles_empire#";
            }
            else
            {
                appropriatelocs = "#coastal_locations#";
                appropriatebuildings = "#coastal_buildings#";
                   appropriatebuildingsrare = "#city_buildings_rare#";
                   current_vehicles = "#sea_river_vehicles_home#";
                current_vehicles_rare = "#sea_river_vehicles_empire#";
            }
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

[System.Serializable]
public class SentenceData
{
    public string[] sentences;
}




public void delayDecide(bool autoDelay) {
    
 Debug.Log("1 Delaying attempt...");

if (autoDelay == false) {

 // We first of all determine if there is a chance of a train...

             int diceroll = UnityEngine.Random.Range(1, 101);
                  if (diceroll <= 10) {
                    //TRIGGER DELAY WITH PASSING TRAIN
                    tc.delaying = true;
                    checkingdelay = false;
                    delay = "train";
                    rocker.SuddenJolt(rocker.suddenJoltStrength * 2f);

         
                    cc.PushBackIntoCabin();
                    
                    tc.generatedtrain = false;
                    Debug.Log("2) It's a train delay!");
                    delayTimer = UnityEngine.Random.Range(25, 200);

                    } else {

                    Debug.Log ("3) No train delay, trying for an accident delay...");
                    int todmod = 0;
                    int seasonmod = 0;

                     //if not, chance for an accident delay...
                        bool trainwordfound = false;
                        switch (currenttod) {
                            case "Morning":
                            todmod = 1;
                            break;

                            case "Afternoon":
                            todmod = 0;
                            break;

                            case "Evening":
                            todmod = 1;
                            break;

                            case "Night":
                            todmod = 2;
                            break;
                        }

                        switch (currentseason) {
                            case "Winter":
                            seasonmod = 2;
                            break;
                            
                            case "Summer":
                            seasonmod = 0;
                            break;

                            case "Autumn":
                            seasonmod = 1;
                            break;

                            case "Spring":
                            seasonmod = 1;
                            break;
                        
                        }

                    //Choose one (or multiple!) sentences from sentence.json, and look for a train-related word in them.
                    //Randomly choose a number of sentences from sentences.json (a percentage of the total);
                    //Add more for:
                     //Time Discrep Mod
                     //TOD (darkness is higher chance).
                     //Weather (certain weathers have higher chance).
                        double numberofsentences = 1 + Math.Round((2.0 / 100) * timediscrepMod) + Math.Round(2.0 * todmod) + Math.Round(2.0 * seasonmod);

                        Debug.Log($"Checking {numberofsentences} sentences...");

                       string path = Path.Combine(Application.streamingAssetsPath, "sentences.json");

                        string json = File.ReadAllText(path);

                        SentenceWrapper data = JsonUtility.FromJson<SentenceWrapper>(json);

                        for (int i = 0; i < numberofsentences; i++) 

                        {
                               Sentence s = data.sentences[UnityEngine.Random.Range(0, data.sentences.Length)];

                                 trainwordfound = wordsToCheck.Any(word =>
                               s.text.ToLower().Contains(word.ToLower()));

                                   
                        }

                

                        Debug.Log($"Found a train word? {trainwordfound}");

                      if (trainwordfound == true) {
                        tc.delaying = true;
                           rocker.SuddenJolt(rocker.suddenJoltStrength * 2f);
                           cc.PushBackIntoCabin();
                          
                        delay = "accident";
                       checkingdelay = false;
                       delayTimer = UnityEngine.Random.Range(25, 200);

                         } else {
                         //If nothing, exit
                         tc.delaying = false;
                         checkingdelay = false;
                         delay = "none";
                         nextStationDelayTimer = UnityEngine.Random.Range(20, 50);
                    }
                  }

} else if (autoDelay == true) {
    Debug.Log("Auto Delay!");
     tc.delaying = true;
                           rocker.SuddenJolt(rocker.suddenJoltStrength * 2f);
                            cc.PushBackIntoCabin();
                        delay = "accident";
                       checkingdelay = false;
                       delayTimer = UnityEngine.Random.Range(25, 200);
}

            }

            public void deleteTexts() {

                    
                    //Delete any texts that are close to the generators.

                    GameObject[] allObjects = FindObjectsOfType<GameObject>();

                     foreach (GameObject obj in allObjects)
                       {
                       if (obj.name == "frontgen" || obj.name == "loadup" || obj.name == "middlegen" || obj.name == "backgen" || obj.name == "belowgen" || obj.name == "sidegen")
                      {
                      float z = obj.GetComponent<RectTransform>().anchoredPosition3D.z;
                        if (z < -5000) {
                            Destroy(obj);
                        }
                         }
                   }
                  

            }


}

