using UnityEngine;

public class CursorManager : MonoBehaviour
{
    [Header("Cursor Textures")]
    [SerializeField] private Texture2D defaultCursor;
    [SerializeField] private Texture2D hoverCursor;
    [SerializeField] private Texture2D interactCursor;
    
    [Header("Settings")]
    [SerializeField] private Vector2 cursorHotspot = Vector2.zero;
    [SerializeField] private float raycastDistance = 100f;
    [SerializeField] private LayerMask interactableLayer;
    
    private Camera mainCamera;
    private CursorType currentCursorType = CursorType.Default;
    
    public enum CursorType
    {
        Default,
        Hover,
        Interact
    }
    
    void Start()
    {
        mainCamera = Camera.main;
        SetCursor(CursorType.Default);
    }
    
    void Update()
    {
        CheckCursorState();
    }
    
    void CheckCursorState()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, raycastDistance, interactableLayer))
        {
            // Check if the object has an Interactable component
            Interactable interactable = hit.collider.GetComponent<Interactable>();
            
            if (interactable != null)
            {
                SetCursor(interactable.GetCursorType());
            }
            else
            {
                SetCursor(CursorType.Hover);
            }
        }
        else
        {
            SetCursor(CursorType.Default);
        }
    }
    
    void SetCursor(CursorType type)
    {
        if (currentCursorType == type) return;
        
        currentCursorType = type;
        
        switch (type)
        {
            case CursorType.Default:
                Cursor.SetCursor(defaultCursor, cursorHotspot, CursorMode.Auto);
                break;
            case CursorType.Hover:
                Cursor.SetCursor(hoverCursor, cursorHotspot, CursorMode.Auto);
                break;
            case CursorType.Interact:
                Cursor.SetCursor(interactCursor, cursorHotspot, CursorMode.Auto);
                break;
        }
    }
}