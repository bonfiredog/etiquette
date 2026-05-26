using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class tunnelController : MonoBehaviour
{
  
    public float speed;
    public float topspeed;
    public string type;
    private TrainControl tc;
    private rockingController rocker;

    // Start is called before the first frame update
    void Start()
    {

        tc = GameObject.Find("trainController").GetComponent<TrainControl>();
        rocker = GameObject.Find("Rocker").GetComponent<rockingController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (type == "tunnel")
        {
            //Set the speed as a percentage of the train's current speed.
            speed = (topspeed / tc.trainTopSpeed) * tc.trainCurrentSpeed;
            speed = Mathf.Clamp(speed, 0, topspeed);
        } else
        {
            speed = topspeed;
            if (transform.position.z == 840.0f) {
                rocker.SuddenJolt(rocker.suddenJoltStrength * 1.5f);
            }
        }

        //Move text to the left at speed (slightly different from the main text controller as the object orientation is different).

        transform.position += new Vector3(0, 0, 1) * speed * Time.deltaTime;

        //If beyond a certain z value, destroy.
        if (transform.position.z > 80000)
        {
            Destroy(gameObject);
        }
    }
}
