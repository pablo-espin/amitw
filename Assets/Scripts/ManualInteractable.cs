using UnityEngine;

public class ManualInteractable : MonoBehaviour
{
    [SerializeField] private ManualSystem manualSystem;
    [SerializeField] private string interactionPrompt = "Press E to pick up induction manual";
    [SerializeField] private LockerDoorController parentLocker; // Reference to parent locker
    
    private float lastInteractionTime = 0f;
    private float debounceTime = 0.5f; // Half-second cooldown
    private bool interactionEnabled = false; // Controlled by locker door
    private bool manualTaken = false; // Track if manual has been picked up
    
    public string GetInteractionPrompt()
    {
        // Only show prompt if interaction is enabled and manual hasn't been taken
        if (!interactionEnabled || manualTaken)
            return "";
            
        return interactionPrompt;
    }
    
    public void Interact()
    {
        // Check if interaction is allowed
        if (!interactionEnabled || manualTaken)
        {
            Debug.Log("Manual interaction not allowed - locker closed or manual already taken");
            return;
        }
        
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
            InteractionSoundManager.Instance.PlayManualPickup();
        }
        
        // Mark manual as taken
        manualTaken = true;
        
        // Notify parent locker that manual was picked up
        if (parentLocker != null)
        {
            parentLocker.OnManualPickedUp();
        }
        
        // Make the physical object disappear
        gameObject.SetActive(false);
        
        Debug.Log("Manual picked up and object deactivated");
    }
    
    // Method to enable/disable interaction (called by LockerDoorController)
    public void SetInteractionEnabled(bool enabled)
    {
        interactionEnabled = enabled;
        Debug.Log($"Manual interaction {(enabled ? "enabled" : "disabled")}");
    }
    
    // Method to check if manual is still available for pickup
    public bool IsManualAvailable()
    {
        return !manualTaken && gameObject.activeInHierarchy;
    }
    
    // Method to check if interaction is currently enabled
    public bool IsInteractionEnabled()
    {
        return interactionEnabled;
    }
    
    // Reset method for testing/debugging
    [ContextMenu("Reset Manual")]
    public void ResetManual()
    {
        manualTaken = false;
        gameObject.SetActive(true);
        Debug.Log("Manual reset - available for pickup again");
    }
}