using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ManualHUDIndicator : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private float pulseMinScale = 0.9f;
    [SerializeField] private float pulseMaxScale = 1.1f;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private int highlightPulseCount = 3;
    [SerializeField] private TextMeshProUGUI keyHintText;
    
    [Header("References")]
    [SerializeField] private ManualSystem manualSystem;
    
    private RectTransform rectTransform;
    private bool isPulsing = false;
    private float pulseTimer = 0f;
    private int pulseCount = 0;
    private Vector3 originalScale;
    
    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        
        if (rectTransform != null)
            originalScale = rectTransform.localScale;
            
        // Hide initially if we don't have the manual yet
        if (manualSystem != null)
        {
            bool hasManual = manualSystem.HasManualBeenFound();
            Debug.Log($"ManualHUDIndicator - Manual found: {hasManual}");
            
            if (!hasManual)
                gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("ManualHUDIndicator - manualSystem reference is null!");
            // Try to find it
            manualSystem = FindObjectOfType<ManualSystem>();
            if (manualSystem != null)
                Debug.Log("ManualHUDIndicator - Found ManualSystem via FindObjectOfType");
            else
                Debug.LogError("ManualHUDIndicator - Could not find ManualSystem!");
        }
            
        // Set key hint text if available
        if (keyHintText != null)
            keyHintText.text = "M";
            
        // Start with a pulse highlight when first activated
        if (gameObject.activeSelf)
            StartPulseHighlight();
    }
    
    private void Update()
    {
        if (isPulsing)
            PulseEffect();
    }
    
    // Start the pulse effect when the player enters a new area or gets the manual
    public void StartPulseHighlight()
    {
        isPulsing = true;
        pulseTimer = 0f;
        pulseCount = 0;
    }
    
    private void PulseEffect()
    {
        if (rectTransform == null)
            return;
            
        pulseTimer += Time.deltaTime;
        float pulseFactor = Mathf.Sin(pulseTimer * pulseSpeed * Mathf.PI) * 0.5f + 0.5f; // 0 to 1 value
        
        // Scale between min and max scale
        float currentScale = Mathf.Lerp(pulseMinScale, pulseMaxScale, pulseFactor);
        rectTransform.localScale = originalScale * currentScale;
        
        // Check if a full pulse cycle is complete (from min to max and back to min)
        if (pulseTimer >= 1f / pulseSpeed)
        {
            pulseTimer = 0f;
            pulseCount++;
            
            // If we've completed the desired number of pulses, stop
            if (pulseCount >= highlightPulseCount)
            {
                isPulsing = false;
                rectTransform.localScale = originalScale; // Reset to original scale
            }
        }
    }
}