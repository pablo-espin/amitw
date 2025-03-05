using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorController : MonoBehaviour
{
    private bool _cursorLocked;

    void Start()
    {
        // Hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        _cursorLocked = true;
    }

    void Update()
    {        
        // Hide/Show mouse cursor
        if (Input.GetKeyDown(KeyCode.H))
        {
            Hide_ShowMouseCursor();
        }
    }

    // Toggle mouse cursor lock mode
    public void Hide_ShowMouseCursor()
    {
        if (!_cursorLocked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            _cursorLocked = true;
        }
        else 
        {
            Cursor.lockState = CursorLockMode.None;
            _cursorLocked = false;
        }
    }    
}