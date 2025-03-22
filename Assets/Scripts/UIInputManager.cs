using UnityEngine;

/// <summary>
/// Helper class for managing UI input state transitions
/// Add to any UI panel that needs to control player input
/// </summary>
public class UIInputManager : MonoBehaviour
{
    // Reference to the InputManager singleton
    private InputManager inputManager;
    
    private void Start()
    {
        // Get the InputManager instance
        inputManager = InputManager.Instance;
        
        // Optional: Automatically disable player input when this UI is enabled
        if (gameObject.activeInHierarchy && inputManager != null)
        {
            DisablePlayerInput();
        }
    }
    
    // Call this when opening a UI panel
    public void DisablePlayerInput()
    {
        if (inputManager != null)
        {
            inputManager.SetUIMode(true);
        }
        else
        {
            Debug.LogWarning("InputManager not found. Make sure it's been created.");
            
            // Fallback to direct cursor control
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
    
    // Call this when closing a UI panel
    public void EnablePlayerInput()
    {
        if (inputManager != null)
        {
            inputManager.SetUIMode(false);
        }
        else
        {
            Debug.LogWarning("InputManager not found. Make sure it's been created.");
            
            // Fallback to direct cursor control
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
    
    // Automatically re-enable player input when this UI is disabled/destroyed
    private void OnDisable()
    {
        if (inputManager != null)
        {
            EnablePlayerInput();
        }
    }
}