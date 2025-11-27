using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class startEndController : MonoBehaviour
{


    private StationScheduler ss;
    private TrainControl tc;
    private float endTimer1 = 180;
    private float endTimer2 = 15;
    public bool isStarted = false;
    private GameObject ttstart;
    private GameObject ttquit;


    // Start is called before the first frame update
    void Start()
    {
        

        ss = GameObject.Find("stationScheduleController").GetComponent<StationScheduler>();
        tc = GameObject.Find("trainController").GetComponent<TrainControl>();
        ttstart = GameObject.Find("startbutton");
        ttquit = GameObject.Find("quitbutton");
        


    }

    // Update is called once per frame
    void Update()
    {

        if (ss.nextStationName == "London Paddington" && tc.docked == true && ss.milesToNextStation <= 1) {
            if (ss.ending == false){ss.ending = true;}
            if (endTimer1 > 0 ){
                endTimer1 -= 1 * Time.deltaTime;
            } else {
                SceneManager.LoadScene(0);
            }
        }
       

    }
}
