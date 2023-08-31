using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class rockingController : MonoBehaviour
{

    public float udLower = 50;
    public float udHigher = 200;
    public float lrLower = 50;
    public float lrHigher = 200;
    public float upBound = 20;
    public float lrBound = 2;
    public float upSpeed = 20;
    public float lrSpeed = 20;
    public float upDownTimer;
    public float leftRightTimer;
    public float gravity = 10;

    private float jumpInAirAmount;
    private float jumpInAirSpeed;
    private Vector3 originalPos;
    private Quaternion originalRot;

    private TrainControl tc;
    private cameraControl cc;
    private bool movingLR;
    private float LRAmount;
    private float LRSpeed;
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

        upDownTimer = Random.Range(udLower, udHigher);
        leftRightTimer = Random.Range(lrLower, lrHigher);
        movingLR = false;
        minRotation = new Vector3(0, 0, -lrBound);
        maxRotation = new Vector3(0, 0, lrBound);
    }

    // Update is called once per frame
    void Update()
    {

        //Up & Down
        //Count down the timer.

     if (upDownTimer > 0) {
            upDownTimer -= 1;
        } else
        {
            //Jump in the air a random amount, at a random speed, according to the train's speed (and the out of the cab amount).
            jumpInAirAmount = ((Random.Range(0, upBound) / 100) * ((tc.trainCurrentSpeed / tc.trainTopSpeed) * 100)) / 100 * cc.outAmount;
            jumpInAirSpeed = (Random.Range(upSpeed * 0.25f, upSpeed) / 100) * ((tc.trainCurrentSpeed / tc.trainTopSpeed) * 100);

            //Reset the timer.
            upDownTimer = Random.Range(udLower, udHigher);
        }
        
     //Actual Jumping
     if (jumpInAirAmount > 0) {
            jumpInAirAmount -= jumpInAirSpeed;
            transform.position += new Vector3(0, jumpInAirSpeed, 0);
        } else
        {
            if (transform.position.y > originalPos.y)
            {
                transform.position -= new Vector3(0, gravity, 0);
            }
        }

        //Left & Right
        //Count down the timer, if not currently moving.

        //If out at all...
        if (cc.outAmount > 0)
        {
            if (movingLR == false)
            {
                if (leftRightTimer > 0)
                {
                    leftRightTimer -= 1;
                }
                else
                {
                    if (transform.rotation.z == 0)
                    {
                        var leftOrRight = Random.Range(0, 2);
                        if (leftOrRight == 0)
                        {
                            zDir = 1;
                        }
                        else if (leftOrRight == 1)
                        {
                            zDir = -1;
                        }

                        LRAmount = (((Random.Range(0, lrBound))) / 100) * (((tc.trainCurrentSpeed / tc.trainTopSpeed) * 100) / 100 * cc.outAmount);
                        LRSpeed = (Random.Range(lrSpeed * 0.25f, lrSpeed) / 100) * ((tc.trainCurrentSpeed / tc.trainTopSpeed) * 100);
                        leftRightTimer = Random.Range(lrLower, lrHigher);
                        movingLR = true;
                    } else
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.identity, 20 * Time.deltaTime);
                        willReset = true;
                    }
                 
                    if (willReset == true && transform.rotation.z == 0)
                    {
                        leftRightTimer = Random.Range(lrLower, lrHigher);
                        willReset = false;
                    }
                }
            }

            if (movingLR == true)
            {
                if (LRAmount > 0)
                {
                    LRAmount -= LRSpeed;
                    transform.Rotate(transform.rotation.x, transform.rotation.y, transform.rotation.z + (LRSpeed * zDir));
                }
                else
                {
                    movingLR = false;
                }
            }

        } else
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.identity, 20 * Time.deltaTime);
        }


     //Clamping Rotation
        // Get the current Euler angles of the object
        Vector3 currentRotation = transform.rotation.eulerAngles;

        float clampedZ = Mathf.Clamp(currentRotation.z, minRotation.z, maxRotation.z);

        // Set the clamped Euler angles back to the transform rotation
        transform.rotation = Quaternion.Euler(0, 0, clampedZ);



    }
}
