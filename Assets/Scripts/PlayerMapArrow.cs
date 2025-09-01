using UnityEngine;
using UnityEngine.UI;

public class PlayerMapArrow : MonoBehaviour
{
    [Header("Arrow Settings")]
    [SerializeField] private RectTransform arrowTransform;
    [SerializeField] private Transform playerCameraTransform;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    private void Start()
    {
        // If not assigned, try to find the camera transform
        if (playerCameraTransform == null)
        {
            playerCameraTransform = Camera.main.transform;
        }
        
        // If arrow transform not assigned, use this transform
        if (arrowTransform == null)
        {
            arrowTransform = GetComponent<RectTransform>();
        }
    }
    
    // Updates the arrow rotation based on player's camera direction
    // Accounts for the 90-degree coordinate system rotation used in the map
    public void UpdateArrowDirection()
    {
        if (arrowTransform == null || playerCameraTransform == null)
            return;
        
        // Get player's forward direction in world space
        Vector3 worldForward = playerCameraTransform.forward;
        
        // Convert world direction to map direction accounting for the 90-degree rotation
        // World X (east-west) maps to UI Y (top-bottom) with inverted mapping
        // World Z (north-south) maps to UI X (left-right) with direct mapping
        
        // Calculate the angle in world space (Y-axis rotation)
        float worldAngle = Mathf.Atan2(worldForward.x, worldForward.z) * Mathf.Rad2Deg;
        
        // Apply the 90-degree clockwise rotation to match map coordinate system
        // The map is rotated 90 degrees clockwise from world coordinates
        float mapAngle = worldAngle + 90f;
        
        // Apply rotation to the arrow
        arrowTransform.rotation = Quaternion.Euler(0, 0, -mapAngle);
        
        if (showDebugInfo)
        {
            Debug.Log($"World Forward: {worldForward}, World Angle: {worldAngle:F1}°, Map Angle: {mapAngle:F1}°");
        }
    }

    // Update both position and rotation of the arrow marker
    public void UpdateArrowPositionAndDirection(Vector2 newPosition)
    {
        if (arrowTransform == null)
            return;

        // Update position
        arrowTransform.anchoredPosition = newPosition;

        // Update direction
        UpdateArrowDirection();
    }
    
    // Set the camera transform reference
    public void SetCameraTransform(Transform cameraTransform)
    {
        playerCameraTransform = cameraTransform;
    }
}