using UnityEngine;

public class UIMovementBlocker : MonoBehaviour
{
    // References to the first-person controller components
    private FirstPersonLook lookScript;
    private FirstPersonMovement moveScript;
    
    // Track whether movement is currently blocked
    private bool isBlocked = false;
    
    void Start()
    {
        // Find the first-person controller components
        lookScript = FindObjectOfType<FirstPersonLook>();
        moveScript = FindObjectOfType<FirstPersonMovement>();
        
        if (lookScript == null || moveScript == null)
        {
            Debug.LogError("Could not find FirstPersonLook or FirstPersonMovement scripts!");
        }
    }
    
    // Call this when opening a UI
    public void BlockMovement()
    {
        if (isBlocked) return;

        // Reset all input to clear any ongoing camera movement
        Input.ResetInputAxes();

        isBlocked = true;

    // Try to reset camera velocity using reflection (optional)
    if (lookScript != null)
    {
        try
        {
            var velocityField = lookScript.GetType().GetField("velocity", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
            if (velocityField != null)
            {
                velocityField.SetValue(lookScript, Vector2.zero);
                Debug.Log("Camera velocity field reset to zero");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Could not reset camera velocity: " + e.Message);
        }
    }

        // Disable camera look and movement
        if (lookScript != null) lookScript.enabled = false;
        if (moveScript != null) moveScript.enabled = false;
        
        Debug.Log("Player movement and camera blocked");
    }
    
    // Call this when closing a UI
    public void UnblockMovement()
    {
        if (!isBlocked) return;
        
        isBlocked = false;
        
        // Re-enable camera look and movement
        if (lookScript != null) lookScript.enabled = true;
        if (moveScript != null) moveScript.enabled = true;
        
        Debug.Log("Player movement and camera unblocked");
    }
}