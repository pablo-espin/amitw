using UnityEngine;
using System.Collections;

public class DoorInteractable : MonoBehaviour
{
    [SerializeField] private string interactionPrompt = "Press E to open door";
    [SerializeField] private float targetYRotation = 180f; // Final Y rotation
    [SerializeField] private float openSpeed = 2f;
    [SerializeField] private BoxCollider doorCollider;
    
    private bool isOpen = false;
    private float initialYRotation; // Store the initial Y rotation
    private float currentYRotation;
    private PlayerInteractionManager interactionManager;
    private float lastInteractionTime = 0f;
    private float debounceTime = 0.5f; // Half-second cooldown
    
    private void Start()
    {
        // Store the initial Y rotation
        initialYRotation = transform.localRotation.eulerAngles.y;
        currentYRotation = initialYRotation;
        
        interactionManager = FindObjectOfType<PlayerInteractionManager>();
        
        // Ensure the door collider reference is set
        if (doorCollider == null)
        {
            doorCollider = GetComponent<BoxCollider>();
        }
        
        Debug.Log($"Door initial rotation: {initialYRotation}");
    }
    
    private void Update()
    {
        // Handle door animation
        if (isOpen && currentYRotation < targetYRotation)
        {
            currentYRotation = Mathf.MoveTowards(currentYRotation, targetYRotation, openSpeed * Time.deltaTime * 60f);
            
            // Maintain the original X and Z rotation while changing Y
            Vector3 currentRotation = transform.localRotation.eulerAngles;
            transform.localRotation = Quaternion.Euler(currentRotation.x, currentYRotation, currentRotation.z);
            
            // When the door is fully open, disable the collider
            if (Mathf.Approximately(currentYRotation, targetYRotation) && doorCollider != null)
            {
                doorCollider.enabled = false;
            }
        }
    }
    
    public string GetInteractionPrompt()
    {
        return !isOpen ? interactionPrompt : "";
    }
    
    public void Interact()
    {
        // Prevent multiple interactions in quick succession
        if (Time.time - lastInteractionTime < debounceTime)
        {
            Debug.Log("Door interaction debounced - too soon");
            return;
        }
        
        lastInteractionTime = Time.time;
        
        if (!isOpen)
        {
            OpenDoor();
            
            // Play door sound if you have an InteractionSoundManager
            if (InteractionSoundManager.Instance != null)
            {
                // You'll need to add this method to your InteractionSoundManager
                // InteractionSoundManager.Instance.PlayDoorOpen();
            }
        }
    }
    
    private void OpenDoor()
    {
        isOpen = true;
        
        // Temporarily disable player interaction during animation
        if (interactionManager != null)
        {
            StartCoroutine(EnableInteractionAfterDelay(1.0f));
        }
        
        Debug.Log($"Opening door from {currentYRotation} to {targetYRotation}");
    }
    
    private IEnumerator EnableInteractionAfterDelay(float delay)
    {
        interactionManager.SetInteractionEnabled(false);
        yield return new WaitForSeconds(delay);
        interactionManager.SetInteractionEnabled(true);
    }
}