using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class commandConsole : MonoBehaviour {
    
    public TextMeshProUGUI myText;
    private TrainControl tc;
    private StationScheduler ss;


    // Start is called before the first frame update
    void Start()
    {
        myText = gameObject.GetComponent<TextMeshProUGUI>();
        tc = GameObject.Find("trainController").GetComponent<TrainControl>();
        ss = GameObject.Find("stationScheduleController").GetComponent<StationScheduler>();
    }

    // Update is called once per frame
    void Update()
    {

        //Every frame, update the readout.
        myText.text = $"etiquette v. 1.0.0.1<br>Last Station: {ss.lastStationName}<br>Next Station: {ss.nextStationName}<br>Meters: {ss.milesToNextStation}<br>Current Speed: {tc.trainCurrentSpeed}<br>Current Terrain: {ss.currentTerrain}<br>Current Weather: {ss.currentweather}<br>Current Season: {ss.currentseason}<br>Current Month: {ss.currentmonth}";     
             
        
    }
}
