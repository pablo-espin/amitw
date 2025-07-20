using UnityEngine;

public class PlayerInteractionManager : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private LayerMask interactableLayer;
    
    [Header("Interaction Prompts")]
    [SerializeField] private GameObject interactionPromptUI;
    [SerializeField] private TMPro.TextMeshProUGUI promptText;
    
    private Camera playerCamera;
    private MemorySphere currentMemorySphere;
    private bool canInteract = true;

    void Start()
    {
        playerCamera = Camera.main;
        
        // Hide interaction prompt initially
        if (interactionPromptUI != null)
        {
            interactionPromptUI.SetActive(false);
        }
    }

    void Update()
    {
        if (canInteract)
        {
            CheckForInteractables();
            
            // Check for interaction input
            if (Input.GetKeyDown(KeyCode.E))
            {
                TryInteract();
            }
        }
    }

    void CheckForInteractables()
    {
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionRange, interactableLayer))
        {
            // Check for memory sphere
            MemorySphere sphere = hit.collider.GetComponent<MemorySphere>();
            if (sphere != null && !sphere.IsDecrypted() && !sphere.IsCorrupted())
            {
                // Show interaction prompt
                ShowInteractionPrompt("Press E to interact with memory sphere");
                return;
            }
            
            // Check for water interactables
            WaterInteractable waterObject = hit.collider.GetComponent<WaterInteractable>();
            if (waterObject != null)
            {
                ShowInteractionPrompt(waterObject.GetInteractionPrompt());
                return;
            }
            
            // Check for electricity interactables
            ElectricityInteractable electricityObject = hit.collider.GetComponent<ElectricityInteractable>();
            if (electricityObject != null)
            {
                ShowInteractionPrompt(electricityObject.GetInteractionPrompt());
                return;
            }
            
            // Check for location interactables
            LocationInteractable locationObject = hit.collider.GetComponent<LocationInteractable>();
            if (locationObject != null)
            {
                ShowInteractionPrompt(locationObject.GetInteractionPrompt());
                return;
            }
            
            // Check for false clue interactable
            FalseClueInteractable falseClueObject = hit.collider.GetComponent<FalseClueInteractable>();
            if (falseClueObject != null)
            {
                ShowInteractionPrompt(falseClueObject.GetInteractionPrompt());
                return;
            }
            
            // Check for manual interactable
            ManualInteractable manualObject = hit.collider.GetComponent<ManualInteractable>();
            if (manualObject != null)
            {
                ShowInteractionPrompt(manualObject.GetInteractionPrompt());
                return;
            }

            // Check for key card interactable
            KeyCardInteractable keyCardObject = hit.collider.GetComponent<KeyCardInteractable>();
            if (keyCardObject != null)
            {
                ShowInteractionPrompt(keyCardObject.GetInteractionPrompt());
                return;
            }

            // Check for key card door controller
            DoorKeyCardController keyCardDoor = hit.collider.GetComponent<DoorKeyCardController>();
            if (keyCardDoor != null)
            {
                string prompt = keyCardDoor.GetInteractionPrompt();
                if (!string.IsNullOrEmpty(prompt))
                {
                    ShowInteractionPrompt(prompt);
                    return;
                }
            }

            // Check for the lounge door interactable
            DoorInteractable doorObject = hit.collider.GetComponent<DoorInteractable>();
            if (doorObject != null)
            {
                ShowInteractionPrompt(doorObject.GetInteractionPrompt());
                return;
            }
            

            // If no valid interactable found, hide prompt
            HideInteractionPrompt();
        }
        else
        {
            HideInteractionPrompt();
        }
    }

    void TryInteract()
    {
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionRange, interactableLayer))
        {
            // Check for memory sphere
            MemorySphere sphere = hit.collider.GetComponent<MemorySphere>();
            if (sphere != null && !sphere.IsDecrypted() && !sphere.IsCorrupted())
            {
                sphere.OnInteract();
                currentMemorySphere = sphere;
                return;
            }
            
            // Check for water interactables
            WaterInteractable waterObject = hit.collider.GetComponent<WaterInteractable>();
            if (waterObject != null)
            {
                waterObject.Interact();
                return;
            }
            
            // Check for electricity interactables
            ElectricityInteractable electricityObject = hit.collider.GetComponent<ElectricityInteractable>();
            if (electricityObject != null)
            {
                electricityObject.Interact();
                return;
            }
            
            // Check for location interactables
            LocationInteractable locationObject = hit.collider.GetComponent<LocationInteractable>();
            if (locationObject != null)
            {
                locationObject.Interact();
                return;
            }
            
            // Check for false clue interactable
            FalseClueInteractable falseClueObject = hit.collider.GetComponent<FalseClueInteractable>();
            if (falseClueObject != null)
            {
                falseClueObject.Interact();
                return;
            }
            
            // Check for manual interactable
            ManualInteractable manualObject = hit.collider.GetComponent<ManualInteractable>();
            if (manualObject != null)
            {
                manualObject.Interact();
                return;
            }

            // Check for key card interactable
            KeyCardInteractable keyCardObject = hit.collider.GetComponent<KeyCardInteractable>();
            if (keyCardObject != null)
            {
                keyCardObject.Interact();
                return;
            }

            // Check for key card door controller (restricted area door)
            DoorKeyCardController doorController = hit.collider.GetComponent<DoorKeyCardController>();
            if (doorController != null)
            {
                Debug.Log("Interacting with door");
                doorController.TryOpenDoor();
                return;
            }

            // Check for lounge door interactable
            DoorInteractable doorObject = hit.collider.GetComponent<DoorInteractable>();
            if (doorObject != null)
            {
                doorObject.Interact();
                return;
            }
        }
    }
    
    private void ShowInteractionPrompt(string message)
    {
        if (interactionPromptUI != null)
        {
            interactionPromptUI.SetActive(true);
            if (promptText != null)
            {
                promptText.text = message;
            }
        }
        else
        {
            // Fallback to debug log if UI is not set up
            Debug.Log(message);
        }
    }
    
    private void HideInteractionPrompt()
    {
        if (interactionPromptUI != null)
        {
            interactionPromptUI.SetActive(false);
        }
    }

    // Call this from the GameHUDManager when decryption is successful
    public void DecryptCurrentSphere()
    {
        if (currentMemorySphere != null)
        {
            currentMemorySphere.Decrypt();
        }
    }
    
    // Call this from the GameHUDManager when false clue is used
    public void CorruptCurrentSphere()
    {
        if (currentMemorySphere != null)
        {
            currentMemorySphere.Corrupt();
        }
    }
    
    // Use this to temporarily disable player interaction (e.g., during animations)
    public void SetInteractionEnabled(bool enabled)
    {
        canInteract = enabled;
        if (!enabled)
        {
            HideInteractionPrompt();
        }
    }
    
    // Check if interaction is currently enabled
    public bool IsInteractionEnabled()
    {
        return canInteract;
    }
    
    // Get the current memory sphere reference
    public MemorySphere GetCurrentMemorySphere()
    {
        return currentMemorySphere;
    }
}