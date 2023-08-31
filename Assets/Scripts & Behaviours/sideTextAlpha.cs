using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class sideTextAlpha : MonoBehaviour
{
    public Color originalColor;

    private TextMeshPro myTM;
    private float myCurrentAlpha;
    private cameraControl cc;



    // Start is called before the first frame update
    void Start()
    {
        myTM = gameObject.GetComponent<TextMeshPro>();
        cc = GameObject.Find("Main Camera").GetComponent<cameraControl>();
    }

    // Update is called once per frame
    void Update()
    {
        myCurrentAlpha = (255 / 100) * cc.outAmount;
        myTM.faceColor = new Color(originalColor.r, originalColor.g, originalColor.b, myCurrentAlpha);
    }
}
