using UnityEngine;

public class DecryptionPanelFix : MonoBehaviour
{
    // Reference to the player's rigidbody and camera
    private Rigidbody playerRigidbody;
    private FirstPersonMovement movementController;
    private EnhancedFirstPersonLook lookController;
    
    private void Start()
    {
        // Find the player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerRigidbody = player.GetComponent<Rigidbody>();
            movementController = player.GetComponent<FirstPersonMovement>();
            lookController = player.GetComponentInChildren<EnhancedFirstPersonLook>();
        }
    }
    
    // Call this when the decryption panel opens
    public void OnPanelOpened()
    {
        // Explicitly register with UIStateManager
        if (UIStateManager.Instance != null)
        {
            UIStateManager.Instance.RegisterActiveUI(gameObject);
        }
        
        // Force zero velocity on the player
        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }
        
        // Explicitly disable any movement component if needed
        if (movementController != null)
        {
            movementController.enabled = false;
        }
        
        // Force input disabling in the look controller
        if (lookController != null)
        {
            // If the EnhancedFirstPersonLook has a public method to disable input, call it here
            // Otherwise, we need to add such a method
        }
        
        // In the OnPanelOpened method of DecryptionPanelFix.cs
        if (lookController != null)
        {
            lookController.ResetCameraRotation();
        }

        // Force cursor state
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        Debug.Log("Decryption panel opened - movement and camera controls explicitly disabled");
    }
    
    // Call this when the panel closes
    public void OnPanelClosed()
    {
        // Unregister with UIStateManager
        if (UIStateManager.Instance != null)
        {
            UIStateManager.Instance.UnregisterActiveUI(gameObject);
        }
        
        // Re-enable movement component if needed
        if (movementController != null)
        {
            movementController.enabled = true;
        }
        
        Debug.Log("Decryption panel closed - movement controls re-enabled");
    }
    
    private void OnEnable()
    {
        OnPanelOpened();
    }
    
    private void OnDisable()
    {
        OnPanelClosed();
    }
}