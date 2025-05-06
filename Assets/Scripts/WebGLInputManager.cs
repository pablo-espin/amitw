using UnityEngine;

public class WebGLManager : MonoBehaviour
{
    [SerializeField] private bool autoLockCursor = true;
    private bool isLocked = false;

    void Start()
    {
        Debug.Log("WebGLManager starting...");
        
        #if UNITY_WEBGL && !UNITY_EDITOR
            // Set initial cursor state
            if (autoLockCursor)
            {
                // Don't lock immediately in WebGL - wait for user interaction
                isLocked = false;
                Cursor.lockState = CursorLockMode.None;
                Debug.Log("Initial cursor state set to None for WebGL");
            }
        #endif
    }

    void Update()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
            // Lock cursor on click if not locked
            if (!isLocked && Input.GetMouseButtonDown(0))
            {
                LockCursor();
            }
            
            // Allow escape to unlock cursor
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                UnlockCursor();
            }
        #endif
    }

    private void LockCursor()
    {
        Debug.Log("Attempting to lock cursor");
        try {
            Cursor.lockState = CursorLockMode.Locked;
            isLocked = true;
            Debug.Log("Cursor locked successfully");
        } catch (System.Exception e) {
            Debug.LogError("Failed to lock cursor: " + e.Message);
        }
    }

    private void UnlockCursor()
    {
        Debug.Log("Unlocking cursor");
        Cursor.lockState = CursorLockMode.None;
        isLocked = false;
    }
}