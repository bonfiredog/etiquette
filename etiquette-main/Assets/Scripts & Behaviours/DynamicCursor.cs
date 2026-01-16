using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image), typeof(CanvasGroup))]
public class DynamicCursor : MonoBehaviour
{
    [Header("Public Controls")]
    public GameObject currentTarget; 
    public bool forceHide = false;
    public bool isGrabbed = false; // NEW: Set by camera script
    public Vector3 grabPosition;   // NEW: The world-to-screen position of the handle

    [Header("Cursor Visuals")]
    [SerializeField] private Sprite standardSprite;
    [SerializeField] private Sprite hoverSprite;
    [SerializeField] private Sprite grabSprite; // NEW: The downward arrow
    
    [Header("Settings")]
    [SerializeField] private float standardSize = 1.0f;
    [SerializeField] private float hoverSize = 1.2f;
    [SerializeField] private float sizeLerpSpeed = 10f;
    [SerializeField] private string interactableTag = "inter";
    [SerializeField] private float raycastDistance = 1000f; 
    [SerializeField] private LayerMask detectionLayers = ~0;

    [Header("Opacity & Fading")]
    [SerializeField] private float defaultOpacity = 0.6f;
    [SerializeField] private float hoverOpacity = 1.0f;
    [SerializeField] private float fadeIdleTime = 3.0f;
    [SerializeField] private float fadeSpeed = 5.0f;

    private Image cursorImage;
    private CanvasGroup canvasGroup;
    private float lastInputTime;
    private float targetAlpha;
    private float targetScale;

    void Awake()
    {
        cursorImage = GetComponent<Image>();
        canvasGroup = GetComponent<CanvasGroup>();
        cursorImage.raycastTarget = false;
        Cursor.visible = false;
        if (standardSprite != null) cursorImage.sprite = standardSprite;
    }

    void Update()
    {
        // 1. POSITIONING
        if (isGrabbed)
        {
            // Lock to the handle's screen position
            transform.position = grabPosition;
            cursorImage.sprite = grabSprite;
            targetAlpha = hoverOpacity;
        }
        else
        {
            // Follow mouse as normal
            transform.position = Input.mousePosition;
            if (Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0 || Input.anyKey || Input.GetMouseButton(0))
            {
                lastInputTime = Time.time;
            }
            PerformRaycast();
        }

        ApplyTransitions();
    }

    private void PerformRaycast()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, raycastDistance, detectionLayers))
        {
            if (hit.collider.CompareTag(interactableTag))
            {
                currentTarget = hit.collider.gameObject;
                cursorImage.sprite = hoverSprite;
                targetAlpha = hoverOpacity;
                targetScale = hoverSize;
                return;
            }
        }

        currentTarget = null;
        cursorImage.sprite = standardSprite;
        targetAlpha = defaultOpacity;
        targetScale = standardSize;
    }

    private void ApplyTransitions()
    {
        float finalTargetAlpha = (Time.time - lastInputTime > fadeIdleTime) ? 0f : targetAlpha;
        if (forceHide) finalTargetAlpha = 0f;

        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, finalTargetAlpha, Time.deltaTime * fadeSpeed);
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * targetScale, Time.deltaTime * sizeLerpSpeed);
    }
}