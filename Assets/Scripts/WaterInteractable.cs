using UnityEngine;

public class WaterInteractable : MonoBehaviour
{
    public enum InteractableType
    {
        Tap,
        Valve
    }
    
    [SerializeField] private InteractableType type;
    [SerializeField] private WaterClueSystem waterClueSystem;
    [SerializeField] private string interactionPrompt = "Press E to interact";
    
    public string GetInteractionPrompt()
    {
        return interactionPrompt;
    }
    
    private float lastInteractionTime = 0f;
    private float debounceTime = 0.5f; // Half-second cooldown

        public void Interact()
    {
        // Prevent multiple interactions in quick succession
        if (Time.time - lastInteractionTime < debounceTime)
        {
            Debug.Log("Interaction debounced - too soon");
            return;
        }
        
        lastInteractionTime = Time.time;
        
        if (waterClueSystem == null)
        {
            Debug.LogError("WaterClueSystem not assigned to " + gameObject.name);
            return;
        }
        
        switch (type)
        {
            case InteractableType.Tap:
                waterClueSystem.InteractWithTap();
                break;
                
            case InteractableType.Valve:
                waterClueSystem.InteractWithValve();
                break;
        }
    }
}