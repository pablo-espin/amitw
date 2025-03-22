using UnityEngine;

public class InputManager : MonoBehaviour
{
    // Singleton pattern
    public static InputManager Instance { get; private set; }
    
    // State tracking
    private bool _playerInputEnabled = true;
    private bool _cameraInputEnabled = true;
    
    // Subscribe to these events to get notified of input state changes
    public delegate void InputStateChangedEvent(bool isEnabled);
    public event InputStateChangedEvent OnPlayerInputChanged;
    public event InputStateChangedEvent OnCameraInputChanged;
    
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
    }
    
    // Public accessors to check input state
    public bool IsPlayerInputEnabled => _playerInputEnabled;
    public bool IsCameraInputEnabled => _cameraInputEnabled;
    
    // Methods to enable/disable player movement
    public void EnablePlayerInput(bool enable)
    {
        if (_playerInputEnabled != enable)
        {
            _playerInputEnabled = enable;
            OnPlayerInputChanged?.Invoke(enable);
            Debug.Log($"Player input {(enable ? "enabled" : "disabled")}");
        }
    }
    
    // Methods to enable/disable camera movement
    public void EnableCameraInput(bool enable)
    {
        if (_cameraInputEnabled != enable)
        {
            _cameraInputEnabled = enable;
            OnCameraInputChanged?.Invoke(enable);
            Debug.Log($"Camera input {(enable ? "enabled" : "disabled")}");
        }
    }
    
    // Convenience method to enable/disable both at once
    public void EnableAllInput(bool enable)
    {
        EnablePlayerInput(enable);
        EnableCameraInput(enable);
    }
    
    // Helper to handle UI state
    public void SetUIMode(bool active)
    {
        // When UI is active, disable player and camera input, and show the cursor
        EnableAllInput(!active);
        
        if (active)
        {
            // Show cursor for UI interaction
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // Hide cursor for gameplay
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}