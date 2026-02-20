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
    public GameObject mytext;

    private float timer;
    private TrainControl tc;
    private bool stopped = false; 
    private BendyTextPath bend;
    private BendyTextAnimator animate;



    // Start is called before the first frame update
    void Start()
    {
        //Void these generators at the start.
        myText.text = "";
        timer = Random.Range(timerMin, timerMax);

        if (myTag == "window")
        {
             tc = GameObject.Find("trainController").GetComponent<TrainControl>();
             bend = mytext.GetComponent<BendyTextPath>();
             animate = mytext.GetComponent<BendyTextAnimator>();
        }
    }

    // Update is called once per frame
    void Update()
    {
      if (myTag == "window")
        {
           
                 if (stopped == false)
            {
                 if (tc.trainCurrentSpeed < 1 && tc.docked == true) {
             if (timer > 0)
                    {
                        timer -= 1 * Time.deltaTime * timerMulti;
                    }  else
                    {
                        //Regenerate the text, path bend, and animate
                        var thisTextGen = myText.GetComponent<textGenerationControl>();
                        thisTextGen.setGrammarForObject(grammarName);
                        thisTextGen.generateTextFromGrammar(myText);
                        bend.RandomizePath();
                        bend.ApplyBend();
                        animate.Activate();
                        stopped = true;
                    }
            } 
            } else if (stopped == true) {
                if (tc.docked == false) {
                    //Retract the text, set the "".
                    animate.ReverseActivate();

                    Invoke("resetText", 3);
                    stopped = false;
                     timer = Random.Range(timerMin, timerMax);
                }
            }  
        }
        }

        void resetText() {
            myText.text = "";
        }
    }


                  
                   
           