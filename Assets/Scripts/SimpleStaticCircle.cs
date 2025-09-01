using UnityEngine;
using UnityEngine.UI;

public class SimpleStaticCircle : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private RectTransform arrowTransform; // Reference to the arrow
    [SerializeField] private Image circleImage; // The static circle image

    [Header("Opacity Animation")]
    [SerializeField] private bool enableOpacityOscillation = true;
    [SerializeField] private float cycleDuration = 1f; // Time for one complete cycle (0 to 0.5 and back to 0)
    [SerializeField] private float minOpacity = 0f;
    [SerializeField] private float maxOpacity = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private RectTransform circleTransform;
    private Color baseColor;
    private float cycleTimer = 0f;

    private void Start()
    {
        // Get our own RectTransform
        circleTransform = GetComponent<RectTransform>();

        // Try to find the arrow transform if not assigned
        if (arrowTransform == null)
        {
            PlayerMapArrow arrowComponent = FindObjectOfType<PlayerMapArrow>();
            if (arrowComponent != null)
            {
                arrowTransform = arrowComponent.GetComponent<RectTransform>();
                if (showDebugLogs)
                    Debug.Log("SimpleStaticCircle: Auto-found arrow transform");
            }
            else
            {
                Debug.LogWarning("SimpleStaticCircle: Could not find PlayerMapArrow component!");
            }
        }

        // Get circle image if not assigned
        if (circleImage == null)
        {
            circleImage = GetComponent<Image>();
        }

        // Set circle properties
        if (circleImage != null)
        {
            baseColor = new Color(0f, 0.66f, 0.78f, 1f); // Same color as arrow
            circleImage.color = baseColor;
            circleImage.raycastTarget = false; // Don't block interactions
            Color initialColor = baseColor;
            initialColor.a = minOpacity;
            circleImage.color = initialColor;

            if (showDebugLogs)
                Debug.Log("SimpleStaticCircle: Circle image setup complete");
        }
        else
        {
            Debug.LogError("SimpleStaticCircle: No Image component found!");
        }
    }

    private void Update()
    {
        // Copy position from arrow every frame
        if (arrowTransform != null && circleTransform != null)
        {
            Vector2 arrowPosition = arrowTransform.anchoredPosition;
            circleTransform.anchoredPosition = arrowPosition;

            if (showDebugLogs && Time.frameCount % 60 == 0) // Log every second
            {
                Debug.Log($"SimpleStaticCircle: Following arrow at position {arrowPosition}");
            }
        }

        // Handle opacity oscillation
        if (enableOpacityOscillation)
        {
            UpdateOpacityOscillation();
        }
    }

    private void UpdateOpacityOscillation()
    {
        // Update timer
        cycleTimer += Time.deltaTime;

        // Calculate progress through the cycle (0 to 1)
        float cycleProgress = cycleTimer / cycleDuration;

        // If we've completed a full cycle, reset
        if (cycleProgress >= 1f)
        {
            cycleTimer = 0f;
            cycleProgress = 0f;

            if (showDebugLogs)
                Debug.Log("SimpleStaticCircle: Cycle complete - resetting to 0 opacity");
        }

        // Smooth fade from 0 to maxOpacity (linear interpolation)
        float currentOpacity = Mathf.Lerp(minOpacity, maxOpacity, cycleProgress);

        // Apply to circle
        Color currentColor = baseColor;
        currentColor.a = currentOpacity;
        circleImage.color = currentColor;

        // Debug opacity every 30 frames
        if (showDebugLogs && Time.frameCount % 30 == 0)
        {
            Debug.Log($"SimpleStaticCircle: Progress = {cycleProgress:F2}, Opacity = {currentOpacity:F2}");
        }
    }

    // Start opacity oscillation
    public void StartOscillation()
    {
        enableOpacityOscillation = true;
        cycleTimer = 0f;

        if (showDebugLogs)
            Debug.Log("SimpleStaticCircle: Started opacity oscillation");
    }


    // Stop opacity oscillation
    public void StopOscillation()
    {
        enableOpacityOscillation = false;

        // Reset to base opacity
        if (circleImage != null)
        {
            Color resetColor = baseColor;
            resetColor.a = minOpacity;
            circleImage.color = resetColor;
        }

        if (showDebugLogs)
            Debug.Log("SimpleStaticCircle: Stopped opacity oscillation");
    }


    // Set the oscillation speed
    public void SetCycleDuration(float duration)
    {
        cycleDuration = duration;

        if (showDebugLogs)
            Debug.Log($"SimpleStaticCircle: Cycle duration set to {duration}");
    }

    // Set the opacity range
    public void SetOpacityRange(float min, float max)
    {
        minOpacity = min;
        maxOpacity = max;

        if (showDebugLogs)
            Debug.Log($"SimpleStaticCircle: Opacity range set to {min} - {max}");
    }

    // Set the arrow transform reference manually
    public void SetArrowTransform(RectTransform arrow)
    {
        arrowTransform = arrow;
        if (showDebugLogs)
            Debug.Log($"SimpleStaticCircle: Arrow transform set to {arrow.name}");
    }
}