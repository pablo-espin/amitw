using UnityEngine;
using System.Collections.Generic;

public class StatsSystem : MonoBehaviour
{
    [Header("Base Rates (Configurable)")]
    [SerializeField] private float basePowerMW = 700f; // Base power in megawatts
    [SerializeField] private float baseWaterLiterPerSecond = 200f; // Base water consumption
    [SerializeField] private float baseCO2KgPerSecond = 0.545f; // Base CO2 emissions

    [Header("Power Level Timing (Real-world minutes)")]
    [SerializeField] private float level1StartTime = 360f; // 6 minutes in seconds
    [SerializeField] private float level2StartTime = 660f; // 11 minutes in seconds
    [SerializeField] private float level3StartTime = 840f; // 14 minutes in seconds

    [Header("Power Level Multipliers")]
    [SerializeField] private float level1MaxMultiplier = 1.5f; // Level 1 reaches 1.5x base
    [SerializeField] private float level2Multiplier = 1.8f; // Level 2 instant jump to 1.8x
    [SerializeField] private float level3Multiplier = 2.0f; // Level 3 instant jump to 2x
    [SerializeField] private float level4MaxMultiplier = 3.0f; // Level 4 reaches 3x base

    [Header("Player Action Multipliers")]
    [SerializeField] private float electricityConnectionBonus = 0.1f; // 10% increase
    [SerializeField] private float waterTapBonusLiterPerSecond = 5f; // Extra water when tap runs
    [SerializeField] private float captchaMultiplier = 4.0f; // 4x base power
    [SerializeField] private float memoryReleaseMultiplier = 10.0f; // 10x all stats

    [Header("Memory Health Settings")]
    [SerializeField] private float memoryDegradationStartTime = 660f; // 11 minutes in seconds
    [SerializeField] private float memoryDegradationMultiplier = 0.1f; // How fast memories degrade

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    // Current state
    private float gameStartTime;
    private float currentPowerMW;
    private float currentWaterLiterPerSecond;
    private float currentCO2KgPerSecond;

    // Player action modifiers
    private bool electricityConnected = false;
    private bool waterTapRunning = false;
    private bool captchaSolved = false;
    private bool memoryReleased = false;

    // Accumulated totals (for integration)
    private float totalEnergyMWh = 0f;
    private float totalWaterLiters = 0f;
    private float totalCO2Kg = 0f;

    // Memory health tracking
    private float currentMemoryHealth = 100f; // Start at 100%
    private bool memoryDegradationStarted = false;

    // Integration tracking
    private float lastUpdateTime;
    private List<float> powerHistory = new List<float>(); // For more accurate integration
    private List<float> timeHistory = new List<float>();

    // Game stats tracking
    private bool isGameActive = true; // Track if game is still running
    private bool statsStoppedForEnding = false; // Flag to prevent multiple stops

    // Singleton
    public static StatsSystem Instance { get; private set; }

    // Events for HUD updates
    public System.Action<float, float, float, float> OnStatsUpdated; // power, water rate, CO2 rate, total CO2
    public System.Action<float> OnMemoryHealthUpdated; // memory health percentage

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
        gameStartTime = Time.time;
        lastUpdateTime = gameStartTime;

        // Calculate initial values
        UpdateCurrentStats();

        if (showDebugInfo)
        {
            Debug.Log($"StatsSystem initialized. Base power: {basePowerMW} MW, Base water: {baseWaterLiterPerSecond} L/s, Base CO2: {baseCO2KgPerSecond} kg/s");
        }
    }

    private void Update()
    {
        // Only update stats if game is still active
        if (!isGameActive) return;

        UpdateCurrentStats();
        IntegrateStats();
        UpdateMemoryHealth();
    }

    // Stop stats tracking when game ends
    public void StopStatsTracking()
    {
        if (statsStoppedForEnding) return; // Prevent multiple calls

        isGameActive = false;
        statsStoppedForEnding = true;

        if (showDebugInfo)
        {
            Debug.Log("StatsSystem: Stats tracking stopped for game ending");
            Debug.Log($"Final Stats - Energy: {totalEnergyMWh:F2} MWh, Water: {totalWaterLiters:F0} L, CO2: {totalCO2Kg:F1} kg");
        }
    }

    // Resume stats tracking (if needed for new game)
    public void ResumeStatsTracking()
    {
        isGameActive = true;
        statsStoppedForEnding = false;

        if (showDebugInfo)
        {
            Debug.Log("StatsSystem: Stats tracking resumed");
        }
    }

    // Reset all stats for new game
    public void ResetStats()
    {
        // Reset accumulated totals
        totalEnergyMWh = 0f;
        totalWaterLiters = 0f;
        totalCO2Kg = 0f;

        // Reset player action modifiers
        electricityConnected = false;
        waterTapRunning = false;
        captchaSolved = false;
        memoryReleased = false;

        // Reset memory health
        currentMemoryHealth = 100f;
        memoryDegradationStarted = false;

        // Reset game state
        isGameActive = true;
        statsStoppedForEnding = false;

        // Reset timing
        gameStartTime = Time.time;
        lastUpdateTime = gameStartTime;

        // Clear history
        powerHistory.Clear();
        timeHistory.Clear();

        // Recalculate initial values
        UpdateCurrentStats();

        if (showDebugInfo)
        {
            Debug.Log("StatsSystem: All stats reset for new game");
        }
    }

    private void UpdateMemoryHealth()
    {
        if (!isGameActive) return; // Don't update if game is not active

        float currentGameTime = Time.time - gameStartTime;

        // Check if memory degradation should start
        if (!memoryDegradationStarted && currentGameTime >= memoryDegradationStartTime)
        {
            memoryDegradationStarted = true;
            if (showDebugInfo)
            {
                Debug.Log("Memory degradation started at 11 minutes");
            }
        }

        // Calculate memory health degradation if started
        if (memoryDegradationStarted && currentMemoryHealth > 0f)
        {
            // Calculate degradation rate based on power above base level
            float powerRatio = currentPowerMW / basePowerMW;
            float excessPowerRatio = Mathf.Max(0f, powerRatio - 1f); // Only excess power causes degradation

            // Degradation rate increases with excess power
            float degradationRate = excessPowerRatio * memoryDegradationMultiplier;

            // Apply degradation over time
            float deltaTime = Time.deltaTime;
            float healthLoss = degradationRate * deltaTime;

            if (healthLoss > 0f)
            {
                currentMemoryHealth = Mathf.Max(0f, currentMemoryHealth - healthLoss);

                if (showDebugInfo && Time.frameCount % 300 == 0) // Log every 5 seconds
                {
                    Debug.Log($"Memory Health: {currentMemoryHealth:F1}% (degradation rate: {degradationRate:F3}/s)");
                }

                // Notify listeners
                OnMemoryHealthUpdated?.Invoke(currentMemoryHealth);
            }
        }
    }

    private void UpdateCurrentStats()
    {
        if (!isGameActive) return; // NEW: Don't update if game is not active
        
        float currentGameTime = Time.time - gameStartTime;

        // Calculate base power multiplier based on time-based levels
        float basePowerMultiplier = CalculateBasePowerMultiplier(currentGameTime);

        // Start with base power after time-based multiplier
        currentPowerMW = basePowerMW * basePowerMultiplier;

        // Apply player action modifiers
        if (electricityConnected)
        {
            currentPowerMW *= (1f + electricityConnectionBonus);
        }

        if (captchaSolved)
        {
            currentPowerMW = basePowerMW * captchaMultiplier; // Override with 4x base power
        }

        // Calculate water consumption
        float waterMultiplier = currentPowerMW / basePowerMW; // Water scales with power
        currentWaterLiterPerSecond = baseWaterLiterPerSecond * waterMultiplier;

        // Add tap bonus if water is running
        if (waterTapRunning)
        {
            currentWaterLiterPerSecond += waterTapBonusLiterPerSecond;
        }

        // Calculate CO2 emissions (scales with power)
        float co2Multiplier = currentPowerMW / basePowerMW; // CO2 scales with power
        currentCO2KgPerSecond = baseCO2KgPerSecond * co2Multiplier;

        // Notify HUD of updates (current rates + total CO2)
        OnStatsUpdated?.Invoke(currentPowerMW, currentWaterLiterPerSecond, currentCO2KgPerSecond, totalCO2Kg);

        // Notify of memory health updates
        OnMemoryHealthUpdated?.Invoke(currentMemoryHealth);
    }

    private float CalculateBasePowerMultiplier(float gameTime)
    {
        // Check if we're in final lockdown phase
        LockdownManager lockdownManager = LockdownManager.Instance;
        bool inFinalPhase = lockdownManager != null &&
                           lockdownManager.GetCurrentPhase() == LockdownManager.LockdownPhase.FinalLockdown;

        if (inFinalPhase)
        {
            // Level 4: Linear increase from level3Multiplier to level4MaxMultiplier
            float finalPhaseStartTime = lockdownManager.GetLockdownTime() + 60f; // After escape window
            float timeSinceFinalStart = gameTime - finalPhaseStartTime;
            float finalPhaseDuration = 300f; // 5 minutes

            if (timeSinceFinalStart >= 0)
            {
                float progress = Mathf.Clamp01(timeSinceFinalStart / finalPhaseDuration);
                return Mathf.Lerp(level3Multiplier, level4MaxMultiplier, progress);
            }
        }

        // Time-based levels (before final phase)
        if (gameTime < level1StartTime)
        {
            // Level 0: Base power
            return 1.0f;
        }
        else if (gameTime < level2StartTime)
        {
            // Level 1: Linear increase from 1.0x to level1MaxMultiplier
            float progress = (gameTime - level1StartTime) / (level2StartTime - level1StartTime);
            return Mathf.Lerp(1.0f, level1MaxMultiplier, progress);
        }
        else if (gameTime < level3StartTime)
        {
            // Level 2: Instant jump to level2Multiplier
            return level2Multiplier;
        }
        else
        {
            // Level 3: Instant jump to level3Multiplier
            return level3Multiplier;
        }
    }

    private void IntegrateStats()
    {
        if (!isGameActive) return; // NEW: Don't integrate if game is not active
        
        float currentTime = Time.time;
        float deltaTime = currentTime - lastUpdateTime;
        
        if (deltaTime > 0)
        {
            // Numerical integration using trapezoidal rule for better accuracy
            totalEnergyMWh += currentPowerMW * (deltaTime / 3600f); // Convert seconds to hours
            totalWaterLiters += currentWaterLiterPerSecond * deltaTime;
            totalCO2Kg += currentCO2KgPerSecond * deltaTime;
            
            // Store history for debugging/analysis
            powerHistory.Add(currentPowerMW);
            timeHistory.Add(currentTime);
            
            // Keep history manageable (last 100 entries)
            if (powerHistory.Count > 100)
            {
                powerHistory.RemoveAt(0);
                timeHistory.RemoveAt(0);
            }
            
            lastUpdateTime = currentTime;
        }
    }

    // Player action methods
    public void OnElectricityConnected()
    {
        if (!electricityConnected)
        {
            electricityConnected = true;
            if (showDebugInfo)
            {
                Debug.Log($"Electricity connected! Power multiplier increased by {electricityConnectionBonus * 100}%");
            }
        }
    }

    public void OnWaterTapStateChanged(bool isRunning)
    {
        waterTapRunning = isRunning;
        if (showDebugInfo)
        {
            if (isRunning)
            {
                Debug.Log($"StatsSystem: Water tap started running! Extra {waterTapBonusLiterPerSecond} L/s consumption");
            }
            else
            {
                Debug.Log("StatsSystem: Water tap stopped running");
            }
        }
    }

    public void OnCaptchaSolved()
    {
        if (!captchaSolved)
        {
            captchaSolved = true;
            if (showDebugInfo)
            {
                Debug.Log($"CAPTCHA solved! Power draw increased to {captchaMultiplier}x base power ({basePowerMW * captchaMultiplier} MW)");
            }
        }
    }

    public void OnMemoryReleased()
    {
        if (!memoryReleased)
        {
            memoryReleased = true;
            
            // Multiply all current totals by the memory release multiplier
            totalEnergyMWh *= memoryReleaseMultiplier;
            totalWaterLiters *= memoryReleaseMultiplier;
            totalCO2Kg *= memoryReleaseMultiplier;
            
            if (showDebugInfo)
            {
                Debug.Log($"Memory released! All stats multiplied by {memoryReleaseMultiplier}x");
            }
        }
    }

    // Getters for current stats
    public float GetCurrentPowerMW() => currentPowerMW;
    public float GetCurrentWaterLiterPerSecond() => currentWaterLiterPerSecond;
    public float GetCurrentCO2KgPerSecond() => currentCO2KgPerSecond;
    public float GetCurrentMemoryHealth() => currentMemoryHealth;

    // Getters for total stats
    public float GetTotalEnergyMWh() => totalEnergyMWh;
    public float GetTotalWaterLiters() => totalWaterLiters;
    public float GetTotalCO2Kg() => totalCO2Kg;

    // Get game time for context
    public float GetGameTime() => Time.time - gameStartTime;

    // Get memory health
    public bool IsMemoryDegradationStarted() => memoryDegradationStarted;
    public bool AreMemoriesFullyDeleted() => currentMemoryHealth <= 0f;

    // Check if game is active
    public bool IsGameActive() => isGameActive;

    // For end-game stats formatting
    public string GetFormattedStats(bool isEscapeEnding = false, bool isHeroicEnding = false)
    {
        string contextMessage = "";

        if (isEscapeEnding)
        {
            contextMessage = "WASTE IMPACT - Resources consumed for nothing:\n\n";
        }
        else if (isHeroicEnding)
        {
            contextMessage = "SACRIFICE IMPACT - Resources used to save memories:\n\n";
        }

        return contextMessage +
               $"TOTAL ENERGY CONSUMED: {totalEnergyMWh:F1} MWh\n" +
               $"TOTAL WATER CONSUMED: {totalWaterLiters:F0} Liters\n" +
               $"TOTAL CO2 EMISSIONS: {totalCO2Kg:F1} kg CO2";
    }

    // Debug methods
    [ContextMenu("Show Current Stats")]
    public void ShowCurrentStats()
    {
        Debug.Log($"=== CURRENT STATS ===");
        Debug.Log($"Game Time: {GetGameTime():F1} seconds");
        Debug.Log($"Power: {currentPowerMW:F1} MW");
        Debug.Log($"Water: {currentWaterLiterPerSecond:F1} L/s");
        Debug.Log($"CO2: {currentCO2KgPerSecond:F3} kg/s");
        Debug.Log($"=== TOTALS ===");
        Debug.Log($"Energy: {totalEnergyMWh:F2} MWh");
        Debug.Log($"Water: {totalWaterLiters:F0} L");
        Debug.Log($"CO2: {totalCO2Kg:F1} kg");
    }

    [ContextMenu("Test Electricity Connection")]
    public void TestElectricityConnection()
    {
        OnElectricityConnected();
        ShowCurrentStats();
    }

    [ContextMenu("Test Water Tap On")]
    public void TestWaterTapOn()
    {
        OnWaterTapStateChanged(true);
        ShowCurrentStats();
    }

    [ContextMenu("Test Water Tap Off")]
    public void TestWaterTapOff()
    {
        OnWaterTapStateChanged(false);
        ShowCurrentStats();
    }

    [ContextMenu("Test CAPTCHA Solved")]
    public void TestCaptchaSolved()
    {
        OnCaptchaSolved();
        ShowCurrentStats();
    }

    [ContextMenu("Test Memory Released")]
    public void TestMemoryReleased()
    {
        OnMemoryReleased();
        ShowCurrentStats();
    }
    
    [ContextMenu("Stop Stats Tracking")]
    public void TestStopStatsTracking()
    {
        StopStatsTracking();
        ShowCurrentStats();
    }
    
    [ContextMenu("Reset Stats")]
    public void TestResetStats()
    {
        ResetStats();
        ShowCurrentStats();
    }
}