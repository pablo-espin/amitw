using UnityEngine;

public class ProximityDialogueTrigger : MonoBehaviour
{
    [Header("Dialogue Settings")]
    [SerializeField] private AudioClip dialogueClip;
    [SerializeField] private string dialogueID;
    [SerializeField] private bool playOnce = true;
    [SerializeField] private float volume = 1f;
    [Tooltip("Text representation of the dialogue (for debugging)")]
    [SerializeField, TextArea(2, 5)] private string dialogueText;
    
    [Header("Trigger Settings")]
    [SerializeField] private float triggerRadius = 3f;
    [SerializeField] private bool requireLookingAt = false;
    [SerializeField] private float lookingAtAngle = 40f;
    [SerializeField] private bool showTriggerRadius = true;
    [SerializeField] private bool useFixedUpdate = true; // Add this for WebGL optimization
    
    private bool hasPlayed = false;
    private Transform player;
    private bool isPlayerInRange = false;
    private float lastCheckTime = 0f;
    private float checkInterval = 0.25f; // Check every 1/4 second instead of every frame
    
    private void Start()
    {
        // Move player finding to Start instead of OnEnable for WebGL
        FindPlayer();
        
        // Immediately check if player is already in range (for small levels)
        CheckPlayerProximity();

        // Log whether player was found
        Debug.Log($"ProximityTrigger '{dialogueID}': Player reference is {(player != null ? "FOUND" : "NULL")}");
    
        if (player != null) {
        Debug.Log($"Player position: {player.position}, Trigger position: {transform.position}, Distance: {Vector3.Distance(transform.position, player.position)}");
        }
    }
    
    private void FindPlayer()
    {
        // Try to find specifically by name first (most reliable for your setup)
        GameObject playerCapsule = GameObject.Find("PlayerCapsule");
        if (playerCapsule != null)
        {
            player = playerCapsule.transform;
            Debug.Log("ProximityDialogueTrigger: Found player by name 'PlayerCapsule'");
            return;
        }
        
        // Try by tag
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player != null)
        {
            Debug.Log("ProximityDialogueTrigger: Found player by 'Player' tag");
            return;
        }
        
        // If those fail, try by the main camera
        if (player == null)
        {
            player = Camera.main?.transform;
            Debug.Log("ProximityDialogueTrigger: Using Camera.main as player reference");
        }
        
        if (player == null)
        {
            Debug.LogWarning("ProximityDialogueTrigger: Could not find player reference!");
        }
    }
    
    private void Update()
    {
        if (!useFixedUpdate)
        {
            PerformProximityCheck();
        }
    }
    
    private void FixedUpdate()
    {
        if (useFixedUpdate)
        {
            PerformProximityCheck();
        }
    }
    
    private void PerformProximityCheck()
    {
        if (player == null || (playOnce && hasPlayed))
            return;
            
        // Only check periodically to improve performance
        if (Time.time - lastCheckTime < checkInterval)
            return;
            
        lastCheckTime = Time.time;
        
        // Check player proximity
        CheckPlayerProximity();
    }
    
    private void CheckPlayerProximity()
    {
        // Check if player is within trigger radius
        float distance = Vector3.Distance(transform.position, player.position);
        
        // Debug output to help troubleshoot
        if (showTriggerRadius && !hasPlayed)
        {
            Debug.Log($"Trigger {dialogueID}: Player distance = {distance}, Radius = {triggerRadius}");
        }
        
        if (distance <= triggerRadius)
        {
            bool canTrigger = true;
            
            // Check if player needs to be looking at the trigger
            if (requireLookingAt)
            {
                Vector3 directionToTrigger = (transform.position - player.position).normalized;
                float angle = Vector3.Angle(player.forward, directionToTrigger);
                canTrigger = angle <= lookingAtAngle;
            }
            
            if (canTrigger)
            {
                TriggerDialogue();
            }
        }
    }
    
    private void TriggerDialogue()
    {
        if (NarratorManager.Instance != null && dialogueClip != null)
        {
            Debug.Log($"Attempting to play dialogue: {dialogueID}");
            bool wasPlayed = NarratorManager.Instance.PlayDialogue(dialogueClip, dialogueID, false, volume);
            if (wasPlayed)
            {
                hasPlayed = true;
                Debug.Log($"Successfully played dialogue: {dialogueID}");
            }
            else
            {
                Debug.Log($"Failed to play dialogue: {dialogueID}");
            }
        }
        else
        {
            if (NarratorManager.Instance == null)
                Debug.LogError("NarratorManager.Instance is null!");
            if (dialogueClip == null)
                Debug.LogError($"dialogueClip is null for {dialogueID}!");
        }
    }
    
    // Reset the played state (for testing or scripted resets)
    public void ResetTrigger()
    {
        hasPlayed = false;
    }
    
    // Visual debugging
    private void OnDrawGizmos()
    {
        if (!showTriggerRadius)
            return;
            
        Gizmos.color = hasPlayed ? Color.gray : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
        
        if (requireLookingAt)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, Vector3.forward * (triggerRadius * 0.8f));
        }
    }
    
    // Public method to force-trigger from outside (useful for debugging)
    public void ForcePlayDialogue()
    {
        TriggerDialogue();
    }
}