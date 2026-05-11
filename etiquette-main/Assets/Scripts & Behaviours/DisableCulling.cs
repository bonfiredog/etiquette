using UnityEngine;

[ExecuteAlways]
public class DisableCulling : MonoBehaviour
{
    void Start()
    {
        MeshRenderer rend = GetComponent<MeshRenderer>();
        if (rend != null)
        {
            rend.localBounds = new Bounds(
                Vector3.zero,
                new Vector3(99999, 99999, 99999)
            );
        }
    }
}