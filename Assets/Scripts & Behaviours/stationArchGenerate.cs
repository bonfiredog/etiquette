     using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class stationArchGenerate : MonoBehaviour
{

    private textGenerationControl myTGC;
    private bool hasSet = false;
    // Start is called before the first frame update
    void Start()
    {
        myTGC = gameObject.GetComponent<textGenerationControl>();

    }

    // Update is called once per frame
    void Update()
    {
        if (hasSet == false)
        {
            var chanceToGenerate = Random.Range(0, 100);

            if (chanceToGenerate <= 40 && gameObject.tag != "centralarch")
            {
                myTGC.generateTextFromGrammar(myTGC.myText);
            }
            hasSet = true;
        }
    }
}
