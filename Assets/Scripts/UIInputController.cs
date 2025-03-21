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
        if (playerInput != null && !string.IsNullOrEmpty(defaultActionMap))
        {
            // Switch back to gameplay controls
            playerInput.SwitchCurrentActionMap(defaultActionMap);
        }
        
        if (starterAssetsInputs != null)
        {
            // Reset any input values
            starterAssetsInputs.look = Vector2.zero;
            starterAssetsInputs.move = Vector2.zero;
            starterAssetsInputs.jump = false;
            starterAssetsInputs.sprint = false;
        }
        
        if (firstPersonController != null)
        {
            // Re-enable controller
            firstPersonController.enabled = true;
        }
        
        // Lock cursor for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    public void DisableGameplayInput()
    {
        if (playerInput != null)
        {
            // Switch to UI controls or disable current map
            playerInput.SwitchCurrentActionMap("UI"); // Create this action map or use ""
        }
        
        if (starterAssetsInputs != null)
        {
            // Zero out all inputs
            starterAssetsInputs.look = Vector2.zero;
            starterAssetsInputs.move = Vector2.zero;
            starterAssetsInputs.jump = false;
            starterAssetsInputs.sprint = false;
        }
        
        if (firstPersonController != null)
        {
            // Optionally disable the controller completely
            firstPersonController.enabled = false;
        }
        
        // Unlock cursor for UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}