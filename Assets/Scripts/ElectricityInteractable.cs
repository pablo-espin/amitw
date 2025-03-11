using UnityEngine;

public class ElectricityInteractable : MonoBehaviour
{
    [SerializeField] private ElectricityClueSystem electricityClueSystem;
    [SerializeField] private string interactionPrompt = "Press E to connect cable";
    
    private float lastInteractionTime = 0f;
    private float debounceTime = 0.5f; // Half-second cooldown
    
    public string GetInteractionPrompt()
    {
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
        
        if (electricityClueSystem == null)
        {
            Debug.LogError("ElectricityClueSystem not assigned to " + gameObject.name);
            return;
        }
        
        electricityClueSystem.InteractWithCable();
    }
}