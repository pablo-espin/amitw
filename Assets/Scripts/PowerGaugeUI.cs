using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PowerGaugeUI : MonoBehaviour
{
    [Header("Gauge Settings")]
    [SerializeField] private float basePowerPercentage = 35f; // Base power shows at 35%
    [SerializeField] private float maxGaugePercentage = 120f; // Gauge can go beyond 100%
    [SerializeField] private float basePowerMW = 700f; // Should match StatsSystem base power
    
    [Header("Gauge Components")]
    [SerializeField] private RectTransform needleTransform;
    [SerializeField] private Image gaugeBackground; // Background with color zones
    [SerializeField] private Image needleImage; // The needle itself for glow effects
    [SerializeField] private Image powerGaugeContainerBackground; // Background that will pulsate in critical zone
    
    [Header("Background Pulsation")]
    [SerializeField] private Color normalBackgroundColor = new Color(0f, 0f, 0f, 0.5f); // Semi-transparent black
    [SerializeField] private Color warningDarkRedColor = new Color(0.3f, 0f, 0f, 0.5f); // Dark red base for warning+
    [SerializeField] private Color warningBrightRedColor = new Color(0.8f, 0f, 0f, 0.5f); // Bright red pulse peak for warning+
    [SerializeField] private float pulsationSpeed = 1.5f; // Pulses per second
    
    [Header("Needle Glow Effects")]
    [SerializeField] private bool useNeedleGlow = true;
    [SerializeField] private Color normalNeedleColor = Color.white;
    [SerializeField] private Color warningNeedleColor = Color.yellow;
    [SerializeField] private Color dangerNeedleColor = Color.red;
    [SerializeField] private Color criticalNeedleColor = new Color(1f, 0.3f, 0.1f, 1f); // Bright red-orange
    
    [Header("Animation Settings")]
    [SerializeField] private float needleAnimationSpeed = 2f;
    [SerializeField] private AnimationCurve needleEasing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Needle Rotation")]
    [SerializeField] private float minNeedleAngle = -90f; // Left side of semicircle
    [SerializeField] private float maxNeedleAngle = 90f; // Right side of semicircle
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    // Current state
    private float currentPowerMW = 0f;
    private float targetNeedleAngle = 0f;
    private float currentNeedleAngle = 0f;
    private Coroutine needleAnimationCoroutine;
    private Coroutine backgroundPulsationCoroutine;
    private bool isInWarningZone = false;
    
    private void Start()
    {
        // Validate background reference
        if (powerGaugeContainerBackground == null)
        {
            Debug.LogWarning("PowerGaugeUI: PowerGaugeContainerBackground not assigned! Pulsation effect will not work.");
        }
        else
        {
            // Set initial background to normal color
            powerGaugeContainerBackground.color = normalBackgroundColor;
        }
        
        // Subscribe to stats updates
        if (StatsSystem.Instance != null)
        {
            StatsSystem.Instance.OnStatsUpdated += OnStatsUpdated;
            
            // Get initial power reading
            currentPowerMW = StatsSystem.Instance.GetCurrentPowerMW();
            UpdateGaugeDisplay(currentPowerMW);
        }
        else
        {
            Debug.LogWarning("PowerGaugeUI: StatsSystem not found!");
            // Set fallback initial values
            currentPowerMW = basePowerMW; // Use base power as fallback
        }
        
        // Set initial needle position and color
        if (needleTransform != null)
        {
            currentNeedleAngle = CalculateNeedleAngle(currentPowerMW);
            needleTransform.localRotation = Quaternion.Euler(0, 0, currentNeedleAngle);
        }
        
        // Set initial needle color
        UpdateNeedleColor(currentPowerMW);
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (StatsSystem.Instance != null)
        {
            StatsSystem.Instance.OnStatsUpdated -= OnStatsUpdated;
        }
        
        // Stop any running coroutines
        if (backgroundPulsationCoroutine != null)
        {
            StopCoroutine(backgroundPulsationCoroutine);
        }
    }
    
    // Called by StatsSystem when stats update
    private void OnStatsUpdated(float powerMW, float waterLiterPerSecond, float co2KgPerSecond, float totalCO2Kg)
    {
        UpdateGaugeDisplay(powerMW);
    }
    
    private void UpdateGaugeDisplay(float powerMW)
    {
        if (Mathf.Approximately(currentPowerMW, powerMW))
            return; // No change needed
            
        currentPowerMW = powerMW;
        
        // Calculate target needle angle
        float newTargetAngle = CalculateNeedleAngle(powerMW);
        
        // Update needle color based on current percentage
        UpdateNeedleColor(powerMW);
        
        // Update background state (pulsation)
        UpdateBackgroundState(powerMW);
        
        // Animate needle to new position
        AnimateNeedleToAngle(newTargetAngle);
        
        if (showDebugInfo)
        {
            float percentage = CalculateGaugePercentage(powerMW);
            string zone = GetCurrentZone();
            Debug.Log($"PowerGauge: {powerMW:F0} MW = {percentage:F1}% (angle: {newTargetAngle:F1}°) - Zone: {zone}");
        }
    }
    
    private float CalculateGaugePercentage(float powerMW)
    {
        // Base power (e.g., 700 MW) = 35% on gauge
        // Calculate percentage based on this relationship
        float powerRatio = powerMW / basePowerMW;
        float percentage = basePowerPercentage * powerRatio;
        
        return percentage;
    }
    
    private float CalculateNeedleAngle(float powerMW)
    {
        float percentage = CalculateGaugePercentage(powerMW);
        
        // Clamp percentage to gauge limits
        percentage = Mathf.Clamp(percentage, 0f, maxGaugePercentage);
        
        // Convert percentage to needle angle
        float normalizedPercentage = percentage / 100f; // 0 to 1.2 (since max is 120%)
        float angle = Mathf.Lerp(minNeedleAngle, maxNeedleAngle, normalizedPercentage);
        
        return angle;
    }
    
    private void UpdateNeedleColor(float powerMW)
    {
        if (!useNeedleGlow || needleImage == null) 
            return;
        
        float percentage = CalculateGaugePercentage(powerMW);
        Color targetColor;
        
        if (percentage <= 70f)
        {
            // Normal zone (0-70%) - White/Normal needle
            targetColor = normalNeedleColor;
        }
        else if (percentage <= 90f)
        {
            // Warning zone (70-90%) - Yellow needle
            float t = (percentage - 70f) / 20f; // 0 to 1 within warning zone
            targetColor = Color.Lerp(normalNeedleColor, warningNeedleColor, t);
        }
        else if (percentage <= 100f)
        {
            // Danger zone (90-100%) - Red needle
            float t = (percentage - 90f) / 10f; // 0 to 1 within danger zone
            targetColor = Color.Lerp(warningNeedleColor, dangerNeedleColor, t);
        }
        else
        {
            // Critical zone (100%+) - Bright red-orange needle
            targetColor = criticalNeedleColor;
        }
        
        // Apply color smoothly
        needleImage.color = Color.Lerp(needleImage.color, targetColor, Time.deltaTime * 3f);
    }
    
    private void UpdateBackgroundState(float powerMW)
    {
        if (powerGaugeContainerBackground == null)
            return;
            
        float percentage = CalculateGaugePercentage(powerMW);
        bool shouldBeInWarningZone = percentage > 85f; // Warning zone starts at 70%
        
        // Check if we need to change state
        if (shouldBeInWarningZone != isInWarningZone)
        {
            isInWarningZone = shouldBeInWarningZone;
            
            if (isInWarningZone)
            {
                // Enter warning zone - start pulsation
                StartBackgroundPulsation();
                if (showDebugInfo)
                    Debug.Log("PowerGauge: Entered WARNING zone - Background pulsation started");
            }
            else
            {
                // Exit warning zone - return to normal (though this shouldn't happen based on game design)
                StopBackgroundPulsation();
                powerGaugeContainerBackground.color = normalBackgroundColor;
                if (showDebugInfo)
                    Debug.Log("PowerGauge: Exited WARNING zone - Background pulsation stopped");
            }
        }
    }
    
    private void StartBackgroundPulsation()
    {
        if (powerGaugeContainerBackground == null)
            return;
            
        // Stop any existing pulsation
        if (backgroundPulsationCoroutine != null)
        {
            StopCoroutine(backgroundPulsationCoroutine);
        }
        
        // Start new pulsation
        backgroundPulsationCoroutine = StartCoroutine(BackgroundPulsationCoroutine());
    }
    
    private void StopBackgroundPulsation()
    {
        if (backgroundPulsationCoroutine != null)
        {
            StopCoroutine(backgroundPulsationCoroutine);
            backgroundPulsationCoroutine = null;
        }
    }
    
    private IEnumerator BackgroundPulsationCoroutine()
    {
        if (showDebugInfo)
            Debug.Log("PowerGauge: Background pulsation coroutine started");
            
        while (isInWarningZone && powerGaugeContainerBackground != null)
        {
            // Calculate sine wave for smooth pulsation
            float time = Time.time * pulsationSpeed * 2f * Mathf.PI; // Convert pulses per second to radians
            float sineValue = Mathf.Sin(time);
            
            // Convert sine wave (-1 to 1) to (0 to 1) for color interpolation
            float normalizedSine = (sineValue + 1f) / 2f;
            
            // Interpolate between dark red and bright red
            Color currentColor = Color.Lerp(warningDarkRedColor, warningBrightRedColor, normalizedSine);
            
            // Apply color to background
            powerGaugeContainerBackground.color = currentColor;
            
            if (showDebugInfo && Time.frameCount % 60 == 0) // Log every 60 frames to avoid spam
            {
                Debug.Log($"PowerGauge: Pulsation - sine: {sineValue:F2}, normalized: {normalizedSine:F2}, color: {currentColor}");
            }
            
            yield return null;
        }
        
        if (showDebugInfo)
            Debug.Log("PowerGauge: Background pulsation coroutine ended");
            
        backgroundPulsationCoroutine = null;
    }
    
    private void AnimateNeedleToAngle(float targetAngle)
    {
        if (needleTransform == null) return;
        
        targetNeedleAngle = targetAngle;
        
        // Stop any existing animation
        if (needleAnimationCoroutine != null)
        {
            StopCoroutine(needleAnimationCoroutine);
        }
        
        // Start new animation
        needleAnimationCoroutine = StartCoroutine(AnimateNeedleCoroutine(targetAngle));
    }
    
    private IEnumerator AnimateNeedleCoroutine(float targetAngle)
    {
        float startAngle = currentNeedleAngle;
        float animationDuration = Mathf.Abs(targetAngle - startAngle) / (180f * needleAnimationSpeed); // Scale duration by angle difference
        animationDuration = Mathf.Clamp(animationDuration, 0.1f, 2f); // Min 0.1s, max 2s
        
        float elapsed = 0f;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            
            // Apply easing curve
            float easedT = needleEasing.Evaluate(t);
            
            // Interpolate angle
            currentNeedleAngle = Mathf.Lerp(startAngle, targetAngle, easedT);
            
            // Apply rotation
            needleTransform.localRotation = Quaternion.Euler(0, 0, currentNeedleAngle);
            
            yield return null;
        }
        
        // Ensure final position is exact
        currentNeedleAngle = targetAngle;
        needleTransform.localRotation = Quaternion.Euler(0, 0, currentNeedleAngle);
        
        needleAnimationCoroutine = null;
    }
    
    // Public method to set power directly (for testing)
    public void SetPower(float powerMW)
    {
        UpdateGaugeDisplay(powerMW);
    }
    
    // Get current zone for external use
    public string GetCurrentZone()
    {
        float percentage = CalculateGaugePercentage(currentPowerMW);
        
        if (percentage <= 70f) return "Normal";
        else if (percentage <= 90f) return "Warning";
        else if (percentage <= 100f) return "Danger";
        else return "Critical";
    }
    
    // Debug context menu methods
    [ContextMenu("Test Base Power")]
    public void TestBasePower()
    {
        SetPower(basePowerMW); // Should show at 35%
    }
    
    [ContextMenu("Test 1.5x Power")]
    public void Test1_5xPower()
    {
        SetPower(basePowerMW * 1.5f); // Should show at ~52.5%
    }
    
    [ContextMenu("Test 2x Power (Warning Zone)")]
    public void Test2xPower()
    {
        SetPower(basePowerMW * 2f); // Should show at 70% and trigger pulsation
    }
    
    [ContextMenu("Test 3x Power")]
    public void Test3xPower()
    {
        SetPower(basePowerMW * 3f); // Should show at 105% (beyond red zone)
    }
    
    [ContextMenu("Test 4x Power (CAPTCHA)")]
    public void Test4xPower()
    {
        SetPower(basePowerMW * 4f); // Should show at 140% (max urgency)
    }
    
    [ContextMenu("Test Warning Zone Pulsation")]
    public void TestWarningPulsation()
    {
        // Force warning zone for testing
        SetPower(basePowerMW * 2f); // This should trigger pulsation at 70%
    }
}