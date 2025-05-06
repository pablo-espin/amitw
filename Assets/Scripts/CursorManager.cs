using UnityEngine;
using System.Collections.Generic;

public class CursorManager : MonoBehaviour
{
    // Singleton instance
    public static CursorManager Instance { get; private set; }
    
    // Track which systems have requested cursor unlock
    private HashSet<string> unlockRequesters = new HashSet<string>();
    
    // Default cursor state
    [SerializeField] private bool defaultLocked = true;
    
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

        // Initially unlock cursor for home screen
        RequestCursorUnlock("HomeScreen");

        // Initial cursor state
        UpdateCursorState();
    }
    
    public void RequestCursorLock(string requesterID)
    {
        if (unlockRequesters.Contains(requesterID))
        {
            unlockRequesters.Remove(requesterID);
            UpdateCursorState();
        }
    }
    
    public void RequestCursorUnlock(string requesterID)
    {
        if (!unlockRequesters.Contains(requesterID))
        {
            unlockRequesters.Add(requesterID);
            UpdateCursorState();
        }
    }
    
    private void UpdateCursorState()
    {
        // If any system wants cursor unlocked, unlock it
        if (unlockRequesters.Count > 0)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Debug.Log("Cursor unlocked, requesters: " + string.Join(", ", unlockRequesters));
        }
        else
        {
            Cursor.lockState = defaultLocked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !defaultLocked;
            Debug.Log("Cursor locked");
        }
    }
    
    // Force cursor to locked state (e.g., at game start)
    public void ForceLockCursor()
    {
        unlockRequesters.Clear();
        UpdateCursorState();
    }
    
    // For debugging - show which systems have requested unlock
    public string[] GetCurrentRequesters()
    {
        string[] requesters = new string[unlockRequesters.Count];
        unlockRequesters.CopyTo(requesters);
        return requesters;
    }
}