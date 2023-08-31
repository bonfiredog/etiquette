using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class timeControl : MonoBehaviour
{

    public GameObject minHandPivot;
    public GameObject hourHandPivot;
    public GameObject localminHandPivot;

    [HideInInspector]
    public float gmtMod;
    [HideInInspector]
    public float currentMinute;
    [HideInInspector]
    public float currentHour;
    [HideInInspector]
    public float currentlocalMinute;
    [HideInInspector]
    public float currentlocalHour;
    [HideInInspector]
    public float currentLocalMinuteDifference;

    private TrainControl tc;

    // Start is called before the first frame update
    void Start()
    {
        tc = GameObject.Find("trainController").GetComponent<TrainControl>();
        updateLocalAndNationalTime();
        updateClock();
    }

    // Update is called once per frame
    void Update() {
        updateLocalAndNationalTime();
        updateClock();
    }


    //Rotate all the hands at once.
    public void updateClock()
    {
        SetMinHand(currentMinute);
        SetHourHand(currentHour);
        SetLocalMinHand(currentlocalMinute);
    }


    //Rotate the minute hand based on the current minute value.
    public void SetMinHand(float timeScratch)
    {
            var timePercent = (timeScratch / 60) * 100;

            var currentRotation = 3.6f * timePercent;

        Quaternion targetTotalRotation = Quaternion.Euler(0f, currentRotation, 0f);
        minHandPivot.transform.localRotation = targetTotalRotation;
    }

    //Rotate the hour hand based on the current hour value.
    public void SetHourHand(float timeScratch)
    {
        var timePercent = (timeScratch / 12) * 100;
        var currentRotation = 3.6f * timePercent;

        Quaternion targetTotalRotation = Quaternion.Euler(0f, currentRotation, 0f);
        hourHandPivot.transform.localRotation = targetTotalRotation;
    }

    //Rotate the local minute hand based on the current local minute value.
    public void SetLocalMinHand(float timeScratch)
    {
        var timePercent = (timeScratch / 60) * 100;
        var currentRotation = 3.6f * timePercent;

        Quaternion targetTotalRotation = Quaternion.Euler(0f, currentRotation, 0f);
        localminHandPivot.transform.localRotation = targetTotalRotation;
    }

    public void updateLocalAndNationalTime()
    {

        //Get the current national time using the system clock.
        currentMinute = System.DateTime.Now.Minute;
        currentHour = System.DateTime.Now.Hour;

        //Once we have calculated the local minute difference, we can check whether this would tip us over into a new hour. Either way, a new 'local hour' and 'local minute' are calculated.
        calculateLocalMinuteDiff();
        var minuteToChange = currentMinute + currentLocalMinuteDifference;
        if (minuteToChange >= 60) {
            currentlocalHour = currentHour + 1;
            currentlocalMinute = minuteToChange - 60;
        } else
        {
            currentlocalMinute = minuteToChange;
            currentlocalHour = currentHour;
        }

    }

    //Calculate the difference (in minutes) between local time and national time. This is based on longitude: every degree of difference from 0 (Greenwich) corresponds to roughly four minutes.
    public void calculateLocalMinuteDiff()
    {
        currentLocalMinuteDifference = System.Math.Abs(tc.currentLong * 4);
    }


}

