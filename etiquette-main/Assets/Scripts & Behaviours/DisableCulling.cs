using UnityEngine;

[ExecuteAlways]
public class DisableCulling : MonoBehaviour
{
    void Start()
    {
        GetComponent<Renderer>().bounds = new Bounds(
            Vector3.zero, 
            new Vector3(99999, 99999, 99999)
        );
    }
}