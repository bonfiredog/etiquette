using TMPro;
using UnityEngine;

[ExecuteInEditMode]
public class CurveText : MonoBehaviour
{
    public float radius = 5f;
    public float arcAngle = 90f;
    [Range(0f, 1f)]
    public float curveAmount = 1f; // Blend between flat and curved
    
    private TMP_Text textMesh;
    private bool isDirty = true;
    
    void OnEnable()
    {
        textMesh = GetComponent<TMP_Text>();
        isDirty = true;
    }
    
    void Update()
    {
        // In edit mode, update when values change
        if (!Application.isPlaying)
        {
            if (isDirty || transform.hasChanged)
            {
                CurveTextMesh();
                isDirty = false;
                transform.hasChanged = false;
            }
        }
    }
    
    void OnValidate()
    {
        isDirty = true;
    }
    
    void Start()
    {
        if (Application.isPlaying)
        {
            CurveTextMesh();
        }
    }
    
void CurveTextMesh()
{
    if (textMesh == null)
        textMesh = GetComponent<TMP_Text>();
        
    textMesh.ForceMeshUpdate();
    TMP_TextInfo textInfo = textMesh.textInfo;
    
    for (int i = 0; i < textInfo.characterCount; i++)
    {
        if (!textInfo.characterInfo[i].isVisible) continue;
        
        int vertexIndex = textInfo.characterInfo[i].vertexIndex;
        int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
        Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;
        Vector3[] sourceVertices = textInfo.meshInfo[materialIndex].vertices;
        
        for (int j = 0; j < 4; j++)
        {
            Vector3 offset = sourceVertices[vertexIndex + j];
            // Use Y position instead of X for vertical curve
            float angle = (offset.y / radius) * Mathf.Deg2Rad;
            
            Vector3 curved = new Vector3(
                offset.x, // Keep X flat
                Mathf.Sin(angle) * radius, // Y becomes curved
                radius - Mathf.Cos(angle) * radius // Z provides depth
            );
            
            // Blend between original and curved
            vertices[vertexIndex + j] = Vector3.Lerp(offset, curved, curveAmount);
        }
    }
    
    textMesh.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
}
}