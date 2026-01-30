using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshPro))]
public class TMPBoundsFixWorld : MonoBehaviour
{
    public float worldBoundsSize = 50f; // very large
    void LateUpdate()
    {
        var r = GetComponent<Renderer>();
        r.localBounds = new Bounds(Vector3.zero, Vector3.one * worldBoundsSize);
    }
}
 