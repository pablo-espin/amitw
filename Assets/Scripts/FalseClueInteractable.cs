using UnityEngine;

public class FalseClueInteractable : MonoBehaviour
{
    [SerializeField] private FalseClueSystem falseClueSystem;
    [SerializeField] private string interactionPrompt = "Press E to use computer";
    
    private float lastInteractionTime = 0f;
    private float debounceTime = 0.5f; // Half-second cooldown
    
    public string GetInteractionPrompt()
    {
        // If computer is locked, return empty string to prevent prompt from showing
        if (falseClueSystem != null && falseClueSystem.IsComputerLocked())
        {
            return "";
        }
        return interactionPrompt;
    }
    
    public void Interact()
    {
        // Prevent multiple interactions in quick succession
        if (Time.time - lastInteractionTime < debounceTime)
        {
            Debug.Log("Interaction debounced - too soon");
            return;
        }
        
        lastInteractionTime = Time.time;
        
        if (falseClueSystem == null)
        {
            Debug.LogError("FalseClueSystem not assigned to " + gameObject.name);
            return;
        }
        
        falseClueSystem.InteractWithComputer();
    }
}