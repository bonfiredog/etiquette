using UnityEngine;
using System;
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


void Start() {
    data = dataObject.GetComponent<dataTest>();
    ss = stationObj.GetComponent<StationScheduler>();
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
Debug.Log($"{dayOrdinal} {monthName}, {time24}");

TextMeshPro dateentry = dateObj.GetComponent<TextMeshPro>();
dateentry.text = $"{dayOrdinal} {monthName}";

int stationtotal = data.stationData.Count;

// Go Through All Stations 
for (int x = 1; x < stationtotal; x++) {
GameObject thisline = GameObject.Find("times" + x);
TextMeshPro thislinetext = thisline.GetComponent<TextMeshPro>();

stationLong = Mathf.Round(float.Parse(ss.getStationDataPointString(x, "longitude")));  
Debug.Log($"SL: {stationLong}");
stationRegress = System.Math.Abs(stationLong* 4).ToString();

float distance = (float)ss.getStationDataPointInt(x, "distanceFromLastStation");
Debug.Log($"dist: {distance}");
float topspeed = 26.9f;
float acceleration = 0.3f;
float averageSpeed = GetAverageSpeed(distance, topspeed, acceleration);
Debug.Log($"AS:{averageSpeed}");

//Special Cases: London Paddington and Penzance
if (x == 1) {
thislinetext.text = $"            {time24}                     {stationRegress}";
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

Debug.Log($"AT:{arrivalTime}");

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



if (x != 68) {
thislinetext.text = $"{arrivalTime} / {leaveTime}                    {stationRegress}";
} else {
  thislinetext.text = $"{arrivalTime}                      0";  
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
}
