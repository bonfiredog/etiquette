using TMPro;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(TextMeshPro))]
public class border : MonoBehaviour
{
    [SerializeField] private Transform borderQuad;
    [SerializeField] private Vector2 padding = new Vector2(0.1f, 0.1f);

    private TextMeshPro _tmp;

    void Awake() => _tmp = GetComponent<TextMeshPro>();

    void LateUpdate()
    {
        if (_tmp == null) return;
        UpdateBorder();
    }

    // Catches changes made in the Inspector while in edit mode
    void OnValidate()
    {
        if (_tmp == null) _tmp = GetComponent<TextMeshPro>();
        // Delay one frame — OnValidate can fire before TMP rebuilds its mesh
        UnityEditor.EditorApplication.delayCall += UpdateBorder;
    }

private void UpdateBorder()
{
    if (borderQuad == null || _tmp == null) return;

    _tmp.ForceMeshUpdate();
    Bounds b = _tmp.textBounds;

    borderQuad.localPosition = b.center;
    borderQuad.localScale = new Vector3(
        b.size.x + padding.x,
        b.size.y + padding.y,
        1f
    );
}
}