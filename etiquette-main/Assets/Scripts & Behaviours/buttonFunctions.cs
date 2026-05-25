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
    public Button pausebutton;
    private GameObject scheduleobject;
    private StationScheduler ss;
    public GameObject pausenotifier;
    public Button effectbutton;
    
    private generateTrainTunnel tpgenerator;
    private rockingController rocker;
    private Flockaroo.ColoredPencilsEffect effect;

     void Start()
    {
        // Add listener to button click event
        tpbutton.onClick.AddListener(OnButtonClick);
        tunbutton.onClick.AddListener(OnTunButtonClick);
        joltbutton.onClick.AddListener(OnJoltButtonClick);
        pausebutton.onClick.AddListener(OnPauseButtonClick);
        effectbutton.onClick.AddListener(OnEffectButtonClick);
        tpgenerator = tp.GetComponent<generateTrainTunnel>();
        rocker = GameObject.Find("Rocker").GetComponent<rockingController>();
        delaybutton.onClick.AddListener(OnDelayButtonClick);
        scheduleobject = GameObject.Find("stationScheduleController");
        ss = scheduleobject.GetComponent<StationScheduler>();
        effect = GameObject.Find("Main Camera").GetComponent<Flockaroo.ColoredPencilsEffect>();
        
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

        void OnEffectButtonClick() {
        Debug.Log("Toggled The Effect!");
        if (effect.enabled == true) {
            effect.enabled = false;
        } else {
            effect.enabled = true;
        }
    }

    void OnPauseButtonClick() {
        if (Time.timeScale == 1) {
            Time.timeScale = 0;
            pausenotifier.SetActive(true);
        } else {
            Time.timeScale = 1;
            pausenotifier.SetActive(false);
        }
        Debug.Log("Triggered A Pause!");
       
    }



}
