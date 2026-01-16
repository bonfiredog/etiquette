using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cameraControl : MonoBehaviour
{
    public Vector3 mousePos;
    private GameObject sec;
    public float moveTimerMax;
    public GameObject commandconsole;
    public GameObject window;
    public GameObject windowcollider;
    public GameObject paper;
    public float windowOpenSpeed;
    public float windowCloseSpeed;
    public float cameraRotateBackRate;
    public GameObject profilers;
    private bool holdingTimetable = false;
    public GameObject cursor;
    private DynamicCursor dc;
    private GameObject timetablefold;
    private GameObject timetable;
    private Vector3 tfreadypos = new Vector3 (-914.5f, -769.9f, -592.2f);
    private Vector3 ttreadypos = new Vector3 (167.2f, 2f, 43.1f);
    private Vector3 tforiginalpos;
    private Vector3 ttoriginalpos;
    public float tfspeed = 100.0f;
    public float ttspeed = 100.0f;
  
    [Header("Drag Settings")]
    [SerializeField] private float dragSensitivity = 0.5f; 
    [SerializeField] private float windowLerpSpeed = 10f; 
    private bool isDraggingHandle = false;
    private float initialMouseY;
    private float initialWindowOpenAmount;
    private float targetWindowAmount;

    private startEndController startcontrol;

    [HideInInspector] public float xRot;
    [HideInInspector] public float yRot;
    [HideInInspector] public Vector3 newRot;
    [HideInInspector] public float newX;
    [HideInInspector] public float newY;
    [HideInInspector] public bool pressing;
    [HideInInspector] public float originalX;
    [HideInInspector] public float originalY;
    [HideInInspector] private float moveRangex;
     [HideInInspector] private float moveRangey;

    public float outSpeed;
    private float mpxPer;
    private float mpyPer;
    public float outAmount;
    private float moveTimer;
    private Vector3 previousMousePosition;
    private bool rotating;
    private windowPosition wp;
    private float lookmod;
    public float lookmodset = 5.0f;

    void Start()
    {
       originalX = transform.localPosition.x;
       originalY = transform.localPosition.y;
       sec = GameObject.Find("startEndController");
       startcontrol = sec.GetComponent<startEndController>();
       dc = cursor.GetComponent<DynamicCursor>();
       
       pressing = false;
       outAmount = 0;
       moveTimer = moveTimerMax;
       previousMousePosition = Input.mousePosition;
       rotating = false;
       wp = window.GetComponent<windowPosition>();
       targetWindowAmount = wp.windowOpenAmount;
       timetablefold = GameObject.Find("timetablefold");
       timetable = GameObject.Find("TIMETABLE");
        tforiginalpos = timetablefold.transform.position;
        ttoriginalpos = timetable.transform.position;
        lookmod = 0.0f;

    }

    void Update()
    {
        if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Input.GetKeyDown(KeyCode.C))
        {
            commandconsole.SetActive(!commandconsole.activeSelf);
            profilers.SetActive(!profilers.activeSelf);
        }

        if (startcontrol.isStarted == true) {
            HandleInputAndRotation();
            HandleWindowAndLeaning();
            OpenAndCloseTimetable();
        }
    }


void OpenAndCloseTimetable() {

    //If hovering over timetablefold... and click...
    
    if (holdingTimetable == false) {
    if (Input.GetMouseButtonDown(0) && dc.currentTarget.name == "tfactual") {
        holdingTimetable = true;

        Debug.Log("holding!");
    }
    }

    if (holdingTimetable == true) {
        if (Input.GetMouseButton(0) == false) {
            holdingTimetable = false;
            Debug.Log("not holding.");
        }
    }

    if (holdingTimetable == true) {
        //Move timetablefold to 'ready position'
      timetablefold.transform.position =
    Vector3.MoveTowards(
        timetablefold.transform.position,
        tfreadypos,
        tfspeed * Time.deltaTime
    );

    

       timetable.transform.position =
    Vector3.MoveTowards(
        timetable.transform.position,
        ttreadypos,
        ttspeed * Time.deltaTime
    );

    lookmod = lookmodset;
    
    }
       

   //Move real timetable to 'ready position'


    if (holdingTimetable == false)  {

    //If release cursor...

    //Move back to 'start positions'.
         timetablefold.transform.position =
    Vector3.MoveTowards(
        timetablefold.transform.position,
        tforiginalpos,
        tfspeed * Time.deltaTime
    );



            timetable.transform.position =
    Vector3.MoveTowards(
        timetable.transform.position,
        ttoriginalpos,
        ttspeed * Time.deltaTime
    );
       lookmod = 0.0f;
    }
}

void HandleWindowAndLeaning()
{
    if (Input.GetMouseButtonDown(0))
    {
        if (dc.currentTarget != null && dc.currentTarget.name == "handle")
        {
            isDraggingHandle = true;
            dc.isGrabbed = true; // NEW: Lock cursor to handle
            initialMouseY = Input.mousePosition.y;
            initialWindowOpenAmount = wp.windowOpenAmount;
        }
    }

    if (Input.GetMouseButton(0) && isDraggingHandle)
    {
        pressing = true;

        // Update the cursor position to stick to the handle's current position
        // We convert the 3D handle position to 2D screen space
        dc.grabPosition = Camera.main.WorldToScreenPoint(dc.currentTarget.transform.position);

        if (outAmount < 1f) 
        {
            float mouseDeltaY = initialMouseY - Input.mousePosition.y;
            targetWindowAmount = Mathf.Clamp(initialWindowOpenAmount + (mouseDeltaY * dragSensitivity), 0, 100);
        }
        else 
        {
            targetWindowAmount = 100f;
        }

        wp.windowOpenAmount = Mathf.Lerp(wp.windowOpenAmount, targetWindowAmount, Time.deltaTime * windowLerpSpeed);

        if (wp.windowOpenAmount >= 98f) 
        {
            if (outAmount < 100) outAmount += outSpeed * Time.deltaTime * 50f;
        }
    }
    else
    {
        isDraggingHandle = false;
        dc.isGrabbed = false; // NEW: Unlock cursor
        pressing = false;

        if (wp.windowOpenAmount > 0) wp.windowOpenAmount -= windowCloseSpeed * Time.deltaTime * 10f;
        if (outAmount > 0) outAmount -= outSpeed * Time.deltaTime * 50f;
        
        targetWindowAmount = wp.windowOpenAmount;
    }

    // Hide cursor when leaning out
    dc.forceHide = (outAmount > 1f);

    outAmount = Mathf.Clamp(outAmount, 0, 100);
    wp.windowOpenAmount = Mathf.Clamp(wp.windowOpenAmount, 0, 100);
    transform.localPosition = new Vector3(originalX + (2.4f * outAmount), originalY + (0.1f * outAmount), transform.localPosition.z);
}

    void HandleInputAndRotation()
    {
        Vector3 currentMousePosition = Input.mousePosition;

        if (pressing == false)
        {
            if (currentMousePosition != previousMousePosition)
            {
                moveTimer = moveTimerMax;
                rotating = false;
            }
            else
            {
                if (moveTimer >= 0)
                {
                    moveTimer -= Time.deltaTime;
                    rotating = false;
                }
                else
                {
                    rotating = true;
                }
            }
        } 
        else
        {
            rotating = false;
        }

        previousMousePosition = currentMousePosition;

        if (rotating == false)
        {
            moveRangex = 12f + (1.0f * outAmount) + (lookmod);
            moveRangey = 12f + (1.0f * outAmount) + (lookmod * 2);
            mousePos = Input.mousePosition;
            mpxPer = (mousePos.x / Screen.width) * 100;
            mpyPer = (mousePos.y / Screen.height) * 100;

            xRot = moveRangex / 100f * (mpxPer - 50f);
            yRot = moveRangey / 100f * (mpyPer - 50f);
            yRot = Mathf.Clamp(yRot, -21, 44);
         
            newRot = new Vector3(-yRot, 90f + xRot, 0);
            transform.rotation = Quaternion.Euler(newRot);
        } 
        else
        {
            Quaternion targetRotation = Quaternion.Euler(0, 90, 0);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, cameraRotateBackRate * Time.deltaTime);
        }
    }
}