using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;



public class generateTrainTunnel : MonoBehaviour
{
    public float timerMin;
    public float timerMax;
    public traceGrammarControl textgen;
    public float assignedSpeed;
    public int assignedSize;
    public GameObject genObjectTunnel;
    public GameObject genObjectTrain;
    public float fontSizeLowerBound;
    public float fontSizeUpperBound;
    public float timer;
    public float timerMulti;
    public float overallChance;

    private Vector3 mypos;
    private TrainControl tc;
    private StationScheduler ss;
    private string toGen;
    GameObject thisTT;
   

    // Start is called before the first frame update
    void Start()
    {       //Initial Values
        mypos = transform.position;
        textgen = GameObject.Find("grammarController").GetComponent<traceGrammarControl>();
        tc = GameObject.Find("trainController").GetComponent<TrainControl>();
        ss = GameObject.Find("stationScheduleController").GetComponent<StationScheduler>();
        
        //Start the timer with a random total (limits set manually for the generator).
        timer = Random.Range(timerMin, timerMax);

      
    }


    // Update is called once per frame
    void Update()
    {
    //Run the timer.
    if (timer > 0)
        {

        timer -= ((1 / tc.trainTopSpeed) * tc.trainCurrentSpeed) * timerMulti * Time.deltaTime;
   
        }
    else
        {
            //Calculate the chance to generate something.
            var chance = Random.Range(0, 100);
            if (chance <= overallChance)
            {
                //Generate a tunnel.
                if (ss.milesToNextStation > 3) {
                generateTT("tunnel");
                }
            }
            timer = Random.Range(timerMin, timerMax);
        }
   }


    public void generateTT(string type)
    {
        TextMeshPro thistextmesh = null; 
        textGenerationControl thisTextGenerator = null; 
        textGenerationControl thisTextGeneratorName = null;

        Debug.Log("Generating a" + type + "! ================================|");
        if (type == "train") {
         thisTT = Instantiate(genObjectTrain);
        var correctGrammar = type + "grammar";
        //Generate the text randomly based on a Tracery grammar.
        thistextmesh = thisTT.transform.Find("text").GetComponent<TextMeshPro>();
        thisTextGenerator = thisTT.transform.Find("text").GetComponent<textGenerationControl>();
        thisTextGeneratorName = thisTT.transform.Find("tunnel name").GetComponent<textGenerationControl>();
        //Make sure the grammars are correct for each one, and generate some text. 
        thisTextGenerator.setGrammarForObject(correctGrammar);
        thisTextGenerator.generateTextFromGrammar(thistextmesh);

        } else {
         thisTT = Instantiate(genObjectTunnel);
        var correctGrammar = type + "grammar";
        var correctGrammarTunnelName = "tunnelnamegrammar";
        
        thistextmesh = thisTT.transform.Find("text").GetComponent<TextMeshPro>();
        thisTextGenerator = thisTT.transform.Find("text").GetComponent<textGenerationControl>();


        var thistextmeshname = thisTT.transform.Find("tunnel name").GetComponent<TextMeshPro>();
        thisTextGeneratorName = thisTT.transform.Find("tunnel name").GetComponent<textGenerationControl>();

        thisTextGenerator.setGrammarForObject(correctGrammar);
        thisTextGeneratorName.setGrammarForObject(correctGrammarTunnelName);
        thisTextGenerator.generateTextFromGrammar(thistextmesh);
        thisTextGeneratorName.generateTextFromGrammar(thistextmeshname);

        }                  

        //Assign a speed and fontSize, based on the generator, to the text object.
        var thistextscript = thisTT.gameObject.GetComponent<tunnelController>();
        thistextscript.speed = assignedSpeed;
        thistextscript.topspeed = assignedSpeed;
        thistextscript.type = type;
        thistextmesh.fontSize = Random.Range(fontSizeLowerBound, fontSizeUpperBound);

        var thistextrect = thisTT.transform.Find("text").GetComponent<RectTransform>();
        setWidthOfBox(thistextrect, thistextmesh.preferredWidth, thistextmesh.preferredHeight);

        //Set the size of the cube to correspond to the size of the text.
        var thisCube = thisTT.transform.Find("trainmass");
        float tmpWidth = thistextrect.sizeDelta.x;
        Vector3 cubeScale = thisCube.localScale;
        cubeScale.z = tmpWidth + (tmpWidth / 3);
        thisCube.localScale = cubeScale;

        //Set its position to the generator.
        thisTT.transform.position = mypos;
        thisTT.transform.position = new Vector3(914, mypos.y, mypos.x);
    }

    private void setWidthOfBox(RectTransform myRect, float textWidth, float textHeight)
    {
        myRect.sizeDelta = new Vector2(textWidth, textHeight);
    }

}
