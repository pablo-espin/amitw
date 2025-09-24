using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// Handles cutscene skip functionality with fade transitions and input filtering
/// </summary>
public class CutsceneSkipManager : MonoBehaviour
{
    [Header("Skip UI")]
    [SerializeField] private GameObject skipMessageUI;
    [SerializeField] private Text skipMessageText;
    [SerializeField] private CanvasGroup skipMessageCanvasGroup;
    
    [Header("Skip Settings")]
    [SerializeField] private float skipMessageDelay = 5f; // Show message after 5 seconds
    [SerializeField] private float skipMessageDuration = 5f; // Show for 5 seconds
    [SerializeField] private float skipFadeInTime = 0.5f;
    [SerializeField] private float skipFadeOutTime = 0.5f;
    [SerializeField] private float skipTransitionTime = 1.5f; // Fade time when skipping
    
    [Header("References")]
    [SerializeField] private SimpleCutscenePlayer cutscenePlayer;
    [SerializeField] private Image fadePanel; // Should be the same fade panel from cutscene player
    
    // Input handling
    private bool isSkipActive = false;
    private bool hasBeenSkipped = false;
    private Coroutine skipMessageCoroutine;
    
    // Input filtering - exclude system keys
    private readonly KeyCode[] excludedKeys = {
        KeyCode.LeftAlt, KeyCode.RightAlt,
        KeyCode.LeftControl, KeyCode.RightControl,
        KeyCode.LeftCommand, KeyCode.RightCommand,
        KeyCode.LeftWindows, KeyCode.RightWindows,
        KeyCode.Menu, KeyCode.Help, KeyCode.Print, KeyCode.SysReq
    };
    
    private void Start()
    {
        InitializeSkipUI();
        StartSkipMessageTimer();
    }
    
    private void InitializeSkipUI()
    {
        if (skipMessageUI != null)
        {
            skipMessageUI.SetActive(false);
        }
        
        if (skipMessageCanvasGroup != null)
        {
            skipMessageCanvasGroup.alpha = 0f;
        }
        
        if (skipMessageText != null)
        {
            skipMessageText.text = "Press any key to skip";
        }
    }
    
    private void StartSkipMessageTimer()
    {
        if (skipMessageCoroutine != null)
        {
            StopCoroutine(skipMessageCoroutine);
        }
        skipMessageCoroutine = StartCoroutine(SkipMessageSequence());
    }
    
    private IEnumerator SkipMessageSequence()
    {
        // Wait for initial delay
        yield return new WaitForSeconds(skipMessageDelay);
        
        // Don't show if already skipped
        if (hasBeenSkipped) yield break;
        
        // Show skip message
        if (skipMessageUI != null)
        {
            skipMessageUI.SetActive(true);
        }
        
        // Fade in message
        yield return StartCoroutine(FadeSkipMessage(true));
        isSkipActive = true;
        
        // Keep message visible for duration
        yield return new WaitForSeconds(skipMessageDuration);
        
        // Fade out message
        yield return StartCoroutine(FadeSkipMessage(false));
        
        if (skipMessageUI != null)
        {
            skipMessageUI.SetActive(false);
        }
        
        // Keep skip functionality active even after message disappears
    }
    
    private IEnumerator FadeSkipMessage(bool fadeIn)
    {
        if (skipMessageCanvasGroup == null) yield break;
        
        float startAlpha = fadeIn ? 0f : 1f;
        float endAlpha = fadeIn ? 1f : 0f;
        float fadeTime = fadeIn ? skipFadeInTime : skipFadeOutTime;
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeTime;
            skipMessageCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            yield return null;
        }
        
        skipMessageCanvasGroup.alpha = endAlpha;
    }
    
    private void Update()
    {
        // Only check for input if not already skipped
        if (hasBeenSkipped) return;
        
        // Check for any key press (excluding system keys)
        if (Input.anyKeyDown)
        {
            if (IsValidSkipKey())
            {
                SkipCutscene();
            }
        }
    }
    
    private bool IsValidSkipKey()
    {
        // Check if any excluded keys are pressed
        foreach (KeyCode excludedKey in excludedKeys)
        {
            if (Input.GetKeyDown(excludedKey))
            {
                return false;
            }
        }
        
        // Additional check for modifier keys being held
        if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt) ||
            Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
            Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand))
        {
            return false;
        }
        
        return true;
    }
    
    private void SkipCutscene()
    {
        if (hasBeenSkipped) return;
        
        hasBeenSkipped = true;
        
        // Stop the skip message coroutine
        if (skipMessageCoroutine != null)
        {
            StopCoroutine(skipMessageCoroutine);
        }
        
        // Hide skip message immediately
        if (skipMessageUI != null)
        {
            skipMessageUI.SetActive(false);
        }
        
        // Stop the cutscene player and start skip transition
        if (cutscenePlayer != null)
        {
            cutscenePlayer.StopCutscene();
        }
        
        StartCoroutine(SkipTransition());
    }
    
    private IEnumerator SkipTransition()
    {
        // Fade to black
        yield return StartCoroutine(FadeToBlack());
        
        // Load the game level
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGameplay();
        }
        else
        {
            // Fallback
            UnityEngine.SceneManagement.SceneManager.LoadScene("GameLevel");
        }
    }
    
    private IEnumerator FadeToBlack()
    {
        if (fadePanel == null) yield break;
        
        // Make sure fade panel is active
        fadePanel.gameObject.SetActive(true);
        
        Color startColor = fadePanel.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 1f);
        
        float elapsedTime = 0f;
        
        while (elapsedTime < skipTransitionTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / skipTransitionTime;
            fadePanel.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }
        
        fadePanel.color = endColor;
    }
    
    // Public method to disable skip (called when cutscene naturally ends)
    public void DisableSkip()
    {
        hasBeenSkipped = true;
        
        if (skipMessageCoroutine != null)
        {
            StopCoroutine(skipMessageCoroutine);
        }
        
        if (skipMessageUI != null)
        {
            skipMessageUI.SetActive(false);
        }
    }
}