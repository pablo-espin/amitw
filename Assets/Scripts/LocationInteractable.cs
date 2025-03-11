using UnityEngine;

public class LocationInteractable : MonoBehaviour
{
    public enum DocumentType
    {
        LocationList,
        TransportCard
    }
    
    [SerializeField] private DocumentType documentType;
    [SerializeField] private LocationClueSystem locationClueSystem;
    [SerializeField] private string interactionPrompt = "Press E to examine";
    
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
        
        if (locationClueSystem == null)
        {
            Debug.LogError("LocationClueSystem not assigned to " + gameObject.name);
            return;
        }
        
        // Call appropriate method based on document type
        switch (documentType)
        {
            case DocumentType.LocationList:
                locationClueSystem.ExamineLocationList();
                break;
                
            case DocumentType.TransportCard:
                locationClueSystem.ExamineTransportCard();
                break;
        }
    }
}