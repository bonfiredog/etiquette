using UnityEngine;
using System;
using System.IO;
using TMPro;

public class timetableproperties : MonoBehaviour
{

public StartButton startButton;
private bool thisrun = false;
public GameObject dateObj;
 public GameObject dataObject;
   private dataTest data;
   public GameObject stationObj;
private float stationLong;
private StationScheduler ss;
private string stationRegress;
private int workingHour;
private int workingMinute;
private string workingHourString;
private string workingMinuteString;
public DateTime starttime;
public DateTime endtime;

    [System.Serializable]
    public class ScheduleEntry
    {
        public string arrive;
        public string leave;
    }

    [System.Serializable]
    public class ScheduleData
    {
        public ScheduleEntry[] entries;
    }


void Start() {
    data = dataObject.GetComponent<dataTest>();
    ss = stationObj.GetComponent<StationScheduler>();
    starttime = DateTime.Now;
}

    void Update()
    {
         if (startButton.clicked && thisrun == false) {
            DateTime now = DateTime.Now;

    string dayOrdinal = DayToOrdinal(now.Day);
    dayOrdinal = char.ToUpper(dayOrdinal[0]) + dayOrdinal.Substring(1);
string monthName = MonthToName(now.Month);
string time24 = now.ToString("HH.mm");




int currentHour = DateTime.Now.Hour;
workingHour = currentHour;
int currentMinute = DateTime.Now.Minute;
workingMinute = currentMinute;


TextMeshPro dateentry = dateObj.GetComponent<TextMeshPro>();
dateentry.text = $"{dayOrdinal} {monthName}";

int stationtotal = data.stationData.Count;

// Go Through All Stations 
for (int x = 1; x < stationtotal; x++) {
GameObject thisline = GameObject.Find("times" + x);
TextMeshPro thislinetext = thisline.GetComponent<TextMeshPro>();

stationLong = Mathf.Round(float.Parse(ss.getStationDataPointString(x, "longitude")));  

stationRegress = System.Math.Abs(stationLong* 4).ToString();

float distance = (float)ss.getStationDataPointInt(x, "distanceFromLastStation");

float topspeed = 26.9f;
float acceleration = 0.3f;
float averageSpeed = GetAverageSpeed(distance, topspeed, acceleration);


//Special Cases: London Paddington and Penzance
if (x == 1) {
thislinetext.text = $"            {time24}                    {stationRegress}";
thislinetext.ForceMeshUpdate();
//UpdateScheduleEntry(x, "leave", time24);
} else if (x != 1) {
//Get the time of arrival.
int timeminutes = Mathf.RoundToInt((distance / averageSpeed) / 60);

TimeSpan startTime = new TimeSpan(workingHour, workingMinute, 0);
TimeSpan resultTime = startTime.Add(TimeSpan.FromMinutes(timeminutes));

workingHour = resultTime.Hours;
workingMinute = resultTime.Minutes;

if (workingHour < 10) {
    workingHourString = "0" + workingHour.ToString();
} else {
    workingHourString = workingHour.ToString();
}

if (workingMinute < 10) {
    workingMinuteString = "0" + workingMinute.ToString();
} else {
    workingMinuteString = workingMinute.ToString();
}


string arrivalTime = $"{workingHourString}.{workingMinuteString}";
//UpdateScheduleEntry(x, "arrive", arrivalTime);




TimeSpan leavestartTime = new TimeSpan(workingHour, workingMinute, 0);
int waitaverage = ss.getStationDataPointInt(x, "minStationStay") + (ss.getStationDataPointInt(x, "maxStationStay") - ss.getStationDataPointInt(x, "minStationStay"));
int waitminutes = Mathf.RoundToInt(waitaverage / 60);

TimeSpan leaveresultTime = leavestartTime.Add(TimeSpan.FromMinutes(waitminutes));

workingHour = leaveresultTime.Hours;
workingMinute = leaveresultTime.Minutes;



if (workingHour < 10) {
   workingHourString = "0" + workingHour.ToString();
} else {
     workingHourString = workingHour.ToString();
}

if (workingMinute < 10) {
   workingMinuteString = "0" + workingMinute.ToString();
} else {
    workingMinuteString = workingMinute.ToString();
}


string leaveTime = $"{workingHourString}.{workingMinuteString}";
//UpdateScheduleEntry(x, "leave", leaveTime);

if (x != 68) {
thislinetext.text = $"{arrivalTime} / {leaveTime}         {stationRegress}";
thislinetext.ForceMeshUpdate(true,true);
} else {
  thislinetext.text = $"{arrivalTime}                      0";  
  thislinetext.ForceMeshUpdate(true,true);
  //Save the finish time
  int endhour = int.Parse(workingHourString);
    int endminute = int.Parse(workingMinuteString);
  endtime = DateTime.Today.AddHours(endhour).AddMinutes(endminute);

// If the time has already passed today, you might want it for tomorrow:
if (endtime < DateTime.Now)
{
    endtime = endtime.AddDays(1);
}
}
} 
}
thisrun = true;

         }
    }


    string DayToOrdinal(int day)
{
    string[] ordinals =
    {
        "", "first", "second", "third", "fourth", "fifth",
        "sixth", "seventh", "eighth", "ninth", "tenth",
        "eleventh", "twelfth", "thirteenth", "fourteenth", "fifteenth",
        "sixteenth", "seventeenth", "eighteenth", "nineteenth", "twentieth",
        "twenty-first", "twenty-second", "twenty-third", "twenty-fourth",
        "twenty-fifth", "twenty-sixth", "twenty-seventh", "twenty-eighth",
        "twenty-ninth", "thirtieth", "thirty-first"
    };

    return ordinals[day];
}

string MonthToName(int month)
{
    return new DateTime(1, month, 1).ToString("MMMM");
}

   float GetAverageSpeed(float distance, float vMax, float a)
    {
        // Pre-calculate constants
        float dAccel = vMax * vMax / (2f * a);   // distance to reach top speed
        float tAccel = vMax / a;                 // time to reach top speed

        // Average speed formula
        return distance / (2f * tAccel + (distance - 2f * dAccel) / vMax);
    }


/*public void UpdateScheduleEntry(int entryNumber, string arriveOrLeave, string value)
    {
        // Path to the JSON file in Assets folder
        string path = Application.dataPath + "/schedule.json";

        // Check if file exists
        if (!File.Exists(path))
        {
            Debug.LogError("schedule.json not found in Assets folder!");
            return;
        }

        // Read the JSON file
        string json = File.ReadAllText(path);
        ScheduleData scheduleData = JsonUtility.FromJson<ScheduleData>(json);



        // Update the appropriate field
        if (arriveOrLeave == "arrive")
        {
            scheduleData.entries[entryNumber].arrive = value;
        }
        else // "leave"
        {
            scheduleData.entries[entryNumber].leave = value;
        }

        // Write back to the JSON file
        string updatedJson = JsonUtility.ToJson(scheduleData, true); // true = pretty print
        File.WriteAllText(path, updatedJson);

        Debug.Log($"Updated entry {entryNumber}, {arriveOrLeave} = {value}");
    }
*/


}
