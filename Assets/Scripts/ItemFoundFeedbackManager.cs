using System.Collections;
using UnityEngine;
using TMPro;

public class ItemFoundFeedbackManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI centerItemFoundText;
    [SerializeField] private TextMeshProUGUI hudPopupText;
    [SerializeField] private UnityEngine.UI.Image hudPopupBackground;
    
    [Header("Text Content")]
    [SerializeField] private string codeFoundTextContent = "Code Found!";
    [SerializeField] private string keycardFoundTextContent = "Keycard Found!";
    [SerializeField] private string manualFoundTextContent = "Manual Found!";
    [SerializeField] private string hudPopupTextContent = "Found codes are stored here";
    
    [Header("Animation Settings")]
    [SerializeField] private float sequenceDelay = 1f;
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private float centerTextDisplayTime = 2.5f;
    [SerializeField] private float phaseDelay = 5f;
    [SerializeField] private float hudPopupDisplayTime = 5f;
    
    // Singleton instance
    public static ItemFoundFeedbackManager Instance { get; private set; }
    
    // State tracking
    private bool isSequenceActive = false;
    private bool hasShownFirstTimeCodePopup = false;
    
    // Original colors for fade animations
    private Color centerTextOriginalColor;
    private Color hudPopupOriginalColor;
    private Color hudPopupBackgroundOriginalColor;
    
    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Initialize UI elements
        InitializeUI();
    }
    
    private void InitializeUI()
    {
        if (centerItemFoundText != null)
        {
            centerTextOriginalColor = centerItemFoundText.color;
            // Start invisible
            Color invisibleColor = centerTextOriginalColor;
            invisibleColor.a = 0f;
            centerItemFoundText.color = invisibleColor;
        }
        else
        {
            Debug.LogWarning("ItemFoundFeedbackManager: centerItemFoundText not assigned in inspector!");
        }
        
        if (hudPopupText != null)
        {
            hudPopupText.text = hudPopupTextContent;
            hudPopupOriginalColor = hudPopupText.color;
            // Start invisible
            Color invisibleColor = hudPopupOriginalColor;
            invisibleColor.a = 0f;
            hudPopupText.color = invisibleColor;
        }
        else
        {
            Debug.LogWarning("ItemFoundFeedbackManager: hudPopupText not assigned in inspector!");
        }
        
        if (hudPopupBackground != null)
        {
            hudPopupBackgroundOriginalColor = hudPopupBackground.color;
            // Start invisible
            Color invisibleColor = hudPopupBackgroundOriginalColor;
            invisibleColor.a = 0f;
            hudPopupBackground.color = invisibleColor;
        }
        else
        {
            Debug.LogWarning("ItemFoundFeedbackManager: hudPopupBackground not assigned in inspector!");
        }
    }
    
    // Main method to trigger the code found feedback sequence (with HUD popup)
    // Called by clue systems when a code is discovered
    public void ShowCodeFoundSequence()
    {
        ShowItemFoundSequence(codeFoundTextContent, true);
    }
    
    // Method to trigger keycard found feedback (center text only)
    // Called when keycard is found
    public void ShowKeycardFoundSequence()
    {
        ShowItemFoundSequence(keycardFoundTextContent, false);
    }
    
    // Method to trigger manual found feedback (center text only)
    // Called when manual is found
    public void ShowManualFoundSequence()
    {
        ShowItemFoundSequence(manualFoundTextContent, false);
    }
    
    // Internal method to handle all item found sequences
    private void ShowItemFoundSequence(string messageText, bool showHUDPopup)
    {
        // Safeguard: prevent multiple simultaneous sequences
        if (isSequenceActive)
        {
            return;
        }
        
        // Start the sequence
        StartCoroutine(ItemFoundSequenceCoroutine(messageText, showHUDPopup));
    }
    
    // Main sequence coroutine that handles the feedback
    private IEnumerator ItemFoundSequenceCoroutine(string messageText, bool showHUDPopup)
    {
        isSequenceActive = true;
        
        try
        {
            // Initial delay before starting the sequence
            yield return new WaitForSeconds(sequenceDelay);
            
            // Phase 1: Show center item found text
            yield return StartCoroutine(ShowCenterText(messageText));
            
            // Phase 2: Show HUD popup (only for codes and only first time)
            if (showHUDPopup && !hasShownFirstTimeCodePopup)
            {
                // Additional delay between center text and HUD popup
                yield return new WaitForSeconds(phaseDelay);

                yield return StartCoroutine(ShowHUDPopup());
                hasShownFirstTimeCodePopup = true;
            }
        }
        finally
        {
            // Always reset the sequence flag, even if there was an error
            isSequenceActive = false;
        }
    }
    
    // Phase 1: Display center item found text for 2.5 seconds
    private IEnumerator ShowCenterText(string messageText)
    {
        if (centerItemFoundText == null) yield break;
        
        // Set the message text
        centerItemFoundText.text = messageText;
        
        // Fade in
        yield return StartCoroutine(FadeInText(centerItemFoundText, centerTextOriginalColor));
        
        // Wait for display time (minus fade times)
        float waitTime = Mathf.Max(0, centerTextDisplayTime - fadeInDuration - fadeOutDuration);
        yield return new WaitForSeconds(waitTime);
        
        // Fade out
        yield return StartCoroutine(FadeOutText(centerItemFoundText));
    }
    
    // Phase 2: Display HUD popup for 5 seconds (first time only)
    private IEnumerator ShowHUDPopup()
    {
        if (hudPopupText == null && hudPopupBackground == null) yield break;
        
        // Play notification sound when popup appears
        if (UISoundManager.Instance != null)
        {
            UISoundManager.Instance.PlayNotification();
        }
        else
        {
            Debug.LogWarning("ItemFoundFeedbackManager: UISoundManager.Instance is null, cannot play notification sound");
        }
        
        // Fade in both background and text simultaneously
        yield return StartCoroutine(FadeInHUDPopup());
        
        // Wait for display time (minus fade times)
        float waitTime = Mathf.Max(0, hudPopupDisplayTime - fadeInDuration - fadeOutDuration);
        yield return new WaitForSeconds(waitTime);
        
        // Fade out both background and text simultaneously
        yield return StartCoroutine(FadeOutHUDPopup());
    }
    
    // Fade in animation for text elements
    private IEnumerator FadeInText(TextMeshProUGUI textElement, Color targetColor)
    {
        if (textElement == null) yield break;
        
        Color startColor = textElement.color;
        startColor.a = 0f;
        textElement.color = startColor;
        
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, targetColor.a, elapsed / fadeInDuration);
            
            Color currentColor = targetColor;
            currentColor.a = alpha;
            textElement.color = currentColor;
            
            yield return null;
        }
        
        // Ensure final color is set
        textElement.color = targetColor;
    }
    
    // Fade out animation for text elements
    private IEnumerator FadeOutText(TextMeshProUGUI textElement)
    {
        if (textElement == null) yield break;
        
        Color startColor = textElement.color;
        Color endColor = startColor;
        endColor.a = 0f;
        
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / fadeOutDuration);
            
            Color currentColor = startColor;
            currentColor.a = alpha;
            textElement.color = currentColor;
            
            yield return null;
        }
        
        // Ensure final invisible state
        textElement.color = endColor;
    }
    
    // Fade in animation for the HUD popup (both background and text)
    private IEnumerator FadeInHUDPopup()
    {
        // Set starting alpha to 0 for both elements
        if (hudPopupBackground != null)
        {
            Color startColor = hudPopupBackgroundOriginalColor;
            startColor.a = 0f;
            hudPopupBackground.color = startColor;
        }
        
        if (hudPopupText != null)
        {
            Color startColor = hudPopupOriginalColor;
            startColor.a = 0f;
            hudPopupText.color = startColor;
        }
        
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = elapsed / fadeInDuration;
            
            // Fade in background
            if (hudPopupBackground != null)
            {
                Color currentColor = hudPopupBackgroundOriginalColor;
                currentColor.a = Mathf.Lerp(0f, hudPopupBackgroundOriginalColor.a, alpha);
                hudPopupBackground.color = currentColor;
            }
            
            // Fade in text
            if (hudPopupText != null)
            {
                Color currentColor = hudPopupOriginalColor;
                currentColor.a = Mathf.Lerp(0f, hudPopupOriginalColor.a, alpha);
                hudPopupText.color = currentColor;
            }
            
            yield return null;
        }
        
        // Ensure final colors are set
        if (hudPopupBackground != null)
        {
            hudPopupBackground.color = hudPopupBackgroundOriginalColor;
        }
        
        if (hudPopupText != null)
        {
            hudPopupText.color = hudPopupOriginalColor;
        }
    }
    
    // Fade out animation for the HUD popup (both background and text)
    private IEnumerator FadeOutHUDPopup()
    {
        Color backgroundStartColor = hudPopupBackground != null ? hudPopupBackground.color : Color.clear;
        Color textStartColor = hudPopupText != null ? hudPopupText.color : Color.clear;
        
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / fadeOutDuration);
            
            // Fade out background
            if (hudPopupBackground != null)
            {
                Color currentColor = backgroundStartColor;
                currentColor.a = Mathf.Lerp(0f, backgroundStartColor.a, alpha);
                hudPopupBackground.color = currentColor;
            }
            
            // Fade out text
            if (hudPopupText != null)
            {
                Color currentColor = textStartColor;
                currentColor.a = Mathf.Lerp(0f, textStartColor.a, alpha);
                hudPopupText.color = currentColor;
            }
            
            yield return null;
        }
        
        // Ensure final invisible state for both
        if (hudPopupBackground != null)
        {
            Color finalColor = hudPopupBackgroundOriginalColor;
            finalColor.a = 0f;
            hudPopupBackground.color = finalColor;
        }
        
        if (hudPopupText != null)
        {
            Color finalColor = hudPopupOriginalColor;
            finalColor.a = 0f;
            hudPopupText.color = finalColor;
        }
    }
    
    // Public method to check if the feedback sequence is currently active
    public bool IsSequenceActive()
    {
        return isSequenceActive;
    }
    
    // Public method to check if the first-time code popup has been shown
    public bool HasShownFirstTimeCodePopup()
    {
        return hasShownFirstTimeCodePopup;
    }
    
    // Debug method to manually reset the first-time popup flag
    [ContextMenu("Reset First Time Code Popup Flag")]
    private void ResetFirstTimeCodePopupFlag()
    {
        hasShownFirstTimeCodePopup = false;
    }
    
    // Debug method to test the code found sequence
    [ContextMenu("Test Code Found Sequence")]
    private void TestCodeFoundSequence()
    {
        ShowCodeFoundSequence();
    }
    
    // Debug method to test the keycard found sequence
    [ContextMenu("Test Keycard Found Sequence")]
    private void TestKeycardFoundSequence()
    {
        ShowKeycardFoundSequence();
    }
    
    // Debug method to test the manual found sequence
    [ContextMenu("Test Manual Found Sequence")]
    private void TestManualFoundSequence()
    {
        ShowManualFoundSequence();
    }
}