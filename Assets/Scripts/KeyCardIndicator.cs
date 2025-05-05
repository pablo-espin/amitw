using UnityEngine;

public class KeyCardIndicator : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private float pulseMinScale = 0.9f;
    [SerializeField] private float pulseMaxScale = 1.1f;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private int highlightPulseCount = 3;
    
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
    }
    
    private void Update()
    {
        if (isPulsing)
            PulseEffect();
    }
    
    // Start the pulse effect when key card is acquired
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