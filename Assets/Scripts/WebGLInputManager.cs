using UnityEngine;

public class WebGLManager : MonoBehaviour
{
    [SerializeField] private bool autoLockCursor = true;
    private bool isLocked = false;

    void Start()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
            // Set initial cursor state
            if (autoLockCursor)
            {
                LockCursor();
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
        Cursor.lockState = CursorLockMode.Locked;
        isLocked = true;
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        isLocked = false;
    }
}