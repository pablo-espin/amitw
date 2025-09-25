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
    
    [Header("Visual Settings")]
    [SerializeField] private Color healthyColor = new Color(0.2f, 0.8f, 0.2f, 1f); // Green
    [SerializeField] private Color warningColor = new Color(1f, 1f, 0.2f, 1f); // Yellow  
    [SerializeField] private Color criticalColor = new Color(1f, 0.3f, 0.2f, 1f); // Red
    [SerializeField] private float colorTransitionSpeed = 2f;
    [SerializeField] private float criticalHealthThreshold = 25f; // Show warning below 25%
    
    [Header("Warning Text Settings")]
    [SerializeField] private float warningTextFadeSpeed = 2f;
    [SerializeField] private float warningTextPulseSpeed = 1f;
    [SerializeField] private Color warningTextColor = Color.red;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    // Current visual state
    private bool criticalWarningShown = false;
    private Coroutine warningTextCoroutine;
    
    private void Start()
    {
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
        
        // Subscribe to stats updates from StatsSystem
        if (StatsSystem.Instance != null)
        {
            StatsSystem.Instance.OnMemoryHealthUpdated += UpdateHealthBar;
            if (showDebugInfo)
            {
                Debug.Log("MemoryHealthBar subscribed to StatsSystem memory health updates");
            }
        }
        else
        {
            Debug.LogWarning("StatsSystem.Instance not found! MemoryHealthBar cannot subscribe to updates.");
        }
        
        // Set initial color
        UpdateHealthBarColor(100f);
        
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
            StatsSystem.Instance.OnMemoryHealthUpdated -= UpdateHealthBar;
        }
    }
    
    // Called by StatsSystem when memory health updates
    private void UpdateHealthBar(float currentHealth)
    {
        // Update visual elements
        UpdateHealthSlider(currentHealth);
        UpdateHealthBarColor(currentHealth);
        UpdateCriticalWarning(currentHealth);
        
        // if (showDebugInfo && Time.frameCount % 300 == 0) // Log every 5 seconds
        // {
        //     Debug.Log($"MemoryHealthBar updated - Health: {currentHealth:F1}%");
        // }
    }
    
    private void UpdateHealthSlider(float currentHealth)
    {
        if (healthSlider != null)
        {
            float targetValue = currentHealth / 100f; // Convert to 0-1 range
            healthSlider.value = Mathf.Lerp(healthSlider.value, targetValue, Time.deltaTime * 5f); // Smooth transition
        }
    }
    
    private void UpdateHealthBarColor(float currentHealth)
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
    
    private void UpdateCriticalWarning(float currentHealth)
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
        
        // Update warning text content if showing
        if (criticalWarningShown && criticalWarningText != null)
        {
            criticalWarningText.text = $"CRITICAL: MEMORY INTEGRITY {currentHealth:F0}%";
        }
    }
    
    private void ShowCriticalWarning()
    {
        if (criticalWarningText == null) return;
        
        criticalWarningShown = true;
        criticalWarningText.gameObject.SetActive(true);
        
        // Stop any existing coroutine
        if (warningTextCoroutine != null)
        {
            StopCoroutine(warningTextCoroutine);
        }
        
        // Start pulsing animation
        warningTextCoroutine = StartCoroutine(PulseWarningText());
        
        if (showDebugInfo)
        {
            Debug.Log("Critical memory warning shown");
        }
    }
    
    private void HideCriticalWarning()
    {
        if (criticalWarningText == null) return;
        
        criticalWarningShown = false;
        
        // Stop pulsing coroutine
        if (warningTextCoroutine != null)
        {
            StopCoroutine(warningTextCoroutine);
            warningTextCoroutine = null;
        }
        
        // Fade out the warning
        StartCoroutine(FadeOutWarningText());
        
        if (showDebugInfo)
        {
            Debug.Log("Critical memory warning hidden");
        }
    }
    
    private IEnumerator PulseWarningText()
    {
        while (criticalWarningShown && criticalWarningText != null)
        {
            // Pulse the alpha value
            float alpha = (Mathf.Sin(Time.time * warningTextPulseSpeed) + 1f) * 0.5f;
            alpha = Mathf.Lerp(0.5f, 1f, alpha); // Keep it visible, just pulse between 50% and 100%
            
            Color color = warningTextColor;
            color.a = alpha;
            criticalWarningText.color = color;
            
            yield return null;
        }
    }
    
    private IEnumerator FadeOutWarningText()
    {
        if (criticalWarningText == null) yield break;
        
        float startAlpha = criticalWarningText.color.a;
        float fadeTime = 1f / warningTextFadeSpeed;
        float elapsed = 0f;
        
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
    
    // Public getters for external systems (gets values from StatsSystem)
    public float GetCurrentHealth()
    {
        return StatsSystem.Instance != null ? StatsSystem.Instance.GetCurrentMemoryHealth() : 100f;
    }
    
    public bool IsHealthCritical()
    {
        return GetCurrentHealth() <= criticalHealthThreshold;
    }
    
    public float GetCriticalThreshold()
    {
        return criticalHealthThreshold;
    }
    
    // Debug methods
    [ContextMenu("Test Critical Health Display")]
    public void TestCriticalHealth()
    {
        UpdateHealthBar(20f);
    }
    
    [ContextMenu("Test Zero Health Display")]
    public void TestZeroHealth()
    {
        UpdateHealthBar(0f);
    }
    
    [ContextMenu("Reset Health Display")]
    public void ResetHealthDisplay()
    {
        UpdateHealthBar(100f);
    }
    
    [ContextMenu("Force Hide Warning")]
    public void ForceHideWarning()
    {
        if (criticalWarningShown)
        {
            HideCriticalWarning();
        }
    }
}