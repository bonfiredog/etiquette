using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class textController : MonoBehaviour
{
    
    public float speed;
   
    public float topspeed;

    RectTransform myPos;
    private TrainControl tc;

    // Start is called before the first frame update
    void Start()
    {
        myPos = GetComponent<RectTransform>();
        tc = GameObject.Find("trainController").GetComponent<TrainControl>();
        
    }

    // Update is called once per frame
    void Update()
    {
        //Set the speed as a percentage of the train's current speed.
        speed = (topspeed / tc.trainTopSpeed) * tc.trainCurrentSpeed;
        speed = Mathf.Clamp(speed, 0, topspeed);


        //Move text to the left at speed.
       
            myPos.anchoredPosition3D += new Vector3(0, 0, 1) * speed * Time.deltaTime;
        

        //If beyond a certain z value, destroy.
        if (myPos.anchoredPosition3D.z > 10000)
        {
            Destroy(gameObject);
        }
        
    }
}
