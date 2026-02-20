using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(TMP_Text))]
[RequireComponent(typeof(BendyTextPath))]
public class BendyTextAnimator : MonoBehaviour
{
    [Header("Auto Play")]
    public bool autoPlay = true;
    public float autoPlayDelay = 0.1f;

    [Header("Timing")]
    [Tooltip("How long the whole animation takes")]
    public float totalDuration = 2.5f;

    [Tooltip("How far to the right each letter starts before sliding in (in units matching text width, 0-1)")]
    [Range(0f, 1f)]
    public float startOffset = 0.3f;

    [Tooltip("How staggered each letter's start is (seconds)")]
    public float letterDelay = 0.06f;

    [Tooltip("How long each individual letter takes to slide to its spot")]
    public float letterDuration = 0.4f;

    [Tooltip("Easing for each letter sliding in")]
    public AnimationCurve letterCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Effects")]
    public bool fadeIn = true;

    private TMP_Text textMesh;
    private BendyTextPath bendyPath;
    private bool isAnimating = false;

    void Awake()
    {
        textMesh  = GetComponent<TMP_Text>();
        bendyPath = GetComponent<BendyTextPath>();
    }

    void Start()
    {
        if (autoPlay)
            StartCoroutine(AutoPlayAfterDelay());
    }

    IEnumerator AutoPlayAfterDelay()
    {
        yield return null;
        if (autoPlayDelay > 0) yield return new WaitForSeconds(autoPlayDelay);
        Activate();
    }

    public void Activate()
    {
        if (isAnimating) StopAllCoroutines();
        StartCoroutine(AnimateLetters());
    }

    public void ReverseActivate()
    {
        if (isAnimating) StopAllCoroutines();
        StartCoroutine(AnimateLettersOut());
    }

    IEnumerator AnimateLettersOut()
    {
        isAnimating = true;

        textMesh.ForceMeshUpdate();
        bendyPath.ApplyBend();

        TMP_TextInfo textInfo = textMesh.textInfo;

        Vector3[][] finalVerts  = SnapshotVertices(textInfo);
        Color32[][] finalColors = SnapshotColors(textInfo);

        // Collect visible chars left to right, then reverse so rightmost letter leaves first
        List<int> chars = new List<int>();
        for (int i = 0; i < textInfo.characterCount; i++)
            if (textInfo.characterInfo[i].isVisible) chars.Add(i);
        chars.Reverse();

        if (chars.Count == 0) { isAnimating = false; yield break; }

        float textWidth = GetTextWidth(textInfo);

        for (int i = 0; i < chars.Count; i++)
        {
            StartCoroutine(AnimateSingleLetterOut(
                textInfo, chars[i], i * letterDelay,
                finalVerts, finalColors, textWidth));
        }

        float totalWait = (chars.Count - 1) * letterDelay + letterDuration + 0.1f;
        yield return new WaitForSeconds(totalWait);

        HideAllCharacters();

        isAnimating = false;
    }

    IEnumerator AnimateSingleLetterOut(TMP_TextInfo textInfo, int charIndex, float delay,
                                       Vector3[][] finalVerts, Color32[][] finalColors,
                                       float textWidth)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        int matIdx = textInfo.characterInfo[charIndex].materialReferenceIndex;
        int vtxIdx = textInfo.characterInfo[charIndex].vertexIndex;

        Vector3 fv0 = finalVerts[matIdx][vtxIdx + 0];
        Vector3 fv1 = finalVerts[matIdx][vtxIdx + 1];
        Vector3 fv2 = finalVerts[matIdx][vtxIdx + 2];
        Vector3 fv3 = finalVerts[matIdx][vtxIdx + 3];

        // Slide out to the right
        Vector3 slideOffset = new Vector3(textWidth * startOffset, 0f, 0f);

        float elapsed = 0f;

        while (elapsed < letterDuration)
        {
            elapsed += Time.deltaTime;
            float t     = Mathf.Clamp01(elapsed / letterDuration);
            float eased = letterCurve.Evaluate(t);

            // Interpolate from final position outward to the right
            Vector3 offset = Vector3.Lerp(Vector3.zero, slideOffset, eased);
            byte    alpha  = fadeIn ? (byte)((1f - eased) * 255f) : (byte)255;

            Vector3[] verts  = textInfo.meshInfo[matIdx].vertices;
            Color32[] colors = textInfo.meshInfo[matIdx].colors32;

            verts[vtxIdx + 0] = fv0 + offset;
            verts[vtxIdx + 1] = fv1 + offset;
            verts[vtxIdx + 2] = fv2 + offset;
            verts[vtxIdx + 3] = fv3 + offset;

            if (fadeIn)
            {
                for (int j = 0; j < 4; j++)
                {
                    Color32 fc = finalColors[matIdx][vtxIdx + j];
                    colors[vtxIdx + j] = new Color32(fc.r, fc.g, fc.b, alpha);
                }
                textInfo.meshInfo[matIdx].mesh.colors32 = colors;
            }

            textInfo.meshInfo[matIdx].mesh.vertices = verts;
            textMesh.UpdateGeometry(textInfo.meshInfo[matIdx].mesh, matIdx);

            yield return null;
        }

        // Hide this letter once it's gone
        Color32[] hcolors = textInfo.meshInfo[matIdx].colors32;
        for (int j = 0; j < 4; j++)
        {
            Color32 fc = finalColors[matIdx][vtxIdx + j];
            hcolors[vtxIdx + j] = new Color32(fc.r, fc.g, fc.b, 0);
        }
        textInfo.meshInfo[matIdx].mesh.colors32 = hcolors;
        textMesh.UpdateGeometry(textInfo.meshInfo[matIdx].mesh, matIdx);
    }

    public void ResetToStart()
    {
        StopAllCoroutines();
        isAnimating = false;
        textMesh.ForceMeshUpdate();
        bendyPath.ApplyBend();
        HideAllCharacters();
    }

    // ---------------------------------------------------------------
    //  Core animation
    // ---------------------------------------------------------------

    IEnumerator AnimateLetters()
    {
        isAnimating = true;

        // Apply the bend — this sets every character to its correct final position
        textMesh.ForceMeshUpdate();
        bendyPath.ApplyBend();

        TMP_TextInfo textInfo = textMesh.textInfo;

        // Snapshot the final bent positions — this is ground truth, never modified
        Vector3[][] finalVerts  = SnapshotVertices(textInfo);
        Color32[][] finalColors = SnapshotColors(textInfo);

        // Collect visible chars left to right
        List<int> chars = new List<int>();
        for (int i = 0; i < textInfo.characterCount; i++)
            if (textInfo.characterInfo[i].isVisible) chars.Add(i);

        if (chars.Count == 0) { isAnimating = false; yield break; }

        // Hide everything
        HideAllCharacters();

        // Calculate text bounds for offset scaling
        float textWidth = GetTextWidth(textInfo);

        // Launch each letter's coroutine with a stagger
        for (int i = 0; i < chars.Count; i++)
        {
            StartCoroutine(AnimateSingleLetter(
                textInfo, chars[i], i * letterDelay,
                finalVerts, finalColors, textWidth));
        }

        // Wait for all to finish
        float totalWait = (chars.Count - 1) * letterDelay + letterDuration + 0.1f;
        yield return new WaitForSeconds(totalWait);

        // Final snap — restore all to exact final state
        RestoreAll(textInfo, finalVerts, finalColors);

        isAnimating = false;
    }

    IEnumerator AnimateSingleLetter(TMP_TextInfo textInfo, int charIndex, float delay,
                                    Vector3[][] finalVerts, Color32[][] finalColors,
                                    float textWidth)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        int matIdx = textInfo.characterInfo[charIndex].materialReferenceIndex;
        int vtxIdx = textInfo.characterInfo[charIndex].vertexIndex;

        // The final 4 vertices for this letter
        Vector3 fv0 = finalVerts[matIdx][vtxIdx + 0];
        Vector3 fv1 = finalVerts[matIdx][vtxIdx + 1];
        Vector3 fv2 = finalVerts[matIdx][vtxIdx + 2];
        Vector3 fv3 = finalVerts[matIdx][vtxIdx + 3];

        // The slide-in offset: push the letter to the right by startOffset * textWidth
        // This means it slides in from the right along the same bent orientation it will land at
        Vector3 slideOffset = new Vector3(textWidth * startOffset, 0f, 0f);

        float elapsed = 0f;

        while (elapsed < letterDuration)
        {
            elapsed += Time.deltaTime;
            float t     = Mathf.Clamp01(elapsed / letterDuration);
            float eased = letterCurve.Evaluate(t);

            // Interpolate from (final + slideOffset) to final
            Vector3 offset = Vector3.Lerp(slideOffset, Vector3.zero, eased);
            byte    alpha  = fadeIn ? (byte)(eased * 255f) : (byte)255;

            Vector3[] verts  = textInfo.meshInfo[matIdx].vertices;
            Color32[] colors = textInfo.meshInfo[matIdx].colors32;

            verts[vtxIdx + 0] = fv0 + offset;
            verts[vtxIdx + 1] = fv1 + offset;
            verts[vtxIdx + 2] = fv2 + offset;
            verts[vtxIdx + 3] = fv3 + offset;

            if (fadeIn)
            {
                for (int j = 0; j < 4; j++)
                {
                    Color32 fc = finalColors[matIdx][vtxIdx + j];
                    colors[vtxIdx + j] = new Color32(fc.r, fc.g, fc.b, alpha);
                }
                textInfo.meshInfo[matIdx].mesh.colors32 = colors;
            }

            textInfo.meshInfo[matIdx].mesh.vertices = verts;
            textMesh.UpdateGeometry(textInfo.meshInfo[matIdx].mesh, matIdx);

            yield return null;
        }

        // Snap to exact final position
        Vector3[] fverts  = textInfo.meshInfo[matIdx].vertices;
        Color32[] fcolors = textInfo.meshInfo[matIdx].colors32;
        fverts[vtxIdx + 0] = fv0;
        fverts[vtxIdx + 1] = fv1;
        fverts[vtxIdx + 2] = fv2;
        fverts[vtxIdx + 3] = fv3;
        for (int j = 0; j < 4; j++)
            fcolors[vtxIdx + j] = finalColors[matIdx][vtxIdx + j];
        textInfo.meshInfo[matIdx].mesh.vertices = fverts;
        textInfo.meshInfo[matIdx].mesh.colors32 = fcolors;
        textMesh.UpdateGeometry(textInfo.meshInfo[matIdx].mesh, matIdx);
    }

    // ---------------------------------------------------------------
    //  Helpers
    // ---------------------------------------------------------------

    void HideAllCharacters()
    {
        textMesh.ForceMeshUpdate();
        TMP_TextInfo textInfo = textMesh.textInfo;
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;
            int matIdx = textInfo.characterInfo[i].materialReferenceIndex;
            int vtxIdx = textInfo.characterInfo[i].vertexIndex;
            Color32[] colors = textInfo.meshInfo[matIdx].colors32;
            for (int j = 0; j < 4; j++)
            {
                Color32 c = colors[vtxIdx + j];
                colors[vtxIdx + j] = new Color32(c.r, c.g, c.b, 0);
            }
        }
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.colors32 = textInfo.meshInfo[i].colors32;
            textMesh.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }

    void RestoreAll(TMP_TextInfo textInfo, Vector3[][] finalVerts, Color32[][] finalColors)
    {
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = finalVerts[i];
            textInfo.meshInfo[i].mesh.colors32 = finalColors[i];
            textMesh.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }

    Vector3[][] SnapshotVertices(TMP_TextInfo textInfo)
    {
        var snap = new Vector3[textInfo.meshInfo.Length][];
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
            snap[i] = textInfo.meshInfo[i].vertices.Clone() as Vector3[];
        return snap;
    }

    Color32[][] SnapshotColors(TMP_TextInfo textInfo)
    {
        var snap = new Color32[textInfo.meshInfo.Length][];
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
            snap[i] = textInfo.meshInfo[i].colors32.Clone() as Color32[];
        return snap;
    }

    float GetTextWidth(TMP_TextInfo textInfo)
    {
        float minX = float.MaxValue, maxX = float.MinValue;
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;
            minX = Mathf.Min(minX, textInfo.characterInfo[i].bottomLeft.x);
            maxX = Mathf.Max(maxX, textInfo.characterInfo[i].topRight.x);
        }
        return maxX - minX;
    }
}