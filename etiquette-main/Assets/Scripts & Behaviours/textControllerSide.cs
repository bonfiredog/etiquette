using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class textControllerSide : MonoBehaviour
{
    [HideInInspector]
    public float speed;
    [HideInInspector]
    public float topspeed;
    public System.Action onRelease;
    private TrainControl tc;

    void Start()
    {
        tc = GameObject.Find("trainController").GetComponent<TrainControl>();
    }

    void Update()
    {
        speed = (topspeed / tc.trainTopSpeed) * tc.trainCurrentSpeed;
        speed = Mathf.Clamp(speed, 0, topspeed);
        transform.position += new Vector3(0, 0, 1) * speed * Time.deltaTime;

        if (transform.position.z > 6000)
        {
            if (onRelease != null) onRelease.Invoke();
        }
    }
}