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

    private Vector3 mypos;
    private TrainControl tc;
    private GameObject[] currentmembers;
    private bool shouldgenerate;
    private float timerMaxOriginal;
    private float timerMinOriginal;
    private StationScheduler ss;

    
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

        timerMaxOriginal = timerMax;
        timerMinOriginal = timerMin;

     
        
        if (myTag == "frontgen" || myTag == "middlegen" || myTag == "sidegen")
        {
            timerMin = timerMinOriginal + (timerMinOriginal / 100) * (100 - ss.currentUrbanDensity);
            timerMax = timerMaxOriginal + (timerMaxOriginal / 100) * (100 - ss.currentUrbanDensity);
        }


        //Start the timer with a random total (limits set manually for the generator).
        timer = Random.Range(timerMin, timerMax);
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
                   
                    thistextmesh.alignment = TextAlignmentOptions.Center;

                    //Set its position to the generator.
                    var thistextrect = thistext.GetComponent<RectTransform>();
                    setWidthOfBox(thistextrect, thistextmesh.preferredWidth, thistextmesh.preferredHeight);
                    thistextrect.anchoredPosition3D = mypos;

                

                }
            }
            //Finally, reset the timer to a random total.
            timer = Random.Range(timerMin, timerMax);
        }


    }

    private void setWidthOfBox(RectTransform myRect, float textWidth, float textHeight)
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
}
