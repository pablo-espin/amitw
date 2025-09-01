using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class UIStateManager : MonoBehaviour
{
    // Singleton instance
    public static UIStateManager Instance { get; private set; }
    
    // Tracks which UI panels are currently open
    private HashSet<string> openUIPanels = new HashSet<string>();
    
    // Public property to check if any UI is open
    public bool IsAnyUIOpen => openUIPanels.Count > 0;
    
    // References to UI systems for closing
    private ManualSystem manualSystem;
    private GameHUDManager gameHUDManager;
    private PauseMenuManager pauseMenuManager;
    private FalseClueSystem falseClueSystem;
    private LocationClueSystem locationClueSystem;
    
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
    
    private void Start()
    {
        // Find references to UI systems
        manualSystem = FindObjectOfType<ManualSystem>();
        gameHUDManager = FindObjectOfType<GameHUDManager>();
        pauseMenuManager = FindObjectOfType<PauseMenuManager>();
        falseClueSystem = FindObjectOfType<FalseClueSystem>();
        locationClueSystem = FindObjectOfType<LocationClueSystem>();
        
        // Log which systems were found for debugging
        Debug.Log($"UIStateManager found systems: " +
                  $"Manual={manualSystem != null}, " +
                  $"GameHUD={gameHUDManager != null}, " +
                  $"Pause={pauseMenuManager != null}, " +
                  $"FalseClue={falseClueSystem != null}, " +
                  $"Location={locationClueSystem != null}");
    }
    
    private void Update()
    {
        // Check for escape key press to close all open UIs
        if (Input.GetKeyDown(KeyCode.Escape) && IsAnyUIOpen)
        {
            Debug.Log($"Escape key pressed - closing {openUIPanels.Count} open UI(s)");
            CloseAllOpenUIs();
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
    
    // Close all currently open UIs
    public void CloseAllOpenUIs()
    {
        if (openUIPanels.Count == 0)
        {
            Debug.Log("No UIs to close");
            return;
        }
        
        // Create a copy of the open panels to avoid modification during iteration
        var panelsToClose = openUIPanels.ToList();
        
        Debug.Log($"Closing {panelsToClose.Count} UI panels: {string.Join(", ", panelsToClose)}");
        
        foreach (string panelID in panelsToClose)
        {
            CloseUIByID(panelID);
        }
    }
    
    // Close a specific UI by its panel ID
    private void CloseUIByID(string panelID)
    {
        Debug.Log($"Attempting to close UI: {panelID}");
        
        switch (panelID)
        {
            case "Manual":
                if (manualSystem != null)
                {
                    manualSystem.CloseManual();
                    Debug.Log("Closed Manual via ManualSystem");
                }
                else
                {
                    Debug.LogWarning("ManualSystem reference is null");
                }
                break;
                
            case "DecryptionPanel":
                if (gameHUDManager != null)
                {
                    gameHUDManager.CloseDecryptionPanel();
                    Debug.Log("Closed DecryptionPanel via GameHUDManager");
                }
                else
                {
                    Debug.LogWarning("GameHUDManager reference is null");
                }
                break;
                
            case "PauseMenu":
                if (pauseMenuManager != null)
                {
                    pauseMenuManager.ResumeGame();
                    Debug.Log("Closed PauseMenu via PauseMenuManager");
                }
                else
                {
                    Debug.LogWarning("PauseMenuManager reference is null");
                }
                break;
                
            case "ComputerScreen":
                if (falseClueSystem != null)
                {
                    falseClueSystem.CloseComputer();
                    Debug.Log("Closed ComputerScreen via FalseClueSystem");
                }
                else
                {
                    Debug.LogWarning("FalseClueSystem reference is null");
                }
                break;
                
            case "ComputerCodeChoice":
                if (gameHUDManager != null)
                {
                    gameHUDManager.OnGoBackClicked();
                    Debug.Log("Closed ComputerCodeChoice via GameHUDManager");
                }
                else
                {
                    Debug.LogWarning("GameHUDManager reference is null");
                }
                break;
                
            case "LocationDocument":
                if (locationClueSystem != null)
                {
                    locationClueSystem.CloseDocumentView();
                    Debug.Log("Closed LocationDocument via LocationClueSystem");
                }
                else
                {
                    Debug.LogWarning("LocationClueSystem reference is null");
                }
                break;
                
            default:
                Debug.LogWarning($"Unknown UI panel ID: {panelID}. Cannot close automatically.");
                // Remove from tracking since we don't know how to close it
                openUIPanels.Remove(panelID);
                break;
        }
    }
    
    // For debugging - list all open panels
    public List<string> GetOpenPanels()
    {
        return new List<string>(openUIPanels);
    }
    
    // Manual method to refresh system references (useful after scene changes)
    public void RefreshSystemReferences()
    {
        manualSystem = FindObjectOfType<ManualSystem>();
        gameHUDManager = FindObjectOfType<GameHUDManager>();
        pauseMenuManager = FindObjectOfType<PauseMenuManager>();
        falseClueSystem = FindObjectOfType<FalseClueSystem>();
        locationClueSystem = FindObjectOfType<LocationClueSystem>();
        
        Debug.Log("UIStateManager system references refreshed");
    }
}