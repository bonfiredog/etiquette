using UnityEngine;
using TMPro;
using System.Collections;

[RequireComponent(typeof(TMP_Text))]
public class BendyTextPath : MonoBehaviour
{
    [Header("Path Settings")]
    [Tooltip("Vertical bend as a multiple of text height. 3 = moderate, 6+ = near 90 degrees")]
    [Range(0f, 10f)]
    public float bendStrength = 4f;

    [Tooltip("Horizontal bend as a multiple of text height")]
    [Range(0f, 5f)]
    public float horizontalBendStrength = 1f;

    [Tooltip("Rotate each character to follow the curve tangent")]
    public bool rotateCharactersToPath = true;

    [Tooltip("Randomize the path on start")]
    public bool randomizeOnStart = true;

    [Header("Bend Points (0-1 along text length)")]
    [Range(0f, 1f)]
    public float firstBendPosition = 0.33f;

    [Range(0f, 1f)]
    public float secondBendPosition = 0.66f;

    [Header("Debug")]
    public bool showGizmos = true;

    // Bend offsets in world units (set by InitializePath, scaled to text height)
    [HideInInspector] public float[] bendHeights     = new float[2];
    [HideInInspector] public float[] bendHorizontals = new float[2];

    private TMP_Text textMesh;
    private float cachedTextLength;
    private float cachedTextHeight;
    private float cachedMinX;

    void Start()
    {
        textMesh = GetComponent<TMP_Text>();
        StartCoroutine(InitializeAfterFrame());

        //Randomise text rotation.
         float randomZ = Random.Range(-15, 15);
        transform.rotation = Quaternion.Euler(
            transform.eulerAngles.x,
            transform.eulerAngles.y,
            randomZ
        );
    }

    IEnumerator InitializeAfterFrame()
    {
        yield return null;

        if (randomizeOnStart)
            RandomizePath();
        else
            InitializePath();

        ApplyBend();
    }

    // ---------------------------------------------------------------
    //  Public API
    // ---------------------------------------------------------------

    public void RandomizePath()
    {
        firstBendPosition      = Random.Range(0.1f, 0.2f);
        secondBendPosition     = Random.Range(0.7f, 0.9f);
  bendStrength           = Random.Range(0.05f, 0.12f);  // much gentler
    horizontalBendStrength = Random.Range(0.02f, 0.06f);;

        InitializePath();
        ApplyBend();
    }

    /// Place a single character at an arbitrary t on the path, given its pre-bend vertices.
    /// Uses exactly the same logic as ApplyBend so the result at t==finalT is identical.
    public void PlaceCharacterAtT(TMP_TextInfo textInfo, int charIndex,
                                  Vector3[] preBendCharVerts, float t)
    {
        int materialIndex = textInfo.characterInfo[charIndex].materialReferenceIndex;
        int vertexIndex   = textInfo.characterInfo[charIndex].vertexIndex;

        // Pre-bend bottom-centre (flat baseline)
        Vector3 bottomCentre = (preBendCharVerts[0] + preBendCharVerts[3]) * 0.5f;

        // Target position: original X kept, path offset added on top — identical to ApplyBend
        Vector3 pathOffset = CalculatePathOffset(t);
        Vector3 targetBase = new Vector3(bottomCentre.x + pathOffset.x,
                                         bottomCentre.y + pathOffset.y,
                                         bottomCentre.z);

        float angle = 0f;
        if (rotateCharactersToPath)
        {
            Vector3 tangent = GetPathTangent(t);
            angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
        }

        Quaternion rotation  = Quaternion.Euler(0, 0, angle);
        Vector3[]  vertices  = textInfo.meshInfo[materialIndex].vertices;

        for (int j = 0; j < 4; j++)
        {
            Vector3 local         = preBendCharVerts[j] - bottomCentre;
            vertices[vertexIndex + j] = targetBase + rotation * local;
        }

        textInfo.meshInfo[materialIndex].mesh.vertices = vertices;
        UpdateGeometry(textInfo.meshInfo[materialIndex].mesh, materialIndex);
    }

    void UpdateGeometry(Mesh mesh, int materialIndex)
    {
        textMesh.UpdateGeometry(mesh, materialIndex);
    }

    public void ApplyBend()
    {
        if (textMesh == null) return;

        textMesh.ForceMeshUpdate();
        TMP_TextInfo textInfo = textMesh.textInfo;
        if (textInfo.characterCount == 0) return;

        CacheTextMetrics(textInfo);
        if (cachedTextLength <= 0) return;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;

            int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
            int vertexIndex   = textInfo.characterInfo[i].vertexIndex;

            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

            // Bottom-centre of this character = the anchor point on the baseline
            Vector3 v0           = vertices[vertexIndex + 0]; // bottom-left
            Vector3 v3           = vertices[vertexIndex + 3]; // bottom-right
            Vector3 bottomCentre = (v0 + v3) * 0.5f;

            // t is simply the character's original x position mapped 0-1 across the text
            float charT = Mathf.Clamp01((bottomCentre.x - cachedMinX) / cachedTextLength);

            // World-space position this character's baseline should sit at on the path.
            // The path offset is added ON TOP of the original x position — keeping
            // the horizontal spacing exactly as TMP laid it out, only adding the
            // vertical (and optional horizontal) deflection.
            Vector3 pathOffset = CalculatePathOffset(charT);
            Vector3 targetBase = new Vector3(bottomCentre.x + pathOffset.x,
                                             bottomCentre.y + pathOffset.y,
                                             bottomCentre.z);

            // Rotation: angle of the path tangent at this t
            float angle = 0f;
            if (rotateCharactersToPath)
            {
                Vector3 tangent = GetPathTangent(charT);
                angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
            }

            // Rotate all 4 vertices around the bottom-centre anchor, then translate
            Quaternion rotation = Quaternion.Euler(0, 0, angle);
            for (int j = 0; j < 4; j++)
            {
                Vector3 local           = vertices[vertexIndex + j] - bottomCentre;
                vertices[vertexIndex + j] = targetBase + rotation * local;
            }
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
            textMesh.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }

    // ---------------------------------------------------------------
    //  Path maths
    //
    //  CalculatePathOffset returns ONLY the deflection (dx, dy) from the
    //  character's natural x position.  The x-spacing of the text is
    //  preserved exactly as TMP laid it out — we never remap x positions.
    // ---------------------------------------------------------------

    public Vector3 CalculatePathOffset(float t)
    {
        float v, h;

        if (t < firstBendPosition)
        {
            float localT = t / firstBendPosition;
            v = Mathf.SmoothStep(0, bendHeights[0],     localT);
            h = Mathf.SmoothStep(0, bendHorizontals[0], localT);
        }
        else if (t < secondBendPosition)
        {
            float localT = (t - firstBendPosition) / (secondBendPosition - firstBendPosition);
            v = Mathf.SmoothStep(bendHeights[0],     bendHeights[1],     localT);
            h = Mathf.SmoothStep(bendHorizontals[0], bendHorizontals[1], localT);
        }
        else
        {
            float localT = (t - secondBendPosition) / (1f - secondBendPosition);
            v = Mathf.SmoothStep(bendHeights[1],     0, localT);
            h = Mathf.SmoothStep(bendHorizontals[1], 0, localT);
        }

        return new Vector3(h, v, 0);
    }

    /// Tangent direction of the path at t, accounting for the actual world-space
    /// x positions (not just the offset delta).
    public Vector3 GetPathTangent(float t)
    {
        float eps = 0.005f;
        float tA  = Mathf.Clamp01(t - eps);
        float tB  = Mathf.Clamp01(t + eps);

        // World-space x comes from the original text layout, offset adds y (and maybe x)
        Vector3 offA = CalculatePathOffset(tA);
        Vector3 offB = CalculatePathOffset(tB);

        float xA = cachedMinX + tA * cachedTextLength + offA.x;
        float yA =                                       offA.y;
        float xB = cachedMinX + tB * cachedTextLength + offB.x;
        float yB =                                       offB.y;

        return new Vector3(xB - xA, yB - yA, 0).normalized;
    }

    // ---------------------------------------------------------------
    //  Internal helpers
    // ---------------------------------------------------------------

    void InitializePath()
    {
        textMesh.ForceMeshUpdate();
        CacheTextMetrics(textMesh.textInfo);

        // Scale bend offsets relative to text height so they always look proportional
        float vScale = bendStrength           * cachedTextLength;
        float hScale = horizontalBendStrength * cachedTextLength;

      // First bend always down (negative), dramatic 80-100 degree drop
bendHeights[0]     = -Random.Range(vScale * 0.8f, vScale);
// Second bend always up (positive), more gentle return
bendHeights[1]     =  Random.Range(vScale * 0.4f, vScale * 0.6f);
bendHorizontals[0] = Random.Range(0f, hScale);
bendHorizontals[1] = Random.Range(0f, hScale);
    }

    void CacheTextMetrics(TMP_TextInfo textInfo)
    {
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;
            minX = Mathf.Min(minX, textInfo.characterInfo[i].bottomLeft.x);
            maxX = Mathf.Max(maxX, textInfo.characterInfo[i].topRight.x);
            minY = Mathf.Min(minY, textInfo.characterInfo[i].bottomLeft.y);
            maxY = Mathf.Max(maxY, textInfo.characterInfo[i].topRight.y);
        }

        cachedMinX       = minX;
        cachedTextLength = maxX - minX;
        float h          = maxY - minY;
        cachedTextHeight = h > 0 ? h : textMesh.fontSize;
    }

    // ---------------------------------------------------------------
    //  Editor gizmos
    // ---------------------------------------------------------------

    void OnDrawGizmos()
    {
        if (!showGizmos || bendHeights == null || bendHeights.Length < 2) return;

        Gizmos.color = Color.yellow;
        int steps = 60;
        Vector3 lastPos = transform.position;

        for (int i = 1; i <= steps; i++)
        {
            float   t      = i / (float)steps;
            Vector3 off    = CalculatePathOffset(t);
            Vector3 newPos = transform.position
                           + transform.right * (t * 5f + off.x * 0.005f)
                           + transform.up    * (off.y * 0.005f);
            Gizmos.DrawLine(lastPos, newPos);
            lastPos = newPos;
        }

        Gizmos.color = Color.red;
        Vector3 g1 = CalculatePathOffset(firstBendPosition);
        Vector3 g2 = CalculatePathOffset(secondBendPosition);
        Gizmos.DrawWireSphere(transform.position + transform.right * (firstBendPosition  * 5f) + transform.up * g1.y * 0.005f, 0.12f);
        Gizmos.DrawWireSphere(transform.position + transform.right * (secondBendPosition * 5f) + transform.up * g2.y * 0.005f, 0.12f);
    }

    void OnValidate()
    {
        if (Application.isPlaying && textMesh != null)
            ApplyBend();
    }
}