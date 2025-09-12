using UnityEngine;
using StarterAssets;

public class FootstepController : MonoBehaviour
{
    [Header("Footstep Settings")]
    [SerializeField] private float walkingStepInterval = 0.5f; // Time between walking steps
    [SerializeField] private float runningStepInterval = 0.3f; // Time between running steps
    [SerializeField] private float movementThreshold = 0.1f; // Minimum movement to play footsteps
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    // References
    private FirstPersonController firstPersonController;
    private StarterAssetsInputs starterAssetsInputs;
    private CharacterController characterController;
    
    // Internal variables
    private float stepTimer = 0f;
    private Vector3 lastPosition;
    private bool wasMoving = false;
    
    void Start()
    {
        // Get references from this GameObject (since script is attached to PlayerCapsule)
        firstPersonController = GetComponent<FirstPersonController>();
        starterAssetsInputs = GetComponent<StarterAssetsInputs>();
        characterController = GetComponent<CharacterController>();
                
        if (firstPersonController == null || starterAssetsInputs == null || characterController == null)
        {
            Debug.LogError("FootstepController: Missing required components on this GameObject!");
            enabled = false;
            return;
        }
        
        // Initialize position tracking
        lastPosition = transform.position;
        
        Debug.Log("FootstepController initialized successfully");
    }
    
    void Update()
    {
        // Check if player is moving
        Vector3 currentPosition = transform.position;
        Vector3 movement = currentPosition - lastPosition;
        float movementMagnitude = movement.magnitude;
        
        bool isMoving = movementMagnitude > movementThreshold && characterController.isGrounded;
        bool isRunning = starterAssetsInputs.sprint && isMoving;
                
        if (isMoving)
        {
            // Update step timer
            stepTimer += Time.deltaTime;
            
            // Determine step interval based on running/walking
            float currentStepInterval = isRunning ? runningStepInterval : walkingStepInterval;
                        
            // Play footstep if enough time has passed
            if (stepTimer >= currentStepInterval)
            {
                PlayFootstepSound(isRunning);
                stepTimer = 0f; // Reset timer
            }
        }
        else
        {
            // Reset timer when not moving
            stepTimer = 0f;
        }
        
        // Update tracking variables
        lastPosition = currentPosition;
        wasMoving = isMoving;
    }
    
    private void PlayFootstepSound(bool isRunning)
    {
        if (isRunning)
        {
            InteractionSoundManager.Instance.PlayRunningFootstep();
        }
        else
        {
            InteractionSoundManager.Instance.PlayWalkingFootstep();
        }
    }

}