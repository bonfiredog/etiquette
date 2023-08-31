using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class textControllerSide : MonoBehaviour
{
    [HideInInspector]
    public float speed;
    [HideInInspector]
    public float topspeed;
    private TrainControl tc;

    // Start is called before the first frame update
    void Start()
    {
       
        tc = GameObject.Find("trainController").GetComponent<TrainControl>();
    }

    // Update is called once per frame
    void Update()
    {
        //Set the speed as a percentage of the train's current speed.
        speed = (topspeed / tc.trainTopSpeed) * tc.trainCurrentSpeed;
        speed = Mathf.Clamp(speed, 0, topspeed);

        //Move text to the left at speed (slightly different from the main text controller as the object orientation is different).

        transform.position += new Vector3(0, 0, 1) * speed * Time.deltaTime;

        //If beyond a certain z value, destroy.
        if (transform.position.z > 2500)
        {
            Destroy(gameObject);
        }
    }
}
