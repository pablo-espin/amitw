using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// This manager tracks active UI elements and ensures consistent cursor and input states
/// </summary>
public class UIStateManager : MonoBehaviour
{
    // Singleton pattern
    public static UIStateManager Instance { get; private set; }
    
    // Track active UI elements
    private List<GameObject> activeUIElements = new List<GameObject>();
    
    // References
    private InputManager inputManager;
    
    // Debug options
    [SerializeField] private bool logStateChanges = true;
    
    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Find InputManager
        inputManager = FindObjectOfType<InputManager>();
    }
    
    /// <summary>
    /// Register a UI element as active
    /// </summary>
    public void RegisterActiveUI(GameObject uiElement)
    {
        if (!activeUIElements.Contains(uiElement))
        {
            activeUIElements.Add(uiElement);
            
            if (logStateChanges)
            {
                Debug.Log($"UI Element registered: {uiElement.name}. Total active: {activeUIElements.Count}");
            }
            
            UpdateInputState();
        }
    }
    
    /// <summary>
    /// Unregister a UI element (no longer active)
    /// </summary>
    public void UnregisterActiveUI(GameObject uiElement)
    {
        if (activeUIElements.Contains(uiElement))
        {
            activeUIElements.Remove(uiElement);
            
            if (logStateChanges)
            {
                Debug.Log($"UI Element unregistered: {uiElement.name}. Total active: {activeUIElements.Count}");
            }
            
            UpdateInputState();
        }
    }
    
    /// <summary>
    /// Check if any UI elements are active
    /// </summary>
    public bool IsAnyUIActive()
    {
        // Clean up any null references (destroyed objects)
        activeUIElements.RemoveAll(item => item == null);
        return activeUIElements.Count > 0;
    }
    
    /// <summary>
    /// Update input and cursor state based on active UI elements
    /// </summary>
    private void UpdateInputState()
    {
        bool anyUIActive = IsAnyUIActive();
        
        // Update InputManager if available
        if (inputManager != null)
        {
            inputManager.SetUIMode(anyUIActive);
        }
        
        // Direct cursor management as backup
        Cursor.lockState = anyUIActive ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = anyUIActive;
        
        if (logStateChanges)
        {
            Debug.Log($"Input state updated. UI Active: {anyUIActive}, Cursor visible: {Cursor.visible}, Cursor locked: {Cursor.lockState == CursorLockMode.Locked}");
        }
    }
    
    /// <summary>
    /// Manually force UI mode (for debugging)
    /// </summary>
    public void ForceUIMode(bool active)
    {
        Debug.Log($"Force UI mode: {active}");
        
        if (active)
        {
            // Create a temporary placeholder if needed
            if (activeUIElements.Count == 0)
            {
                GameObject placeholder = new GameObject("UIModePlaceholder");
                RegisterActiveUI(placeholder);
            }
        }
        else
        {
            // Clear all registered UIs
            activeUIElements.Clear();
        }
        
        UpdateInputState();
    }
}