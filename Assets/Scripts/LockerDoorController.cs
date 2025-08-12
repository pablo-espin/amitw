using UnityEngine;
using System.Collections;

public class LockerDoorController : MonoBehaviour
{
    [Header("Interaction Prompts")]
    [SerializeField] private string openPrompt = "Press E to open locker";
    [SerializeField] private string closePrompt = "Press E to close locker";

    [Header("Door Settings")]
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float openSpeed = 2f;
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private Transform hingePoint; // The parent hinge GameObject
    [SerializeField] private Transform doorMesh; // The actual door mesh (if this script is on hinge)
    [SerializeField] private Vector3 hingeOffset = Vector3.zero; // Offset from door center to hinge

    [Header("Manual Reference")]
    [SerializeField] private ManualInteractable manualInteractable;

    [Header("Audio")]
    [SerializeField] private AudioSource doorAudioSource;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    // Door state
    private bool isOpen = false;
    private bool isInRange = false;
    private float currentAngle = 0f;
    private Transform playerTransform;
    private bool isAnimating = false;
    
    // Store initial rotation
    private float initialZRotation; // Keep the variable name for consistency
    private float lastInteractionTime = 0f;
    private float debounceTime = 0.5f;

    private void Start()
    {
        // Find player
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform == null)
        {
            Debug.LogError("Player not found! Make sure the player has the 'Player' tag.");
            playerTransform = Camera.main?.transform;
        }

        // Determine which transform to animate based on script location
        Transform transformToAnimate = GetTransformToAnimate();

        // Store initial Y rotation
        initialZRotation = transformToAnimate.localRotation.eulerAngles.y;
        currentAngle = initialZRotation;

        // Auto-find manual if not assigned
        if (manualInteractable == null)
        {
            manualInteractable = FindObjectOfType<ManualInteractable>();
            if (manualInteractable != null && showDebugInfo)
            {
                Debug.Log($"Auto-found manual: {manualInteractable.gameObject.name}");
            }
        }

        // Ensure manual interaction starts disabled
        if (manualInteractable != null)
        {
            manualInteractable.SetInteractionEnabled(false);
        }

        // Setup audio source if not assigned
        if (doorAudioSource == null)
        {
            doorAudioSource = GetComponent<AudioSource>();
            if (doorAudioSource == null)
            {
                doorAudioSource = gameObject.AddComponent<AudioSource>();
                doorAudioSource.spatialBlend = 1f; // 3D sound
                doorAudioSource.playOnAwake = false;
            }
        }

        if (showDebugInfo)
        {
            Debug.Log($"LockerDoor initialized - Initial Y rotation: {initialZRotation}°");
            Debug.Log($"Script on: {gameObject.name}, Animating: {transformToAnimate.name}");
        }
    }

    private void Update()
    {
        // Check player distance
        if (playerTransform != null)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            bool wasInRange = isInRange;
            isInRange = distance <= interactionDistance;
            
            // Debug range changes
            if (showDebugInfo && (wasInRange != isInRange || Time.frameCount % 60 == 0))
            {
                Debug.Log($"Range check - InRange: {isInRange}, Distance: {distance:F2}, InteractionDistance: {interactionDistance}");
                Debug.Log($"Door position: {transform.position}, Player position: {playerTransform.position}");
            }
        }

        // Handle door animation
        if (isAnimating)
        {
            AnimateDoor();
        }
    }

    public string GetInteractionPrompt()
    {
        if (showDebugInfo)
        {
            Debug.Log($"LockerDoor GetInteractionPrompt - IsAnimating: {isAnimating}, IsOpen: {isOpen}");
            if (manualInteractable != null)
            {
                Debug.Log($"Manual available: {manualInteractable.IsManualAvailable()}");
            }
        }

        // First check - not in range or animating
        if (isAnimating)
        {
            if (showDebugInfo)
                Debug.Log("No prompt - not in range or animating");
            return "";
        }

        // Second check - door is closed, always show open prompt
        if (!isOpen)
        {
            if (showDebugInfo)
                Debug.Log("Showing open prompt");
            return openPrompt;
        }
        
        // Third check - door is open, check manual status
        if (manualInteractable != null)
        {
            bool manualAvailable = manualInteractable.IsManualAvailable();
            if (showDebugInfo)
                Debug.Log($"Door open - Manual available: {manualAvailable}");
                
            if (manualAvailable)
            {
                // Manual is available, don't show door prompt (manual prompt will show instead)
                if (showDebugInfo)
                    Debug.Log("Manual available - no door prompt");
                return "";
            }
            else
            {
                // Manual is taken, show close prompt
                if (showDebugInfo)
                    Debug.Log("Manual taken - showing close prompt");
                return closePrompt;
            }
        }
        else
        {
            // No manual reference, just show close prompt when open
            if (showDebugInfo)
                Debug.Log("No manual reference - showing close prompt");
            return closePrompt;
        }
    }

    public void Interact()
    {
        if (showDebugInfo)
            Debug.Log($"Locker Interact called - Time: {Time.time}, LastInteraction: {lastInteractionTime}, IsAnimating: {isAnimating}");

        // Prevent multiple interactions in quick succession
        if (Time.time - lastInteractionTime < debounceTime)
        {
            if (showDebugInfo)
                Debug.Log("Locker interaction debounced - too soon");
            return;
        }

        if (isAnimating)
        {
            if (showDebugInfo)
                Debug.Log("Locker door is animating - interaction ignored");
            return;
        }

        lastInteractionTime = Time.time;

        if (!isOpen)
        {
            OpenDoor();
        }
        else
        {
            // Only close if manual is not available (taken or doesn't exist)
            if (manualInteractable == null || !manualInteractable.IsManualAvailable())
            {
                CloseDoor();
            }
            else if (showDebugInfo)
            {
                Debug.Log("Cannot close locker - manual still inside");
            }
        }
    }

    private void OpenDoor()
    {
        if (showDebugInfo)
            Debug.Log("Opening locker door");

        isOpen = true;
        isAnimating = true;

        // Play metallic door sound
        if (InteractionSoundManager.Instance != null)
        {
            InteractionSoundManager.Instance.PlayLockerDoorOpen();
        }

        // Enable manual interaction when door opens
        if (manualInteractable != null)
        {
            manualInteractable.SetInteractionEnabled(true);
            if (showDebugInfo)
                Debug.Log("Manual interaction enabled");
        }
    }

    private void CloseDoor()
    {
        if (showDebugInfo)
            Debug.Log("Closing locker door");

        isOpen = false;
        isAnimating = true;

        // Play metallic door sound
        if (InteractionSoundManager.Instance != null)
        {
            InteractionSoundManager.Instance.PlayLockerDoorClose();
        }

        // Disable manual interaction when door closes
        if (manualInteractable != null)
        {
            manualInteractable.SetInteractionEnabled(false);
            if (showDebugInfo)
                Debug.Log("Manual interaction disabled");
        }
    }

    private void AnimateDoor()
    {
        Transform transformToAnimate = GetTransformToAnimate();
        
        float targetAngle = isOpen ? initialZRotation + openAngle : initialZRotation;
        
        // Calculate rotation step
        float rotationStep = openSpeed * Time.deltaTime * 60f;
        
        // Determine rotation direction
        float angleDifference = targetAngle - currentAngle;
        if (Mathf.Abs(angleDifference) > 180f)
        {
            // Handle angle wrapping
            if (angleDifference > 0)
                angleDifference -= 360f;
            else
                angleDifference += 360f;
        }
        
        // Move towards target
        if (Mathf.Abs(angleDifference) <= rotationStep)
        {
            currentAngle = targetAngle;
        }
        else
        {
            currentAngle += Mathf.Sign(angleDifference) * rotationStep;
        }
        
        // Apply rotation to the correct transform
        Vector3 currentRotation = transformToAnimate.localRotation.eulerAngles;
        transformToAnimate.localRotation = Quaternion.Euler(currentRotation.x, currentAngle, currentRotation.z);
        
        // Check if animation is complete
        if (Mathf.Approximately(currentAngle, targetAngle))
        {
            isAnimating = false;
            if (showDebugInfo)
                Debug.Log($"Door animation complete - Final angle: {currentAngle}°");
        }
    }

    // Helper method to determine which transform should be animated
    private Transform GetTransformToAnimate()
    {
        // If script is on the door mesh and hingePoint is assigned, animate the hinge
        if (hingePoint != null)
        {
            return hingePoint;
        }
        // If script is on the hinge and doorMesh is assigned, animate the door
        else if (doorMesh != null)
        {
            return doorMesh;
        }
        // Otherwise animate this object
        else
        {
            return transform;
        }
    }

    // Public method to check door state (for other systems)
    public bool IsOpen()
    {
        return isOpen;
    }

    // Public method to check if door is animating
    public bool IsAnimating()
    {
        return isAnimating;
    }

    // Called by ManualInteractable when manual is picked up
    public void OnManualPickedUp()
    {
        if (showDebugInfo)
            Debug.Log("Manual picked up - locker can now be closed");
    }

    private void OnDrawGizmosSelected()
    {
        // Draw interaction range
        Gizmos.color = isInRange ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
        
        // Draw door swing arc when selected
        if (Application.isPlaying)
        {
            Gizmos.color = isOpen ? Color.red : Color.blue;
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;
            
            // Draw arc showing door swing
            for (int i = 0; i <= 90; i += 10)
            {
                float angle = i * Mathf.Deg2Rad;
                Vector3 direction = forward * Mathf.Cos(angle) + right * Mathf.Sin(angle);
                Gizmos.DrawLine(transform.position, transform.position + direction * 1f);
            }
        }
    }
}