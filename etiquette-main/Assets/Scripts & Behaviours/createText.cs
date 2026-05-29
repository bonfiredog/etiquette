using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Pool;

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
    
    
private ObjectPool<GameObject> _pool;

    public Vector3 mypos;
    private TrainControl tc;
    private GameObject[] currentmembers;
    private bool shouldgenerate;
    private float timerMaxOriginal;
    private float timerMinOriginal;
    private StationScheduler ss;
    public float xBuffer = 50f;
    public string textType;
   public static GameObject delayGen;
   private float lastUrbanDensity = -1f;

    private float modifier = 250;
    
    // Start is called before the first frame update
    void Awake()
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


          _pool = new ObjectPool<GameObject>(
        createFunc: () => Instantiate(text),
        actionOnGet: obj => obj.SetActive(true),
        actionOnRelease: obj => obj.SetActive(false),
        actionOnDestroy: obj => Destroy(obj),
        defaultCapacity: 10,
        maxSize: 50
    );
        

        timerMaxOriginal = timerMax;
        timerMinOriginal = timerMin;
            timer = 0.01f;
     
        
        if (myTag == "frontgen" || myTag == "middlegen" || myTag == "sidegen")
        {

            //COME BACK TO THIS LATER
           // timerMin = timerMinOriginal + (timerMinOriginal / 100) * (100 - ss.currentUrbanDensity);
           // timerMax = timerMaxOriginal + (timerMaxOriginal / 100) * (100 - ss.currentUrbanDensity);
        }



        //At the start...
       if (generateAtStart) {
            for (int i = 0; i < numbertoGenerate; i++) {
                float NZ = 0;
                if (myTag != "trackgen") {
                var newBound = Random.Range(-generateBound, generateBound);
                var thisGap = generateGap + newBound;
                NZ = mypos.z + (thisGap * i);
                } else {
                NZ = mypos.z + (generateGap * i);
                }
                
                if (myTag == "trackgen") {
                    var thistrack = generateMyText(NZ, "track");
                    var thistextrect = thistrack.GetComponent<RectTransform>();
                    thistextrect.localScale = new Vector3(0.7f,0.7f,0.7f);
                    //Vector2 currentSize = thistextrect.sizeDelta;
                    //thistextrect.sizeDelta = new Vector2(20000f, currentSize.y);
                } else {
                    //var thistrack = generateMyText(NZ, "loadup");
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
           if (ss.currentUrbanDensity != lastUrbanDensity) {
        lastUrbanDensity = ss.currentUrbanDensity;
        //timerMin = timerMinOriginal + (timerMinOriginal / 100) * (100 - ss.currentUrbanDensity);
        //timerMax = timerMaxOriginal + (timerMaxOriginal / 100) * (100 - ss.currentUrbanDensity);
    }
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
              //Also only generate if the delay is either non-existent, or it has passed a certain z value. Also not while delaying.
            if (myTag != "trackgen") {
            if (tc.docked == false && tc.trainCurrentSpeed > 0 && (delayGen == null || (delayGen != null && delayGen.transform.position.z > -3000)) && tc.delaying == false) {
                    generateMyText(mypos.z, myTag);
            }

         //Finally, reset the timer to a random total.
          timer = Random.Range(timerMin, timerMax);
        } else {
            if (tc.docked == false && tc.trainCurrentSpeed > 0) {
                     generateMyText(mypos.z, myTag);
            }
            timer = Random.Range(timerMin, timerMax);
        }
        }
        }
    

    public GameObject generateMyText(float zvalue, string myname) {
  
                //Set the y position of the generator to a random position, within its prescribed bounds.
                transform.position = new Vector3(transform.position.x, Random.Range(yPosLowerBound, yPosUpperBound), transform.position.z);
                mypos = transform.position;
           
                //Create a new text object, and give it a tag so we know which generator it came from.
                //var thistext = Instantiate(text);
                var thistext = _pool.Get();


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
                    thisTextGenerator1.setGrammarForObject(grammarNameFront, textgen);
thisTextGenerator1.generateTextFromGrammar(thistextmesh1, ss);
thisTextGenerator2.setGrammarForObject(grammarNameBack, textgen);
thisTextGenerator2.generateTextFromGrammar(thistextmesh2, ss);

                    //Assign a speed and fontSize, based on the generator, to the text object.
                    var thistextscript = thistext.gameObject.GetComponent<textControllerSide>();
                    thistextscript.speed = assignedSpeed;
                    thistextscript.topspeed = assignedSpeed;
                    thistextscript.onRelease = () => _pool.Release(thistext); 

                    thistextmesh1.fontSize = Random.Range(fontSizeLowerBound,fontSizeUpperBound);
                    thistextmesh2.fontSize = Random.Range(fontSizeLowerBound, fontSizeUpperBound);


                    //Make sure they are the right width, and centre-aligned.

                    //Set its position to the generator. 
                    thistext.transform.position = mypos;

                } else if (myTag == "trackgen") {
                    //Assign a speed and fontSize, based on the geneator, to the text object.
                    var thistextscript = thistext.gameObject.GetComponent<textController>();
                    thistextscript.speed = assignedSpeed;
                    thistextscript.topspeed = assignedSpeed;
                    thistextscript.onRelease = () => _pool.Release(thistext); // add this line
                   
                    //Set its position to the generator.
                    var thistextrect = thistext.GetComponent<RectTransform>();
                    thistextrect.anchoredPosition3D = new Vector3(mypos.x, mypos.y, zvalue);
                    thistextrect.localScale = new Vector3(0.7f, 0.7f, 0.7f);

                }
                else
                { //For single-sided text.
                    if (myTag != "trackgen") {
                    //Generate the text randomly based on a Tracery grammar.
                    var thistextmesh = thistext.gameObject.GetComponent<TextMeshPro>();
                    var thisTextGenerator = thistext.gameObject.GetComponent<textGenerationControl>();

                    //Make sure the grammars are correct for each one, and generate some text. 
                    thisTextGenerator.setGrammarForObject(grammarNameFront, textgen);
thisTextGenerator.generateTextFromGrammar(thistextmesh, ss);
                     thistextmesh.fontSize = Random.Range(fontSizeLowerBound, fontSizeUpperBound);
                   
                    }
                    //Assign a speed and fontSize, based on the geneator, to the text object.
                    var thistextscript = thistext.gameObject.GetComponent<textController>();
                    thistextscript.speed = assignedSpeed;
                    thistextscript.topspeed = assignedSpeed;
                   
                    //Set its position to the generator.
                    var thistextrect = thistext.GetComponent<RectTransform>();
                    thistextrect.anchoredPosition3D = new Vector3(mypos.x, mypos.y, zvalue);

                

                }
                thistext.name = myname;
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
            if (objY >= yPosLowerBound && objY <= yPosUpperBound)
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
