using UnityEngine;
using System.Collections.Generic;

public class UIStateManager : MonoBehaviour
{
    // Singleton instance
    public static UIStateManager Instance { get; private set; }
    
    // Tracks which UI panels are currently open
    private HashSet<string> openUIPanels = new HashSet<string>();
    
    // Public property to check if any UI is open
    public bool IsAnyUIOpen => openUIPanels.Count > 0;
    
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
        }
    }
    
    // Register a UI panel as open
    public void RegisterOpenUI(string panelID)
    {
        openUIPanels.Add(panelID);
        Debug.Log($"UI panel '{panelID}' opened. {openUIPanels.Count} panels open.");
    }
    
    // Register a UI panel as closed
    public void RegisterClosedUI(string panelID)
    {
        openUIPanels.Remove(panelID);
        Debug.Log($"UI panel '{panelID}' closed. {openUIPanels.Count} panels open.");
    }
    
    // For debugging - list all open panels
    public List<string> GetOpenPanels()
    {
        return new List<string>(openUIPanels);
    }
}