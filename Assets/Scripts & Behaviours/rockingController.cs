using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class rockingController : MonoBehaviour
{

    public float udLower = 50;
    public float udHigher = 200;
    public float bfLower = 1200;
    public float bfHigher = 2000;

    public float lrLower = 50;
    public float lrHigher = 200;
    public float upBound = 20;
    public float bfBound = 7;
    public float lrBound = 2;
    public float upSpeed = 20;
    public float bfSpeed = 3;
    public float lrSpeed = 20;
    public float upDownTimer;
    public float backForthTimer;
    public float leftRightTimer;
    public float gravity = 10;

    private float jumpInAirAmount;
    private float jumpInAirSpeed;
    private float bfAmount;

    private Vector3 originalPos;
    private Quaternion originalRot;

    private TrainControl tc;
    private cameraControl cc;
    private bool movingLR;
    private float LRAmount;
    private float LRSpeed;
    private float originalBFPosition;
    private float zDir;

    private Vector3 minRotation;
    private Vector3 maxRotation;
    private bool willReset = false;


    // Start is called before the first frame update
    void Start()
    {
        tc = GameObject.Find("trainController").GetComponent<TrainControl>();
        cc = GameObject.Find("Main Camera").GetComponent<cameraControl>();
        originalPos = transform.position;
       originalRot = transform.rotation;
        originalBFPosition = transform.position.z;

        upDownTimer = Random.Range(udLower, udHigher);
        leftRightTimer = Random.Range(lrLower, lrHigher);
        backForthTimer = Random.Range(bfLower, bfHigher);
        movingLR = false;
        minRotation = new Vector3(0, 0, -lrBound);
        maxRotation = new Vector3(0, 0, lrBound);
    }

    // Update is called once per frame
    void Update()
    {


        //Back & Forth  
        //Count down the timer.

        if (cc.outAmount > 0 && tc.trainCurrentSpeed > (tc.trainTopSpeed * 0.25))
        {
            if (backForthTimer > 0)
            {
                backForthTimer -= 1;
            }
            else

            {
                bfAmount = Random.Range(bfBound * 0.25f, bfBound);

                //Reset the timer.
                backForthTimer = Random.Range(bfLower, bfHigher);
            }

            //Actual Jumping
            if (bfAmount > 0)
            {
                bfAmount -= bfSpeed;
                transform.position += new Vector3(0, 0, bfSpeed);
            }
            else
            {
                if (transform.position.z > originalBFPosition)
                {
                    transform.position -= new Vector3(0, 0, bfSpeed);
                }
            }


        }
        else
        {
            if (transform.position.z > originalBFPosition)
            {
                transform.position -= new Vector3(0, 0, bfSpeed);
            }
        }


        //Up & Down
        //Count down the timer.

        if (cc.outAmount > 0 && tc.trainCurrentSpeed > (tc.trainTopSpeed * 0.25))
        {
            if (upDownTimer > 0)
            {
                upDownTimer -= 1;
            }
            else

            {
                jumpInAirAmount = upBound;
                jumpInAirSpeed = upSpeed;

                //Reset the timer.
                upDownTimer = Random.Range(udLower, udHigher);
            }

            //Actual Jumping
            if (jumpInAirAmount > 0)
            {
                jumpInAirAmount -= jumpInAirSpeed;
                transform.position += new Vector3(0, jumpInAirSpeed, 0);
            }
            else
            {
                if (transform.position.y > originalPos.y)
                {
                    transform.position -= new Vector3(0, gravity, 0);
                }
            }


        } else
        {
            if (transform.position.y > originalPos.y)
            {
                transform.position -= new Vector3(0, gravity, 0);
            }
        }


    }
}
