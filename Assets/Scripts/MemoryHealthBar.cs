using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MemoryHealthBar : MonoBehaviour
{
    [Header("Health Bar Components")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image healthFill;
    [SerializeField] private TextMeshProUGUI criticalWarningText;
    
    [Header("Health Degradation Settings")]
    [SerializeField] private float degradationStartTime = 660f; // 11 minutes in seconds
    [SerializeField] private float basePowerMW = 700f; // Should match StatsSystem
    [SerializeField] private float degradationMultiplier = 0.1f; // How fast memories degrade
    [SerializeField] private float criticalHealthThreshold = 25f; // Show warning below 25%
    
    [Header("Visual Settings")]
    [SerializeField] private Color healthyColor = new Color(0.2f, 0.8f, 0.2f, 1f); // Green
    [SerializeField] private Color warningColor = new Color(1f, 1f, 0.2f, 1f); // Yellow  
    [SerializeField] private Color criticalColor = new Color(1f, 0.3f, 0.2f, 1f); // Red
    [SerializeField] private float colorTransitionSpeed = 2f;
    
    [Header("Warning Text Settings")]
    [SerializeField] private float warningTextFadeSpeed = 2f;
    [SerializeField] private float warningTextPulseSpeed = 1f;
    [SerializeField] private Color warningTextColor = Color.red;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    // Current state
    private float currentHealth = 100f; // Start at 100%
    private float gameStartTime;
    private bool degradationStarted = false;
    private bool criticalWarningShown = false;
    private Coroutine warningTextCoroutine;
    
    // Events
    public System.Action OnMemoriesFullyDeleted; // Triggers trapped ending
    
    private void Start()
    {
        gameStartTime = Time.time;
        
        // Initialize health bar
        if (healthSlider != null)
        {
            healthSlider.value = 1f; // 100%
        }
        
        // Hide warning text initially
        if (criticalWarningText != null)
        {
            criticalWarningText.gameObject.SetActive(false);
        }
        
        // Subscribe to stats updates
        if (StatsSystem.Instance != null)
        {
            StatsSystem.Instance.OnStatsUpdated += UpdateHealthBar;
        }
        
        // Set initial color
        UpdateHealthBarColor();
        
        if (showDebugInfo)
        {
            Debug.Log("MemoryHealthBar initialized - memories at 100%");
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (StatsSystem.Instance != null)
        {
            StatsSystem.Instance.OnStatsUpdated -= UpdateHealthBar;
        }
    }
    
    // Called by StatsSystem when stats update
    private void UpdateHealthBar(float powerMW, float waterLiterPerSecond, float co2KgPerSecond, float totalCO2Kg)
    {
        float currentGameTime = Time.time - gameStartTime;
        
        // Check if degradation should start
        if (!degradationStarted && currentGameTime >= degradationStartTime)
        {
            degradationStarted = true;
            if (showDebugInfo)
            {
                Debug.Log("Memory degradation started at 11 minutes");
            }
        }
        
        // Calculate health degradation if started
        if (degradationStarted)
        {
            CalculateHealthDegradation(powerMW);
        }
        
        // Update visual elements
        UpdateHealthSlider();
        UpdateHealthBarColor();
        UpdateCriticalWarning();
        
        // Check for game ending condition
        if (currentHealth <= 0f)
        {
            TriggerMemoriesFullyDeleted();
        }
    }
    
    private void CalculateHealthDegradation(float currentPowerMW)
    {
        // Calculate degradation rate based on power above base level
        float powerRatio = currentPowerMW / basePowerMW; // How many times base power
        float excessPowerRatio = Mathf.Max(0f, powerRatio - 1f); // Only excess power causes degradation
        
        // Degradation rate increases with excess power
        float degradationRate = excessPowerRatio * degradationMultiplier;
        
        // Apply degradation over time
        float deltaTime = Time.deltaTime;
        float healthLoss = degradationRate * deltaTime;
        
        if (healthLoss > 0f)
        {
            currentHealth = Mathf.Max(0f, currentHealth - healthLoss);
            
            if (showDebugInfo && Time.frameCount % 60 == 0) // Log every 60 frames
            {
                Debug.Log($"Memory Health: {currentHealth:F1}% (Power: {currentPowerMW:F0} MW, Rate: {degradationRate:F3}/s)");
            }
        }
    }
    
    private void UpdateHealthSlider()
    {
        if (healthSlider != null)
        {
            float targetValue = currentHealth / 100f; // Convert to 0-1 range
            healthSlider.value = Mathf.Lerp(healthSlider.value, targetValue, Time.deltaTime * 5f); // Smooth transition
        }
    }
    
    private void UpdateHealthBarColor()
    {
        if (healthFill == null) return;
        
        Color targetColor;
        
        if (currentHealth > 50f)
        {
            // Healthy - Green to Yellow transition
            float t = (100f - currentHealth) / 50f; // 0 to 1 as health goes from 100% to 50%
            targetColor = Color.Lerp(healthyColor, warningColor, t);
        }
        else
        {
            // Critical - Yellow to Red transition  
            float t = (50f - currentHealth) / 50f; // 0 to 1 as health goes from 50% to 0%
            targetColor = Color.Lerp(warningColor, criticalColor, t);
        }
        
        // Smooth color transition
        healthFill.color = Color.Lerp(healthFill.color, targetColor, Time.deltaTime * colorTransitionSpeed);
    }
    
    private void UpdateCriticalWarning()
    {
        bool shouldShowWarning = currentHealth <= criticalHealthThreshold && currentHealth > 0f;
        
        if (shouldShowWarning && !criticalWarningShown)
        {
            ShowCriticalWarning();
        }
        else if (!shouldShowWarning && criticalWarningShown)
        {
            HideCriticalWarning();
        }
        
        // Update warning text content
        if (criticalWarningShown && criticalWarningText != null)
        {
            float deletedPercentage = 100f - currentHealth;
            criticalWarningText.text = $"{deletedPercentage:F0}% of memories have been deleted";
        }
    }
    
    private void ShowCriticalWarning()
    {
        if (criticalWarningText == null) return;
        
        criticalWarningShown = true;
        criticalWarningText.gameObject.SetActive(true);
        criticalWarningText.color = warningTextColor;
        
        // Start pulsing animation
        if (warningTextCoroutine != null)
        {
            StopCoroutine(warningTextCoroutine);
        }
        warningTextCoroutine = StartCoroutine(PulseWarningText());
        
        if (showDebugInfo)
        {
            Debug.Log($"Critical warning shown - {100f - currentHealth:F0}% of memories deleted");
        }
    }
    
    private void HideCriticalWarning()
    {
        if (criticalWarningText == null) return;
        
        criticalWarningShown = false;
        
        // Stop pulsing animation
        if (warningTextCoroutine != null)
        {
            StopCoroutine(warningTextCoroutine);
            warningTextCoroutine = null;
        }
        
        // Fade out and hide
        StartCoroutine(FadeOutWarningText());
    }
    
    private IEnumerator PulseWarningText()
    {
        while (criticalWarningShown)
        {
            // Pulse alpha between 0.7 and 1.0
            float time = Time.time * warningTextPulseSpeed;
            float alpha = Mathf.Lerp(0.7f, 1f, (Mathf.Sin(time) + 1f) * 0.5f);
            
            Color color = warningTextColor;
            color.a = alpha;
            criticalWarningText.color = color;
            
            yield return null;
        }
    }
    
    private IEnumerator FadeOutWarningText()
    {
        float startAlpha = criticalWarningText.color.a;
        float elapsed = 0f;
        float fadeTime = 1f / warningTextFadeSpeed;
        
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeTime);
            
            Color color = warningTextColor;
            color.a = alpha;
            criticalWarningText.color = color;
            
            yield return null;
        }
        
        criticalWarningText.gameObject.SetActive(false);
    }
    
    private void TriggerMemoriesFullyDeleted()
    {
        if (currentHealth > 0f) return; // Already triggered
        
        currentHealth = 0f; // Ensure it's exactly 0
        
        if (showDebugInfo)
        {
            Debug.Log("All memories deleted - triggering trapped ending");
        }
        
        // Hide warning text since game is ending
        if (criticalWarningText != null)
        {
            criticalWarningText.gameObject.SetActive(false);
        }
        
        // Trigger event for game manager
        OnMemoriesFullyDeleted?.Invoke();
    }
    
    // Public getters
    public float GetCurrentHealth() => currentHealth;
    public bool IsHealthCritical() => currentHealth <= criticalHealthThreshold;
    public bool AreDegradationStarted() => degradationStarted;
    
    // Debug methods
    [ContextMenu("Test Critical Health")]
    public void TestCriticalHealth()
    {
        currentHealth = 20f;
        UpdateHealthSlider();
        UpdateHealthBarColor();
        UpdateCriticalWarning();
    }
    
    [ContextMenu("Test Zero Health")]
    public void TestZeroHealth()
    {
        currentHealth = 0f;
        TriggerMemoriesFullyDeleted();
    }
    
    [ContextMenu("Reset Health")]
    public void ResetHealth()
    {
        currentHealth = 100f;
        degradationStarted = false;
        criticalWarningShown = false;
        UpdateHealthSlider();
        UpdateHealthBarColor();
        
        if (criticalWarningText != null)
        {
            criticalWarningText.gameObject.SetActive(false);
        }
    }
    
    [ContextMenu("Force Start Degradation")]
    public void ForceStartDegradation()
    {
        degradationStarted = true;
        Debug.Log("Forced memory degradation start");
    }
}