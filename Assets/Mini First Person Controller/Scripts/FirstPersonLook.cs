using UnityEngine;

public class FirstPersonLook : MonoBehaviour
{
    [SerializeField]
    Transform character;
    public float sensitivity = 2;
    public float smoothing = 1.5f;

    Vector2 velocity;
    Vector2 frameVelocity;
    
    // References
    private InputManager inputManager;

    // Flag to help with debugging
    private bool wasInputEnabledLastFrame = true;

    void Reset()
    {
        // Get the character from the FirstPersonMovement in parents.
        character = GetComponentInParent<FirstPersonMovement>().transform;
    }

    void Start()
    {
        // Get reference to input manager
        inputManager = InputManager.Instance;
        
        // Only lock cursor if not in a UI
        if (inputManager == null || inputManager.IsCameraInputEnabled)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
        
        Debug.Log("FirstPersonLook started. InputManager reference: " + (inputManager != null ? "Found" : "Not found"));
    }

    void Update()
    {
        // Check input status for debugging
        bool inputEnabled = (inputManager != null) ? inputManager.IsCameraInputEnabled : (Cursor.lockState == CursorLockMode.Locked);
        
        // Log when input state changes (for debugging)
        if (inputEnabled != wasInputEnabledLastFrame)
        {
            Debug.Log("Camera rotation is now " + (inputEnabled ? "ENABLED" : "DISABLED"));
            wasInputEnabledLastFrame = inputEnabled;
        }
        
        // HARD CHECK: If cursor is visible and free, force disable camera rotation
        if (Cursor.visible && Cursor.lockState == CursorLockMode.None)
        {
            inputEnabled = false;
        }
        
        // Only process camera movement if camera input is enabled
        if (inputEnabled)
        {
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
    }
}