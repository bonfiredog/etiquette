using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cameraControl : MonoBehaviour
{

    public Vector3 mousePos;
    public float moveTimerMax;
    public GameObject commandconsole;
    public GameObject window;
    public GameObject windowcollider;
    public GameObject paper;
    public float windowOpenSpeed;
    public float windowCloseSpeed;
    public float cameraRotateBackRate;
    public GameObject profilers;
    
   

    [HideInInspector]
    public float xRot;
    [HideInInspector]
    public float yRot;
    [HideInInspector]
    public Vector3 newRot;
    [HideInInspector]
    public float newX;
    [HideInInspector]
    public float newY;
    [HideInInspector]
    public bool pressing;
    [HideInInspector]
    public float originalX;
    [HideInInspector]
    public float originalY;
    
   
    [HideInInspector]
    private float moveRange;

 public float outSpeed;
    private float mpxPer;
    private float mpyPer;
    public float outAmount;
    private float moveTimer;
    private Vector3 previousMousePosition;
    private bool rotating;
    private windowPosition wp;


    // Start is called before the first frame update
    void Start()
    {
        //Set the initial position of the camera, for 'snapping back'.
       originalX = transform.localPosition.x;
       originalY = transform.localPosition.y;
       
       //Other Initial Values
       pressing = false;
       outAmount = 0;
       moveTimer = moveTimerMax;
       previousMousePosition = Input.mousePosition;
       rotating = false;
       wp = window.GetComponent<windowPosition>();

    }

    void Update()
    {
        //OPEN & CLOSE THE COMMAND CONSOLE w/ SHIFt+C

    if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Input.GetKeyDown(KeyCode.C))
    {
        if (commandconsole.activeSelf == false) {

        commandconsole.SetActive(true);
        profilers.SetActive(true);

        } else {
            commandconsole.SetActive(false);
            profilers.SetActive(false);
        }

    }






        //If the mouse is not being moved, lower the moveTimer.
        Vector3 currentMousePosition = Input.mousePosition;

        if (pressing == false)
        {
            if (currentMousePosition != previousMousePosition)
            {
                // Mouse is moving
                moveTimer = moveTimerMax;
                rotating = false;
                
            }
            else
            {
                // Mouse is not moving
                if (moveTimer >= 0)
                {
                    moveTimer -= 1 * Time.deltaTime;
                    rotating = false;
                }
                else
                {
                    //If the moveTimer reaches 0, start moving the camera back to the initial position.
                    rotating = true;
                }
            }
        } else
        {
            rotating = false;
        }

        previousMousePosition = currentMousePosition;


        //Move camera out of the train window gradually if pressing the mouse; if not, return to original position. But only if pointing at window!
        if (Input.GetMouseButton(0))
        {
            pressing = true;
        } else
        {
            pressing = false;
        }

        if (pressing == true)
        {
            if (outAmount < 100)
            {
                outAmount += outSpeed;
            }    

            if (wp.windowOpenAmount < 100)
            {
                wp.windowOpenAmount += windowOpenSpeed;
            }
        } else
        {
            if (outAmount > 0)
            {
                outAmount -= outSpeed;
            }

            if (wp.windowOpenAmount > 0)
            {
                wp.windowOpenAmount -= windowCloseSpeed;
            }
        } 
        outAmount = Mathf.Clamp(outAmount, 0, 100);

        newX = originalX + (240 / 100) * outAmount;
        newY = originalY + (10 / 100) * outAmount;

        transform.localPosition = new Vector3(newX, newY, transform.localPosition.z);


        //Rotate the camera based on mouse position: range of rotation is tied to current 'out of window' amount.

        if (rotating == false)
        {
            moveRange = 10f + ((100f / 100) * outAmount);

            mousePos = Input.mousePosition;
            mpxPer = (mousePos.x / Screen.width) * 100;
            mpyPer = (mousePos.y / Screen.height) * 100;

            xRot = moveRange / 100f * (mpxPer - 50f);
            yRot = moveRange / 100f * (mpyPer - 50f);
            yRot = Mathf.Clamp(yRot, -21, 44);
         

            newRot = new Vector3(-yRot, 90f + xRot, 0);
            Quaternion q = Quaternion.Euler(newRot);
            transform.rotation = q;
        } else
        {
            //Rotating the camera back to centre.
            Quaternion targetRotation = Quaternion.Euler(0, 90, 0);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, cameraRotateBackRate * Time.deltaTime);
        }
        }

}
