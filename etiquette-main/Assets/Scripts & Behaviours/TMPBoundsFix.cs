using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshPro))]
public class TMPBoundsFix : MonoBehaviour
{
    void LateUpdate()
    {
        var r = GetComponent<Renderer>();
        r.localBounds = new Bounds(Vector3.zero, Vector3.one * 9f);
    }
}
