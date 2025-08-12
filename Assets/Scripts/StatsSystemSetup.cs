using UnityEngine;

/// <summary>
/// Helper script to ensure StatsSystem is properly set up in the scene.
/// Attach this to any GameObject in your GameLevel scene.
/// </summary>
public class StatsSystemSetup : MonoBehaviour
{
    [Header("Auto-Setup")]
    [SerializeField] private bool autoCreateStatsSystem = true;
    [SerializeField] private bool showSetupDebug = true;
    
    private void Awake()
    {
        if (autoCreateStatsSystem)
        {
            EnsureStatsSystemExists();
        }
    }
    
    private void EnsureStatsSystemExists()
    {
        // Check if StatsSystem already exists
        if (StatsSystem.Instance != null)
        {
            if (showSetupDebug)
                Debug.Log("StatsSystem already exists - no setup needed");
            return;
        }
        
        // Look for existing StatsSystem in scene
        StatsSystem existingStats = FindObjectOfType<StatsSystem>();
        if (existingStats != null)
        {
            if (showSetupDebug)
                Debug.Log("Found existing StatsSystem in scene");
            return;
        }
        
        // Create new StatsSystem
        GameObject statsGO = new GameObject("StatsSystem");
        StatsSystem statsSystem = statsGO.AddComponent<StatsSystem>();
        
        // Optional: Make it persistent across scenes (though it should handle this itself)
        DontDestroyOnLoad(statsGO);
        
        if (showSetupDebug)
        {
            Debug.Log("Created new StatsSystem instance");
            Debug.Log($"Base Power: {statsSystem.GetCurrentPowerMW()} MW");
        }
    }
    
    // Context menu for manual setup
    [ContextMenu("Force Create StatsSystem")]
    public void ForceCreateStatsSystem()
    {
        // Destroy existing if any
        if (StatsSystem.Instance != null)
        {
            DestroyImmediate(StatsSystem.Instance.gameObject);
        }
        
        // Create fresh instance
        GameObject statsGO = new GameObject("StatsSystem");
        statsGO.AddComponent<StatsSystem>();
        
        Debug.Log("Force-created new StatsSystem instance");
    }
    
    [ContextMenu("Test Stats Integration")]
    public void TestStatsIntegration()
    {
        if (StatsSystem.Instance == null)
        {
            Debug.LogError("StatsSystem not found! Run 'Force Create StatsSystem' first.");
            return;
        }
        
        Debug.Log("=== TESTING STATS INTEGRATION ===");
        
        // Test electricity connection
        Debug.Log("Testing electricity connection...");
        StatsSystem.Instance.OnElectricityConnected();
        
        // Wait a frame then show stats
        StartCoroutine(ShowStatsAfterDelay(0.1f));
    }
    
    private System.Collections.IEnumerator ShowStatsAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (StatsSystem.Instance != null)
        {
            StatsSystem.Instance.ShowCurrentStats();
        }
    }
}