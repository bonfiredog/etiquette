using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class fixedGenerator : MonoBehaviour
{
    public TextMeshPro myText;
    public float timerMin;
    public TextAsset sentenceJSON;
    public float timerMax;
    public string myTag;
    public string grammarName;
    public float timerMulti;
    public GameObject mytext;
    public Vector2 anchorMin;
    public Vector2 anchorMax;
    public float xValue;

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
             myText.rectTransform.pivot = new Vector2(0, 0.5f);        // left-centre pivot
            myText.rectTransform.anchorMin = anchorMin;
            myText.rectTransform.anchorMax = anchorMax;
            myText.rectTransform.pivot = anchorMin;
        }
    }

    // Update is called once per frame
    void Update()
    {
      if (myTag == "window")
        {
           
                 if (stopped == false)
            {
                 if (tc.trainCurrentSpeed < 1 && (tc.docked == true || tc.delaying == true)) {
             if (timer > 0)
                    {
                    timer -= 1 * Time.deltaTime * timerMulti;
                    }  else
                    {
                        //Choose a sentence from sentences.json.
        string wrappedJSON = "{\"items\":" + sentenceJSON.text + "}";
SentenceEntry[] sentences = JsonUtility.FromJson<SentenceWrapper>(wrappedJSON).items;
string randomEntry = sentences[Random.Range(0, sentences.Length)].text;
string quotedEntry = $"...{randomEntry}...";
        myText.text = quotedEntry;
myText.ForceMeshUpdate();
float preferredWidth = myText.preferredWidth;
myText.rectTransform.sizeDelta = new Vector2(preferredWidth, myText.rectTransform.sizeDelta.y);
myText.rectTransform.anchoredPosition = new Vector2(xValue, myText.rectTransform.anchoredPosition.y);

                     
                        bend.RandomizePath();
                        bend.ApplyBend();
                        animate.Activate();
                        stopped = true;
                    }
            } 
            } else if (stopped == true) {
                if (tc.docked == false && tc.delaying == false) {
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
[System.Serializable]
public class SentenceEntry
{
    public string text;
    public float score;
    public string source_file;
    public int start_char;
    public int end_char;
}

[System.Serializable]
public class SentenceWrapper
{
    public SentenceEntry[] items;
}
    }


                  
                   
           