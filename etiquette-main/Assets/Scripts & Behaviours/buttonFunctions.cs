using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class buttonFunctions : MonoBehaviour
{

    public Button tpbutton;
    public GameObject tp;
    public Button joltbutton;
    public Button tunbutton;
    public Button delaybutton;
    private GameObject scheduleobject;
    private StationScheduler ss;
    
    private generateTrainTunnel tpgenerator;
    private rockingController rocker;

     void Start()
    {
        // Add listener to button click event
        tpbutton.onClick.AddListener(OnButtonClick);
        tunbutton.onClick.AddListener(OnTunButtonClick);
        joltbutton.onClick.AddListener(OnJoltButtonClick);
        tpgenerator = tp.GetComponent<generateTrainTunnel>();
        rocker = GameObject.Find("Rocker").GetComponent<rockingController>();
        delaybutton.onClick.AddListener(OnDelayButtonClick);
        scheduleobject = GameObject.Find("stationScheduleController");
        ss = scheduleobject.GetComponent<StationScheduler>();
        
    }

      void OnButtonClick()
    {
        Debug.Log("TP Button was clicked!");
        tpgenerator.generateTT("train");
        
    }

    void OnTunButtonClick() {
        Debug.Log("Tun Button was clicked!");
        tpgenerator.generateTT("tunnel");
    }

    void OnJoltButtonClick() {
        Debug.Log("Jolted!");
        rocker.SuddenJolt();
    }

    void OnDelayButtonClick() {
        Debug.Log("Triggered A Delay!");
        ss.delayDecide(true);
    }



}
