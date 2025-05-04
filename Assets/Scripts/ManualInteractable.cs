using UnityEngine;

public class ManualInteractable : MonoBehaviour
{
    [SerializeField] private ManualSystem manualSystem;
    [SerializeField] private string interactionPrompt = "Press E to pick up induction manual";
    
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
        
        if (manualSystem == null)
        {
            Debug.LogError("ManualSystem not assigned to " + gameObject.name);
            return;
        }
        
        // Pickup the manual
        manualSystem.PickupManual();
        
        // Play pickup sound if available
        if (InteractionSoundManager.Instance != null)
        {
            // You'll need to add this method to your InteractionSoundManager
            // InteractionSoundManager.Instance.PlayManualPickup();
        }
        
        // Make the physical object disappear
        gameObject.SetActive(false);
    }
}