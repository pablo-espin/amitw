using UnityEngine;

/// <summary>
/// Add this component to UI elements that should disable game input when active
/// </summary>
public class UIElementTracker : MonoBehaviour
{
    // Optional: Only register if this GameObject is active and this component is enabled
    [SerializeField] private bool onlyTrackWhenActive = true;
    
    private bool wasRegistered = false;
    
    private void OnEnable()
    {
        if (UIStateManager.Instance != null && (!onlyTrackWhenActive || gameObject.activeInHierarchy))
        {
            UIStateManager.Instance.RegisterActiveUI(gameObject);
            wasRegistered = true;
        }
    }
    
    private void OnDisable()
    {
        if (UIStateManager.Instance != null && wasRegistered)
        {
            UIStateManager.Instance.UnregisterActiveUI(gameObject);
            wasRegistered = false;
        }
    }
    
    private void OnDestroy()
    {
        if (UIStateManager.Instance != null && wasRegistered)
        {
            UIStateManager.Instance.UnregisterActiveUI(gameObject);
            wasRegistered = false;
        }
    }
}