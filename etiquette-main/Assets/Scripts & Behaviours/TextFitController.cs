using UnityEngine;
using TMPro;

/// <summary>
/// Dynamically scales a TextMeshPro 3D text object so that its rendered bounds
/// always fit within a defined world-space box size.
///
/// Works in two modes:
///   - Standalone: no TMP on this GameObject → creates a child TMP automatically.
///   - Companion:  a TMP already exists on this GameObject → uses that one directly.
///
/// Usage with textGenOneShot:
///   1. Attach both TextFitController and TextMeshPro to the SAME GameObject.
///   2. In textGenOneShot, call tfc.SetText(savedtext) and do NOT clear myText.text.
/// </summary>
[ExecuteAlways]
public class TextFitController : MonoBehaviour
{
    [Header("Box Constraints")]
    [Tooltip("Maximum width and height (world units) the text must stay within.")]
    public Vector2 boxSize = new Vector2(4f, 1f);

    [Tooltip("Draw a Gizmo in the editor so you can see the box bounds.")]
    public bool showGizmo = true;

    [Header("Text Settings (Standalone mode only)")]
    [Tooltip("Initial text to display. Only used if no external script sets the text.")]
    [TextArea] public string initialText = "";

    [Tooltip("Font asset to use. If null, TMP will use its default.")]
    public TMP_FontAsset fontAsset;

    [Header("Fit Settings")]
    [Tooltip("Upper font size cap before fitting begins.")]
    public float maxFontSize = 36f;

    [Tooltip("Smallest allowed font size (prevents text becoming unreadable).")]
    public float minFontSize = 1f;

    [Tooltip("Binary-search iterations. Higher = more accurate.")]
    [Range(4, 20)] public int fitIterations = 12;

    // ── Private state ─────────────────────────────────────────────────────────

    private TextMeshPro _tmp;
    private bool _initialised = false;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        Initialise();

        // Only auto-display initialText in standalone mode and if text is set.
        if (!string.IsNullOrEmpty(initialText))
            SetText(initialText);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Set the text and resize it to fit the box.
    /// Call this instead of setting myText.text directly.
    /// </summary>
    public void SetText(string text)
    {
        Initialise();
        _tmp.text = text;
        FitTextToBox();
    }

    // ── Initialisation ────────────────────────────────────────────────────────

    private void Initialise()
    {
        if (_initialised) return;
        _initialised = true;

        // Prefer an existing TMP on this GameObject (companion mode).
        _tmp = GetComponent<TextMeshPro>();

        if (_tmp == null)
        {
            // Standalone mode: create a child GameObject with its own TMP.
            var go = new GameObject("DynamicText3D");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale    = Vector3.one;
        

            _tmp = go.AddComponent<TextMeshPro>();


            if (fontAsset != null)
                _tmp.font = fontAsset;
        }

        // Common TMP configuration.
        _tmp.enableAutoSizing = false;
        _tmp.overflowMode     = TextOverflowModes.Overflow;
        _tmp.alignment        = TextAlignmentOptions.Center;

        // Centre the RectTransform pivot so it aligns with the Gizmo.
        var rt              = _tmp.rectTransform;
    
      
    }

    // ── Core fitting logic ────────────────────────────────────────────────────
private void FitTextToBox()
{

var rt = _tmp.rectTransform;
    Debug.Log($"sizeDelta:{rt.sizeDelta} rect:{rt.rect} localScale:{rt.localScale} lossyScale:{rt.lossyScale} parent lossyScale:{rt.parent?.lossyScale}");

    // lossyScale matches the yellow box size when resized by dragging in Scene view
    Vector2 size = new Vector2(
        _tmp.rectTransform.lossyScale.x,
        _tmp.rectTransform.lossyScale.y
    );

    float lo = minFontSize;
    float hi = maxFontSize;

    for (int i = 0; i < fitIterations; i++)
    {
        float mid = (lo + hi) * 0.5f;
        _tmp.fontSize = mid;
        _tmp.ForceMeshUpdate();

        Vector2 rendered = _tmp.GetRenderedValues(onlyVisibleCharacters: false);

        if (rendered.x <= size.x && rendered.y <= size.y)
            lo = mid;
        else
            hi = mid;
    }

    _tmp.fontSize = lo;
    _tmp.ForceMeshUpdate();
}

    // ── Editor Gizmo ─────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        if (!showGizmo) return;

        Gizmos.color  = Color.cyan;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(boxSize.x, boxSize.y, 0.01f));
    }
}