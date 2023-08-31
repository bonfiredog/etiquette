using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TrainControl : MonoBehaviour
{

    [HideInInspector]
    public float trainTopSpeedOriginal;
    [HideInInspector]
    public float maxLong;
    [HideInInspector]
    public float minLong;
    [HideInInspector]
    public float decelerationRate;
    [HideInInspector]
    public float decelerationStartTime;
    [HideInInspector]
    private float timeSinceDeceleration;
    [HideInInspector]
    private float decelerationProgress;
    [HideInInspector]
    public float decelerationTime;
    [HideInInspector]
    public float trainSlowDownStartSpeed;

    public float currentLong = 0;
    public float trainTopSpeed;
    public float trainCurrentSpeed;
    public bool docked = true;
    public bool docking = false;
    public float speedIncrease;
    public bool braking = false;
    public bool delaying = false;
    public float secondsToSlowGentle;
    public float secondsToSlowHard;
    public GameObject scheduler;

    private StationScheduler ss;
    private float delayTime;
    private float brakeSpeed;
   

   
    // Start is called before the first frame update
    void Start()
    {
        //Initial Values
        trainCurrentSpeed = 0;
        maxLong = -5.7f; //Land's End
        minLong = 0f; //Greenwich
        trainTopSpeedOriginal = trainTopSpeed; //For when the train's top speed is affected by weather or gradient, good to know what we originally set it to!
        ss = scheduler.GetComponent<StationScheduler>();
    }

    // Update is called once per frame
    void Update()
    {

        //Updating the current longitude based on the percentage of the distance between the previous and next station, whose longitudes are stored. 
        if (docked == false) {

            currentLong = ss.lastStationLong -
            (((ss.lastStationLong - ss.nextStationLong) / ss.nextStationMilesTotal) * (ss.nextStationMilesTotal - ss.milesToNextStation));
        } else
        {
            currentLong = ss.nextStationLong;
        }


        //TRAIN MOVEMENT
        //Make sure the train does not move when docked at a station. 
        if (docked == true)
        {
            trainCurrentSpeed = 0;
        }
        else
        {
            //If the train isn't docked, and isn't braking to dock, we can assume the train will try and reach its top speed.
            if (docking == false && delaying == false)
            {
                if (trainCurrentSpeed < trainTopSpeed)
                {
                    trainCurrentSpeed += speedIncrease * Time.deltaTime;
                }
            }
            else
            {
                //If Docking, if speed is more than 0, decelerate by the rate calculated.
                if (docking == true)
                {
                    timeSinceDeceleration = Time.time - decelerationStartTime;
                    decelerationProgress = Mathf.Clamp01(timeSinceDeceleration / decelerationTime);

                    if (trainCurrentSpeed > 0)
                    {
                        trainCurrentSpeed = Mathf.Lerp(trainSlowDownStartSpeed, 0f, decelerationProgress);
                    } else
                    {
                        //Set the miles to next station to 0, if the train has finished slowing. Else...
                        trainCurrentSpeed = 0;
                        ss.milesToNextStation = 0;
                    }
                } else
                {
                    //DELAYING
                    //You can only delay if you're not docking.
                    if (delaying == true)
                    {
                        //Slow down the train to 0.
                        if (trainCurrentSpeed > 0)
                        {
                            trainCurrentSpeed -= secondsToSlowHard * Time.deltaTime;
                        } else
                        {
                            //If stopped, keep it stopped.
                            trainCurrentSpeed = 0;

                            //Count down the delay timer.
                            if (ss.delayTimer > 0) {
                                ss.delayTimer -= 1 * Time.deltaTime;
                            } else
                            {
                                //When it's finished, create a delay object some random distance from the camera, with randomly generated text from the special delay grammar.
                                delaying = false;
                                var thisDelay = Instantiate(ss.delayObject);
                                var textc = thisDelay.GetComponent<textController>();
                                textc.topspeed = 50;
                                ss.delayObject.transform.position = new Vector3(750, -57f, Random.Range(-1000, -2000));
                                var textgen = ss.delayObject.GetComponent<textGenerationControl>();
                                textgen.gcScript = GameObject.Find("grammarController").GetComponent<traceGrammarControl>();
                                textgen.setGrammarForObject("delaygrammar");
                                textgen.generateTextFromGrammar(thisDelay.GetComponent<TextMeshPro>());

                                //Reset the delay timer.
                                ss.nextStationDelayTimer = Random.Range(ss.milesToNextStation * 0.25f, ss.milesToNextStation * 0.75f);
                            }

                        }
                    }
                }
            }


        }


        //Lock the train speed.

        if (trainCurrentSpeed < 0)
        {
            trainCurrentSpeed = 0;
        }

        if (trainCurrentSpeed > trainTopSpeed)
        {
            trainCurrentSpeed -= 0.4f;
        }


    }


    public float CalculateDecelerationRate(float trainspeed, float remainingDistance, float stoppingDistance)
    {
        float decelerationRate = (trainspeed * trainspeed) / (2 * remainingDistance - stoppingDistance);
        return decelerationRate;
    }
}
