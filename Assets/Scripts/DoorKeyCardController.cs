using UnityEngine;
using System.Collections;

public class DoorKeyCardController : MonoBehaviour
{
    [Header("Door Settings")]
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float openSpeed = 2f;
    [SerializeField] private float interactionDistance = 3f;
    
    [Header("References")]
    [SerializeField] private KeyCardAccessManager keyCardManager;
    [SerializeField] private BoxCollider doorCollider;
    [SerializeField] private MeshRenderer keyCardLightRenderer; // The light indicator on key card panel

    [Header("Materials")]
    [SerializeField] private Material redMaterial; // Red emissive material
    [SerializeField] private Material greenMaterial; // Green emissive material
    [SerializeField] private Material blackMaterial; // Black material for when the key card lock flashes    
    
    [Header("Audio")]
    [SerializeField] private AudioSource doorAudioSource;
    [SerializeField] private AudioClip doorOpenSound;
    [SerializeField] private AudioClip keyCardDeniedSound;
    [SerializeField] private AudioClip keyCardAcceptedSound;
    
    [Header("Narration")]
    [SerializeField] private bool useNarration = true;
    [SerializeField] private string noKeyCardDialogueID = "door_no_keycard";
    [SerializeField] private string keyCardUsedDialogueID = "door_keycard_used";
    
    private bool isOpen = false;
    private bool isInRange = false;
    private float currentAngle = 0f;
    private Transform playerTransform;
    private bool hasTriggeredNoKeyCardDialogue = false;

    private void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform == null)
        {
            Debug.LogError("Player not found! Make sure the player has the 'Player' tag.");
            playerTransform = Camera.main?.transform;
        }
            
        // Set initial key card light state to red
        if (keyCardLightRenderer != null && redMaterial != null)
        {
            keyCardLightRenderer.material = redMaterial;
        }
        
        // Find key card manager if not set
        if (keyCardManager == null)
        {
            keyCardManager = FindObjectOfType<KeyCardAccessManager>();
            if (keyCardManager == null)
            {
                Debug.LogError("KeyCardAccessManager not found in scene!");
            }
        }
    }

    private void Update()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        isInRange = distanceToPlayer <= interactionDistance;

        // if (isInRange && Input.GetKeyDown(KeyCode.E) && !isOpen)
        // {
        //     TryOpenDoor();
            
        //     // Trigger narration about needing a key card if:
        //     // 1. Player doesn't have a key card
        //     // 2. We haven't triggered this narration before
        //     // 3. Narration is enabled
        //     if (keyCardManager != null && !keyCardManager.HasKeyCard() && !hasTriggeredNoKeyCardDialogue && useNarration)
        //     {
        //         TriggerNoKeyCardNarration();
        //     }
        // }

        if (isOpen)
        {
            // Animate door opening
            currentAngle = Mathf.MoveTowards(currentAngle, openAngle, openSpeed * Time.deltaTime * 60f);
            transform.localRotation = Quaternion.Euler(-90f, 0f, currentAngle);
        }
    }
    
    public void TryOpenDoor()
    {
        if (keyCardManager == null)
            return;

        if (keyCardManager.HasKeyCard())
        {
            // Key card accepted
            if (keyCardLightRenderer != null && greenMaterial != null)
            {
                keyCardLightRenderer.material = greenMaterial;
            }
            
            // Play key card accepted sound
            if (doorAudioSource != null && keyCardAcceptedSound != null)
            {
                doorAudioSource.clip = keyCardAcceptedSound;
                doorAudioSource.Play();
            }
            
            // Trigger narration if enabled
            if (useNarration)
            {
                TriggerKeyCardUsedNarration();
            }
            
            // Wait a moment before opening the door
            StartCoroutine(OpenDoorAfterDelay(0.5f));
        }
        else
        {
            // Key card denied
            if (doorAudioSource != null && keyCardDeniedSound != null)
            {
                doorAudioSource.clip = keyCardDeniedSound;
                doorAudioSource.Play();
            }
            
            // Flash the red light
            if (keyCardLightRenderer != null)
            {
                StartCoroutine(FlashRedLight());
            }

            // Moved from Update: Trigger narration about needing a key card
            if (useNarration && !hasTriggeredNoKeyCardDialogue)
            {
                TriggerNoKeyCardNarration();
            }
        }
    }
    
    private IEnumerator OpenDoorAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Open the door
        isOpen = true;
        
        // Disable the collider when door opens
        if (doorCollider != null)
            doorCollider.enabled = false;
            
        // Play door open sound
        if (doorAudioSource != null && doorOpenSound != null)
        {
            doorAudioSource.clip = doorOpenSound;
            doorAudioSource.Play();
        }
    }
    
    private IEnumerator FlashRedLight()
    {
        Debug.Log("Starting flash sequence");
        
        // Store original material
        Material originalMaterial = keyCardLightRenderer.material;
        Debug.Log("Original material: " + originalMaterial.name);
        
        // Flash to black
        Debug.Log("Setting to black material 1");
        keyCardLightRenderer.material = blackMaterial;
        keyCardLightRenderer.enabled = true; // Ensure renderer is enabled
        
        yield return new WaitForSeconds(0.1f);
        
        // Flash to red
        Debug.Log("Setting back to red material 2");
        keyCardLightRenderer.material = originalMaterial;
        keyCardLightRenderer.enabled = true;
        
        yield return new WaitForSeconds(0.1f);
        
        // Flash to black again
        Debug.Log("Setting to black material again 3");
        keyCardLightRenderer.material = blackMaterial;
        keyCardLightRenderer.enabled = true;
        
        yield return new WaitForSeconds(0.1f);
        
        // Back to original red
        Debug.Log("Final setting back to red material 4");
        keyCardLightRenderer.material = originalMaterial;
        keyCardLightRenderer.enabled = true;
        
        Debug.Log("Flash sequence complete");
    }

    private void TriggerNoKeyCardNarration()
    {
        hasTriggeredNoKeyCardDialogue = true;
        
        if (GameInteractionDialogueManager.Instance != null)
        {
            GameInteractionDialogueManager.Instance.OnDoorWithoutKeyCard();
        }
    }
    
    private void TriggerKeyCardUsedNarration()
    {
        if (GameInteractionDialogueManager.Instance != null)
        {
            GameInteractionDialogueManager.Instance.OnKeyCardUsed();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}