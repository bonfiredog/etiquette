using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interactable : MonoBehaviour
{
  [SerializeField] private InteractionType interactionType = InteractionType.Hover;
    
    public enum InteractionType
    {
        Hover,      // Generic hoverable object
        Interact,   // Can be clicked/interacted with
    }
    
    public CursorManager.CursorType GetCursorType()
    {
        switch (interactionType)
        {
            case InteractionType.Interact:
                return CursorManager.CursorType.Interact;
            default:
                return CursorManager.CursorType.Hover;
        }
    }
    
    // Optional: Add interaction logic
    void OnMouseDown()
    {
        if (interactionType == InteractionType.Interact)
        {
            Interact();
        }
    }
    
    void Interact()
    {
        Debug.Log($"Interacted with {gameObject.name}");
        // Add your interaction logic here
    }
}

// Note: This extension is needed for the CursorManager to access the enum
namespace UnityEngine
{
    public static class CursorManagerExtensions
    {
        public static CursorManager.CursorType ToCursorType(this Interactable.InteractionType type)
        {
            switch (type)
            {
                case Interactable.InteractionType.Interact:
                    return CursorManager.CursorType.Interact;
                default:
                    return CursorManager.CursorType.Hover;
            }
        }
    }
}
