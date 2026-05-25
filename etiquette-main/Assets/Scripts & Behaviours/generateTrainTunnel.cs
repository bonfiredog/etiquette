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
    public textGenOneShot genscript;
   

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
    if (timer > 0f)
    {
        float speedRatio = tc.trainTopSpeed > 0f
            ? tc.trainCurrentSpeed / tc.trainTopSpeed
            : 0f;

        timer -= speedRatio * timerMulti * Time.deltaTime;
        return;
    }

    // timer expired — fire ONCE
    var chance = Random.Range(0, 100);
    if (chance <= overallChance && ss.milesToNextStation > 3)
    {
        if (tc.docking == false && tc.docked == false && tc.delaying == false && ss.milesToNextStation > 5) {

        var which = Random.Range(0, 100);
        if (which <= 50) {
            generateTT("train");
        } else {
            generateTT("tunnel");
        }
        
    } else {
generateTT("tunnel");
    }
    // reset and EXIT
    timer = Random.Range(timerMin, timerMax);
 
}
}


    public void generateTT(string type)
    {
        TextMeshPro thistextmesh = null; 
        textGenerationControl thisTextGenerator = null; 
        textGenerationControl thisTextGeneratorName = null;

        Debug.Log("Generating a " + type + "! ================================|");
        if (type == "train") {
         thisTT = Instantiate(genObjectTrain);
        var correctGrammar = type + "grammar";
        //Generate the text randomly based on a Tracery grammar.
        thistextmesh = thisTT.transform.Find("text").GetComponent<TextMeshPro>();
        thisTextGenerator = thisTT.transform.Find("text").GetComponent<textGenerationControl>();
        //Make sure the grammars are correct for each one, and generate some text. 
        thisTextGenerator.setGrammarForObject(correctGrammar);
        thisTextGenerator.generateTextFromGrammar(thistextmesh);

              //Assign a speed and fontSize, based on the generator, to the text object.
        var thistextscript = thisTT.gameObject.GetComponent<tunnelController>();
        thistextscript.speed = assignedSpeed * 1.4f;
        thistextscript.topspeed = assignedSpeed * 1.4f;
        thistextscript.type = type;
        thistextmesh.fontSize = Random.Range(fontSizeLowerBound, fontSizeUpperBound);

        } else {
  
        
        thisTT = Instantiate(genObjectTunnel);
      genscript = thisTT.transform.Find("tunnel name").GetComponent<textGenOneShot>();
        var correctGrammar = type + "grammar";
        var correctGrammarTunnelName = "tunnelnamegrammar";
                
        thistextmesh = thisTT.transform.Find("text").GetComponent<TextMeshPro>();
        thisTextGenerator = thisTT.transform.Find("text").GetComponent<textGenerationControl>();

        var thistextmeshname = thisTT.transform.Find("tunnel name").GetComponent<TextMeshPro>();
        thisTextGeneratorName = thisTT.transform.Find("tunnel name").GetComponent<textGenerationControl>();
        
              //Set the width of the tunnel randomly.
        genscript.length = Random.Range(1,8);
        var tunnel = thisTT.transform.Find("tunnel");
        tunnel.transform.localScale = new Vector3(5000,5000, 5000 + (1000 * genscript.length));
        thisTT.transform.Find("text").GetComponent<RectTransform>().sizeDelta = new Vector2(100 + (20 * genscript.length), 5);
        Vector3 currenttnpos = thisTT.transform.Find("tunnel name").GetComponent<RectTransform>().localPosition;
        thisTT.transform.Find("tunnel name").GetComponent<RectTransform>().localPosition = new Vector3(currenttnpos.x,currenttnpos.y,currenttnpos.z - (9 * genscript.length));
        thisTT.transform.position = new Vector3(0f, -5.91f, -9955f);

      //Assign a speed and fontSize, based on the generator, to the text object.
        var thistextscript = thisTT.gameObject.GetComponent<tunnelController>();
        thistextscript.speed = assignedSpeed;
        thistextscript.topspeed = assignedSpeed;
        thistextscript.type = type;
      
        thisTextGenerator.setGrammarForObject(correctGrammar);
        thisTextGeneratorName.setGrammarForObject(correctGrammarTunnelName);
        thisTextGenerator.generateTextFromGrammar(thistextmesh);
        thisTextGeneratorName.generateTextFromGrammar(thistextmeshname);

        }                  

  

      


        if (type == "train") {
        var thistextrect = thisTT.transform.Find("text").GetComponent<RectTransform>();
        setWidthOfBox(thistextrect, thistextmesh.preferredWidth, thistextmesh.preferredHeight);

        //Set the size of the cube to correspond to the size of the text.
        var thisCube = thisTT.transform.Find("trainmass");
        float tmpWidth = thistextrect.sizeDelta.x;
        Vector3 cubeScale = thisCube.localScale;
        cubeScale.z = tmpWidth + (tmpWidth / 3);
        thisCube.localScale = cubeScale;
        thisTT.transform.position = new Vector3(989f, -5.91f, -9955f);
        }

        
    }

    private void setWidthOfBox(RectTransform myRect, float textWidth, float textHeight)
    {
        myRect.sizeDelta = new Vector2(textWidth, textHeight);
    }

}
