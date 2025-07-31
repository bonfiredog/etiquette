using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class buttonFunctions : MonoBehaviour
{

    public Button tpbutton;
    public GameObject tp;

    public Button tunbutton;
    

    private generateTrainTunnel tpgenerator;

     void Start()
    {
        // Add listener to button click event
        tpbutton.onClick.AddListener(OnButtonClick);
        tunbutton.onClick.AddListener(OnTunButtonClick);
        tpgenerator = tp.GetComponent<generateTrainTunnel>();
        
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


}
