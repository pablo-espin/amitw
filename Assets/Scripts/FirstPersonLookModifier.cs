using UnityEngine;

public class FirstPersonLookModifier : MonoBehaviour
{
    // Reference to the FirstPersonLook component
    private FirstPersonLook firstPersonLook;
    
    // Cache the original values
    private float originalSensitivity;
    private float originalSmoothing;
    
    // Store mouse velocity for freezing
    private Vector2 lastVelocity;
    private Vector2 lastFrameVelocity;
    
    void Start()
    {
        // Find FirstPersonLook component
        firstPersonLook = GetComponent<FirstPersonLook>();
        
        if (firstPersonLook == null)
        {
            Debug.LogError("FirstPersonLookModifier: FirstPersonLook component not found on this GameObject");
            return;
        }
        
        // Store original values
        originalSensitivity = firstPersonLook.sensitivity;
        originalSmoothing = firstPersonLook.smoothing;
    }
    
    // Call this when opening a UI to properly freeze camera movement
    public void FreezeCamera()
    {
        if (firstPersonLook == null) return;
        
        // Get access to the private fields using reflection
        System.Reflection.FieldInfo velocityField = typeof(FirstPersonLook).GetField("velocity", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
        System.Reflection.FieldInfo frameVelocityField = typeof(FirstPersonLook).GetField("frameVelocity", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (velocityField != null && frameVelocityField != null)
        {
            // Store current values for restoration later
            lastVelocity = (Vector2)velocityField.GetValue(firstPersonLook);
            lastFrameVelocity = (Vector2)frameVelocityField.GetValue(firstPersonLook);
            
            // Reset to zero to stop motion
            velocityField.SetValue(firstPersonLook, Vector2.zero);
            frameVelocityField.SetValue(firstPersonLook, Vector2.zero);
        }
        
        // Set sensitivity to zero (alternative approach)
        firstPersonLook.sensitivity = 0;
        firstPersonLook.smoothing = 100f; // High smoothing further reduces any remaining movement
    }
    
    // Call this when closing a UI to restore camera movement
    public void UnfreezeCamera()
    {
        if (firstPersonLook == null) return;
        
        // Restore original values
        firstPersonLook.sensitivity = originalSensitivity;
        firstPersonLook.smoothing = originalSmoothing;
    }
}