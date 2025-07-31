using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class fixedGenerator : MonoBehaviour
{
    public TextMeshPro myText;
    public float timerMin;
    public float timerMax;
    public string myTag;
    public string grammarName;
    public float timerMulti;

    private float timer;
    private TrainControl tc;
    private bool stopped = false; 

    // Start is called before the first frame update
    void Start()
    {
        //Void these generators at the start.
        myText.text = "";
        timer = Random.Range(timerMin, timerMax);

        if (myTag == "window")
        {
             tc = GameObject.Find("trainController").GetComponent<TrainControl>();
        }
    }

    // Update is called once per frame
    void Update()
    {
      if (myTag == "window")
        {
            if (stopped == false)
            {
                if (tc.trainCurrentSpeed < 2)
                {
                    if (timer > 0)
                    {
                        timer -= 1 * Time.deltaTime * timerMulti;
                    }
                    else
                    {
                        //Generate
                        var thisTextGen = myText.GetComponent<textGenerationControl>();
                        thisTextGen.setGrammarForObject(grammarName);
                        thisTextGen.generateTextFromGrammar(myText);
                        stopped = true;
                    }
                }
            } else
            {
                if (tc.trainCurrentSpeed > 3)
                {
                    stopped = false;
                    timer = Random.Range(timerMin, timerMax);
                    myText.text = "";
                }
            }
           
        }
        
    }
}
