using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshPro))]
public class ZThickness : MonoBehaviour
{
    [SerializeField] private float zThickness = 5f;

    void Start() => FixBounds();

    #if UNITY_EDITOR
    void OnValidate() => FixBounds();  // updates in editor too, not just runtime!
    #endif

    void FixBounds()
    {
        var mr = GetComponent<MeshRenderer>();
        if (mr == null) return;
        var b = mr.bounds;
        mr.bounds = new Bounds(b.center, new Vector3(b.size.x, b.size.y, zThickness));
    }
}