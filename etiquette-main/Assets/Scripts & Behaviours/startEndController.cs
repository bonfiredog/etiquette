using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class startEndController : MonoBehaviour
{

    public GameObject endCredits;
    private StationScheduler ss;
    private TrainControl tc;
    private float endTimer1 = 15;
    private float endTimer2 = 15;

    // Start is called before the first frame update
    void Start()
    {
        //Disable Ending Sequence
        endCredits.SetActive(false);
        ss = GameObject.Find("stationScheduleController").GetComponent<StationScheduler>();
        tc = GameObject.Find("trainController").GetComponent<TrainControl>();

    }

    // Update is called once per frame
    void Update()
    {

        //ENDING
        if (ss.nextStationName == "London Paddington" && tc.docked == true && ss.milesToNextStation <= 1)
        {
            if (ss.ending == false) { ss.ending = true; }
            if (endTimer1 > 0)
            {
                endTimer1 -= 1 * Time.deltaTime;
            } else
            {
                endCredits.SetActive(true);
                if (endTimer2 > 0) {
                    endTimer2 -= 1 * Time.deltaTime;
                } else
                {
                    SceneManager.LoadScene(0);
                }
            }
        }
        
       

    }
}
