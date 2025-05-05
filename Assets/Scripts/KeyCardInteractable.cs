using UnityEngine;

public class KeyCardInteractable : MonoBehaviour
{
    [SerializeField] private KeyCardAccessManager accessManager;
    [SerializeField] private string interactionPrompt = "Press E to pick up key card";
    
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
        
        if (accessManager == null)
        {
            Debug.LogError("KeyCardAccessManager not assigned to " + gameObject.name);
            // Try to find it
            accessManager = FindObjectOfType<KeyCardAccessManager>();
            if (accessManager == null)
            {
                Debug.LogError("KeyCardAccessManager not found in scene!");
                return;
            }
        }
        
        // Pick up the key card
        accessManager.AcquireKeyCard();
        
        // Play pickup sound if available
        if (InteractionSoundManager.Instance != null)
        {
            InteractionSoundManager.Instance.PlayKeyCardPickup();
        }
        
        // Make the physical object disappear
        gameObject.SetActive(false);
    }
}