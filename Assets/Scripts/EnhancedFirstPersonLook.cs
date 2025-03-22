using UnityEngine;

/// <summary>
/// Enhanced version of FirstPersonLook that properly handles UI interactions
/// </summary>
public class EnhancedFirstPersonLook : MonoBehaviour
{
    [SerializeField]
    Transform character;
    public float sensitivity = 2;
    public float smoothing = 1.5f;

    Vector2 velocity;
    Vector2 frameVelocity;
    
    // References
    private InputManager inputManager;
    
    // Additional state tracking
    private bool uiActive = false;
    private bool initializedInputManager = false;
    
    // For checking cursor state
    private CursorLockMode lastCursorLockState;
    private bool lastCursorVisibleState;

    void Reset()
    {
        // Get the character from the FirstPersonMovement in parents.
        character = GetComponentInParent<FirstPersonMovement>().transform;
    }

    void Awake()
    {
        // Try to find InputManager if it exists
        inputManager = FindObjectOfType<InputManager>();
        initializedInputManager = (inputManager != null);
        
        if (initializedInputManager)
        {
            Debug.Log("EnhancedFirstPersonLook found InputManager");
        }
        else
        {
            Debug.LogWarning("EnhancedFirstPersonLook couldn't find InputManager, will use cursor state fallback");
        }
    }

    void Start()
    {
        // Initial cursor lock only if no UI is active
        if (!IsUIActive())
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        // Save initial cursor state
        lastCursorLockState = Cursor.lockState;
        lastCursorVisibleState = Cursor.visible;
    }

    void Update()
    {
        // Update UI active state
        UpdateUIActiveState();
        
        // Skip input processing if UI is active
        if (IsUIActive())
        {
            return;
        }

        // Get smooth velocity.
        Vector2 mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        Vector2 rawFrameVelocity = Vector2.Scale(mouseDelta, Vector2.one * sensitivity);
        frameVelocity = Vector2.Lerp(frameVelocity, rawFrameVelocity, 1 / smoothing);
        velocity += frameVelocity;
        velocity.y = Mathf.Clamp(velocity.y, -90, 90);

        // Rotate camera up-down and controller left-right from velocity.
        transform.localRotation = Quaternion.AngleAxis(-velocity.y, Vector3.right);
        character.localRotation = Quaternion.AngleAxis(velocity.x, Vector3.up);
    }
    
    private void UpdateUIActiveState()
    {
        // Check for cursor state changes
        bool cursorStateChanged = 
            (lastCursorLockState != Cursor.lockState) || 
            (lastCursorVisibleState != Cursor.visible);
            
        if (cursorStateChanged)
        {
            // Log cursor state changes for debugging
            Debug.Log($"Cursor state changed - LockState: {Cursor.lockState}, Visible: {Cursor.visible}");
            
            // If cursor is unlocked and visible, a UI is likely active
            if (Cursor.lockState == CursorLockMode.None && Cursor.visible)
            {
                uiActive = true;
            }
            else if (Cursor.lockState == CursorLockMode.Locked && !Cursor.visible)
            {
                uiActive = false;
            }
            
            // Update our stored state
            lastCursorLockState = Cursor.lockState;
            lastCursorVisibleState = Cursor.visible;
        }
    }

    public void ResetCameraRotation()
    {
        // Reset velocity values to prevent any residual rotation
        velocity = Vector2.zero;
        frameVelocity = Vector2.zero;
        
        // Force UI active state
        uiActive = true;
        
        Debug.Log("Camera rotation state forcibly reset");
    }    
    
    private bool IsUIActive()
    {
        // First priority: check InputManager if available
        if (initializedInputManager && inputManager != null)
        {
            return !inputManager.IsCameraInputEnabled;
        }
        
        // Second priority: use our tracked UI state
        return uiActive || Cursor.lockState == CursorLockMode.None;
    }
}