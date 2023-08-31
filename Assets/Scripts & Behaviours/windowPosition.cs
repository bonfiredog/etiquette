using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class windowPosition : MonoBehaviour
{
    [HideInInspector]
    public float windowOpenAmount;

    private float closedY;
    private float openY;
    private float currentY;


    // Start is called before the first frame update
    void Start()
    {
        //Initial Values
        closedY = transform.localPosition.y;
        openY = 2000;
        currentY = closedY;
    }

    // Update is called once per frame
    void Update()
    {
        //Change the y position of the window, between closed and open, based on the windowOpenAmount.
        currentY = closedY - ((openY / 100) * windowOpenAmount);
        transform.localPosition = new Vector3(transform.localPosition.x, currentY, transform.localPosition.z);

        //Lock the windowOpenAmount & currentY;
        windowOpenAmount = Mathf.Clamp(windowOpenAmount, 0, 100);
        currentY = Mathf.Clamp(currentY, closedY - openY, closedY);
    }
}
