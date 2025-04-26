using UnityEngine;

public class GameNarratorSync : MonoBehaviour
{
    [SerializeField] private GameHUDManager gameHUDManager;
    [SerializeField] private GameNarratorController narratorController;
    
    [Header("Debug Options")]
    [SerializeField] private bool debugMode = false;
    
    private float lastReportedTime = 0f;
    private bool isInitialized = false;
    
    private void Start()
    {
        // Find references if not set
        if (gameHUDManager == null)
            gameHUDManager = FindObjectOfType<GameHUDManager>();
            
        if (narratorController == null)
            narratorController = FindObjectOfType<GameNarratorController>();
            
        if (gameHUDManager != null && narratorController != null)
        {
            isInitialized = true;
            
            // Make sure narrator controller is active
            narratorController.StartTimer();
            
            if (debugMode)
                Debug.Log("GameNarratorSync initialized successfully");
        }
        else
        {
            Debug.LogError("GameNarratorSync: Could not find GameHUDManager or GameNarratorController!");
        }
    }
    
    private void Update()
    {
        if (!isInitialized)
            return;
            
        // For debug mode, log the time every 15 seconds
        if (debugMode)
        {
            float currentTime = narratorController.GetGameTime();
            if (currentTime - lastReportedTime > 15f)
            {
                Debug.Log($"Game time: {currentTime:F1} seconds");
                lastReportedTime = currentTime;
            }
        }
    }
    
    // This method can be called when the game state changes (e.g., when timer is paused)
    public void UpdateNarratorState(bool isRunning)
    {
        if (narratorController != null)
        {
            if (isRunning)
                narratorController.StartTimer();
            else
                narratorController.StopTimer();
        }
    }
}