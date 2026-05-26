using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class textController : MonoBehaviour
{
    
    public float speed;
   public System.Action onRelease;
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
     myPos.anchoredPosition3D += new Vector3(0, 0, 1) * speed * Time.deltaTime;
        

  if (tag == "fargen") {
        if (myPos.anchoredPosition3D.z > 25000) {
            if (onRelease != null) onRelease.Invoke(); // was Destroy(gameObject)
        }
    } else {
        if (myPos.anchoredPosition3D.z > 9000) {
            if (onRelease != null) onRelease.Invoke(); // was Destroy(gameObject)
        }
    }
        
    }
}
