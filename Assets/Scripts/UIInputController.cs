using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets;

public class UIInputController : MonoBehaviour
{
    // Reference to the player's input components
    private StarterAssetsInputs starterAssetsInputs;
    private PlayerInput playerInput;
    private FirstPersonController firstPersonController;
    
    // Store the original input action map
    private string defaultActionMap;
    
    void Awake()
    {
        // Find references
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            starterAssetsInputs = player.GetComponent<StarterAssetsInputs>();
            playerInput = player.GetComponent<PlayerInput>();
            firstPersonController = player.GetComponent<FirstPersonController>();
            
            // Store default action map name
            if (playerInput != null)
            {
                defaultActionMap = playerInput.currentActionMap.name;
            }
        }
    }
    
    public void EnableGameplayInput()
    {
        if (playerInput != null)
        {
            // First re-enable input system
            playerInput.enabled = true;
            
            // Then switch back to gameplay controls
            if (!string.IsNullOrEmpty(defaultActionMap))
            {
                try {
                    playerInput.SwitchCurrentActionMap(defaultActionMap);
                }
                catch (System.Exception e) {
                    Debug.LogWarning("Could not switch action map: " + e.Message);
                }
            }
        }
        
        if (starterAssetsInputs != null)
        {
            // Reset any input values
            starterAssetsInputs.look = Vector2.zero;
            starterAssetsInputs.move = Vector2.zero;
            starterAssetsInputs.jump = false;
            starterAssetsInputs.sprint = false;
            
            // Re-enable cursor look
            starterAssetsInputs.cursorInputForLook = true;
        }
        
        if (firstPersonController != null)
        {
            // Re-enable controller
            firstPersonController.enabled = true;
        }
        
        // Lock cursor for gameplay
        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;

        // Use CursorManager instead
        if (CursorManager.Instance != null)
        {
            CursorManager.Instance.RequestCursorLock("UIInputController");
        }
    }
    
    public void DisableGameplayInput()
    {
        if (playerInput != null)
        {
            // Completely disable input system's processing of player input
            playerInput.enabled = false;
            
            // ALSO switch to UI action map as a secondary safeguard
            // playerInput.SwitchCurrentActionMap("UI");
        }
        
        if (starterAssetsInputs != null)
        {
            // Zero out all inputs
            starterAssetsInputs.look = Vector2.zero;
            starterAssetsInputs.move = Vector2.zero;
            starterAssetsInputs.jump = false;
            starterAssetsInputs.sprint = false;
            
            // Also disable the cursor input for look
            starterAssetsInputs.cursorInputForLook = false;
        }
        
        if (firstPersonController != null)
        {
            // Disable the controller completely
            firstPersonController.enabled = false;
        }
        
        // Unlock cursor for UI
        // Cursor.lockState = CursorLockMode.None;
        // Cursor.visible = true;

        // Use CursorManager instead
        if (CursorManager.Instance != null)
        {
            CursorManager.Instance.RequestCursorUnlock("UIInputController");
        }
    }
}