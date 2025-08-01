using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class createText : MonoBehaviour
{ 
    public float timerMin;
    public float timerMax;
    public string myTag;
    public traceGrammarControl textgen;
    public float assignedSpeed;
    public int assignedSize;
    public GameObject text;
    public string grammarNameFront;
    public string grammarNameBack;
    public float distanceOfGeneratorFromWindow;
    public float fontSizeLowerBound;
    public float fontSizeUpperBound;
    public float yPosLowerBound;
    public float yPosUpperBound;
    public GameObject fc;
    public float timer;
    public float timerMulti;
    public bool showGrammar;
    public bool generateAtStart;
    public int numbertoGenerate;
    public float generateGap;
    private float generateBound;


    public Vector3 mypos;
    private TrainControl tc;
    private GameObject[] currentmembers;
    private bool shouldgenerate;
    private float timerMaxOriginal;
    private float timerMinOriginal;
    private StationScheduler ss;
    public float xBuffer = 50f;
    public string textType;

    private float modifier = 250;
    
    // Start is called before the first frame update
    void Start()
    {
        //Initial Values
        transform.position = new Vector3(transform.position.x, transform.position.y, distanceOfGeneratorFromWindow);
        mypos = transform.position;
        textgen = GameObject.Find("grammarController").GetComponent<traceGrammarControl>();
        tc = GameObject.Find("trainController").GetComponent<TrainControl>();
        ss = GameObject.Find("stationScheduleController").GetComponent<StationScheduler>();
        shouldgenerate = true;
        assignedSpeed = (assignedSpeed / 100) * modifier;
        generateBound = (generateGap / 10);
        

        timerMaxOriginal = timerMax;
        timerMinOriginal = timerMin;

     
        
        if (myTag == "frontgen" || myTag == "middlegen" || myTag == "sidegen")
        {
            timerMin = timerMinOriginal + (timerMinOriginal / 100) * (100 - ss.currentUrbanDensity);
            timerMax = timerMaxOriginal + (timerMaxOriginal / 100) * (100 - ss.currentUrbanDensity);
        }



        //At the start...
        if (generateAtStart) {
            for (int i = 1; i < numbertoGenerate; i++) {
                float NZ = 0;
                if (myTag != "trackgen") {
                var newBound = Random.Range(-generateBound, generateBound);
                var thisGap = generateGap + newBound;
                NZ = mypos.z + (thisGap * i);
                } else {
                NZ = mypos.z + (generateGap * i);
                }
                var thistrack = generateMyText(NZ);
                if (myTag == "trackgen") {
                    var thistextrect = thistrack.GetComponent<RectTransform>();
                    Vector2 currentSize = thistextrect.sizeDelta;
                    thistextrect.sizeDelta = new Vector2(20000f, currentSize.y);
                }
            }
        }









    }

    // Update is called once per frame
    void Update()
    {
        //Update timer max if necessary.
        if (myTag == "frontgen" || myTag == "middlegen" || myTag == "sidegen")
        {
            timerMin = timerMinOriginal + (timerMinOriginal / 100) * (100 - ss.currentUrbanDensity);
            timerMax = timerMaxOriginal + (timerMaxOriginal / 100) * (100 - ss.currentUrbanDensity);
        }


        //Count down this generator's timer, based on the train's current speed. So if the train's speed is 0, the timer won't go down.
        if (timer > 0)
        {
            timer -= ((1 / tc.trainTopSpeed) * tc.trainCurrentSpeed) * timerMulti * Time.deltaTime;
            
        } 
        else
        {
            //When the timer is at zero...
            
            //First of all, check whether you should generate a text:
            //- Are we currently docked?
            //- Are there any other texts on the line that would visually interfere? 
            shouldgenerate = true;
            currentmembers = GameObject.FindGameObjectsWithTag(myTag);
            if (myTag != "sidegen" && myTag != "trackgen") {
            //Working out whether we can generate...
            }
            

          
            //If we're fine to continue...
            if (shouldgenerate == true)                
            {
                    generateMyText(mypos.z);



         //Finally, reset the timer to a random total.
          timer = Random.Range(timerMin, timerMax);
            } else {
                  //Try again in a second.     
              timer = 2;
            }
        }
    }

    public GameObject generateMyText(float zvalue) {


        
              

          
                //Set the y position of the generator to a random position, within its prescribed bounds.
                transform.position = new Vector3(transform.position.x, Random.Range(yPosLowerBound, yPosUpperBound), transform.position.z);
                mypos = transform.position;
           
                //Create a new text object, and give it a tag so we know which generator it came from.
                var thistext = Instantiate(text);
                thistext.tag = myTag;

                if (myTag == "sidegen")
                {//Generate two lots of text for sidegens, one at the front and one at the back.
                    var thistextmesh1 = thistext.transform.Find("fronttext").GetComponent<TextMeshPro>();
                    var thistextmesh2 = thistext.transform.Find("backtext").GetComponent<TextMeshPro>();
                    var thisTextGenerator1 = thistext.transform.Find("fronttext").GetComponent<textGenerationControl>();
                    var thisTextGenerator2 = thistext.transform.Find("backtext").GetComponent<textGenerationControl>();
                    var width1 = thistextmesh1.rectTransform.rect.width;
                    var width2 = thistextmesh2.rectTransform.rect.width;

                    //Make sure the grammars are correct for each one, and generate some text. 
                    thisTextGenerator1.setGrammarForObject(grammarNameFront);
                    thisTextGenerator1.generateTextFromGrammar(thistextmesh1);
                    thisTextGenerator2.setGrammarForObject(grammarNameBack);
                    thisTextGenerator2.generateTextFromGrammar(thistextmesh2);

                    //Assign a speed and fontSize, based on the generator, to the text object.
                    var thistextscript = thistext.gameObject.GetComponent<textControllerSide>();
                    thistextscript.speed = assignedSpeed;
                    thistextscript.topspeed = assignedSpeed;

                    thistextmesh1.fontSize = Random.Range(fontSizeLowerBound,fontSizeUpperBound);
                    thistextmesh2.fontSize = Random.Range(fontSizeLowerBound, fontSizeUpperBound);


                    //Make sure they are the right width, and centre-aligned.

                    //Set its position to the generator. 
                    thistext.transform.position = mypos;

                }
                else
                { //For single-sided text.

                    //Generate the text randomly based on a Tracery grammar.
                    var thistextmesh = thistext.gameObject.GetComponent<TextMeshPro>();
                    var thisTextGenerator = thistext.gameObject.GetComponent<textGenerationControl>();

                    //Make sure the grammars are correct for each one, and generate some text. 
                    thisTextGenerator.setGrammarForObject(grammarNameFront);
                    thisTextGenerator.generateTextFromGrammar(thistextmesh);

                    //Assign a speed and fontSize, based on the geneator, to the text object.
                    var thistextscript = thistext.gameObject.GetComponent<textController>();
                    thistextscript.speed = assignedSpeed;
                    thistextscript.topspeed = assignedSpeed;
                    thistextmesh.fontSize = Random.Range(fontSizeLowerBound, fontSizeUpperBound);
                   
              

                    //Set its position to the generator.
                    var thistextrect = thistext.GetComponent<RectTransform>();
                    thistextrect.anchoredPosition3D = new Vector3(mypos.x, mypos.y, zvalue);

                

                }
            return thistext;
    }

    public void setWidthOfBox(RectTransform myRect, float textWidth, float textHeight)
    {
        myRect.sizeDelta = new Vector2(textWidth, textHeight);
    }

    private float GetTextWidth(TextMeshPro textmesh)
    {
        Bounds renderedBounds = textmesh.bounds;

        // Assuming the text object is not scaled or rotated, width is along the Z-axis
        float textWidth = renderedBounds.size.z;

        return textWidth;
    }
 private bool HasOverlappingObjectStandard(string tag)
    {
        // Find all objects with the specified tag
        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(tag);
        
        // Get generator's x position
        float generatorX = transform.position.x;
        
        // Define the check area bounds
        float leftBound = generatorX - xBuffer;
        float rightBound = generatorX + xBuffer;
        
        foreach (GameObject obj in taggedObjects)
        {
            // Check if object's Y position is within our prescribed range
            float objY = obj.transform.position.y;
            if (objY >= yPosLowerBound && yPosUpperBound <= yPosUpperBound)
            {
                // Check if object's X position is within our buffer zone
                float objX = obj.transform.position.x;
                if (objX >= leftBound && objX <= rightBound)
                {
                    return true; // Found overlapping object
                }
            }
        }
        
        return false; // No overlapping objects found
    }
    
    // Enhanced method that properly handles TextMeshPro objects
    private bool HasOverlappingObjectWithTextMeshPro(string tag)
    {
        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(tag);
        
        float generatorX = transform.position.x;
        float leftBound = generatorX - xBuffer;
        float rightBound = generatorX + xBuffer;
        
        foreach (GameObject obj in taggedObjects)
        {
            // Check for TextMeshPro components
            TextMeshPro tmp = obj.GetComponent<TextMeshPro>();
            TextMeshProUGUI tmpUI = obj.GetComponent<TextMeshProUGUI>();
            
            if (tmp != null)
            {
                // Use TextMeshPro's text bounds
                Bounds textBounds = tmp.textBounds;
                Vector3 worldCenter = obj.transform.TransformPoint(textBounds.center);
                Vector3 worldSize = Vector3.Scale(textBounds.size, obj.transform.lossyScale);
                
                // Check Y-axis overlap
                float textMinY = worldCenter.y - worldSize.y / 2f;
                float textMaxY = worldCenter.y + worldSize.y / 2f;
                
                if (textMinY <= yPosLowerBound && textMaxY >= yPosUpperBound)
                {
                    // Check X-axis overlap
                    float textMinX = worldCenter.x - worldSize.x / 2f;
                    float textMaxX = worldCenter.x + worldSize.x / 2f;
                    
                    if (textMinX <= rightBound && textMaxX >= leftBound)
                    {
                        return true;
                    }
                }
            }
            else if (tmpUI != null)
            {
                // For UI TextMeshPro, use RectTransform bounds
                RectTransform rectTransform = obj.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    Bounds uiBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(rectTransform);
                    Vector3 worldCenter = rectTransform.TransformPoint(uiBounds.center);
                    Vector3 worldSize = Vector3.Scale(uiBounds.size, rectTransform.lossyScale);
                    
                    // Check Y-axis overlap
                    float textMinY = worldCenter.y - worldSize.y / 2f;
                    float textMaxY = worldCenter.y + worldSize.y / 2f;
                    
                    if (textMinY <= yPosUpperBound && textMaxY >= yPosLowerBound)
                    {
                        // Check X-axis overlap
                        float textMinX = worldCenter.x - worldSize.x / 2f;
                        float textMaxX = worldCenter.x + worldSize.x / 2f;
                        
                        if (textMinX <= rightBound && textMaxX >= leftBound)
                        {
                            return true;
                        }
                    }
                }
            }
            else
            {
                // Regular collider check for non-TextMeshPro objects
                Collider objCollider = obj.GetComponent<Collider>();
                if (objCollider != null)
                {
                    Bounds bounds = objCollider.bounds;
                    
                    // Check Y-axis overlap
                    if (bounds.min.y <= yPosUpperBound && bounds.max.y >= yPosLowerBound)
                    {
                        // Check X-axis overlap
                        if (bounds.min.x <= rightBound && bounds.max.x >= leftBound)
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    // Fallback to transform position
                    float objY = obj.transform.position.y;
                    float objX = obj.transform.position.x;
                    
                    if (objY >= yPosLowerBound && objY <= yPosUpperBound && objX >= leftBound && objX <= rightBound)
                    {
                        return true;
                    }
                }
            }
        }
        
        return false;
    }
}
