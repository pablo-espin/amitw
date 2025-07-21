using UnityEngine;

public class ExitDoorController : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private string escapePrompt = "Press E to escape facility";
    [SerializeField] private float interactionRange = 3f;
    
    private bool isEscapeWindowActive = false;
    private bool isInRange = false;
    private bool hasEscaped = false;
    private Transform playerTransform;
    private float lastInteractionTime = 0f;
    private float interactionCooldown = 1f;
    
    private void Start()
    {
        // Find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            playerTransform = Camera.main?.transform;
        }
        
        Debug.Log("Exit door initialized - Locked until escape window");
    }
    
    private void Update()
    {
        if (playerTransform == null || hasEscaped) return;
        
        // Check if player is in range
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        bool wasInRange = isInRange;
        isInRange = distance <= interactionRange;
        
        // Handle interaction
        if (Input.GetKeyDown(KeyCode.E) && isInRange)
        {
            TryInteract();
        }
    }
    
    public string GetInteractionPrompt()
    {
        if (!isInRange) return "";
        
        if (isEscapeWindowActive)
        {
            return escapePrompt;
        }
        
        return ""; // No prompt when door is locked (but still allow interaction for sound)
    }
    
    public void Interact()
    {
        TryInteract();
    }
    
    public void TryInteract()
    {
        // Prevent spam clicking
        if (Time.time - lastInteractionTime < interactionCooldown)
            return;
            
        lastInteractionTime = Time.time;
        
        if (isEscapeWindowActive)
        {
            // Escape the facility
            EscapeFacility();
        }
        else
        {
            // Door is locked
            PlayLockedDoorSound();
        }
    }
    
    private void EscapeFacility()
    {
        if (hasEscaped) return;
        
        hasEscaped = true;
        
        Debug.Log("Player has escaped the facility!");
        
        // Play door opening sound via InteractionSoundManager
        if (InteractionSoundManager.Instance != null)
        {
            InteractionSoundManager.Instance.PlayDoorOpen();
        }
        
        // Disable player movement immediately
        PlayerInteractionManager interactionManager = FindObjectOfType<PlayerInteractionManager>();
        if (interactionManager != null)
        {
            interactionManager.SetInteractionEnabled(false);
        }

        // Disable player input using the same system as other UIs
        UIInputController uiInputController = FindObjectOfType<UIInputController>();
        if (uiInputController != null)
        {
            uiInputController.DisableGameplayInput();
        }

        // Show escape ending
        GameHUDManager hudManager = FindObjectOfType<GameHUDManager>();
        if (hudManager != null)
        {
            hudManager.ShowEscapeOutcome();
        }
    }
    
    private void PlayLockedDoorSound()
    {
        // Play locked door sound via InteractionSoundManager
        if (InteractionSoundManager.Instance != null)
        {
            InteractionSoundManager.Instance.PlayExitDoorLocked();
        }
        
        Debug.Log("Exit door is locked - escape window not active");
    }
    
    // Called by LockdownManager
    public void SetEscapeWindowActive(bool active)
    {
        isEscapeWindowActive = active;
        
        if (active)
        {
            Debug.Log("Escape window ACTIVE - Exit door unlocked");
        }
        else
        {
            Debug.Log("Escape window CLOSED - Exit door locked again");
        }
    }
    
    // For debugging
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isEscapeWindowActive ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}