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
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private bool showTriggerRadius = true;
    
    private bool hasPlayed = false;
    private Transform player;
    
    private void OnEnable()
    {
        // Find player once on enable
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null)
        {
            // Fallback to find the main camera if Player tag isn't set
            player = Camera.main?.transform;
        }
    }
    
    private void Update()
    {
        if (player == null || (playOnce && hasPlayed))
            return;
            
        // Check if player is within trigger radius
        float distance = Vector3.Distance(transform.position, player.position);
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
            bool wasPlayed = NarratorManager.Instance.PlayDialogue(dialogueClip, dialogueID, false, volume);
            if (wasPlayed)
                hasPlayed = true;
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
}