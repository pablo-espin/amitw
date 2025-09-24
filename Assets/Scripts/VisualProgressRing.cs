using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class VisualProgressRing : MonoBehaviour
{
    [Header("Ring Components")]
    [SerializeField] private Image progressRingImage;
    [SerializeField] private Image glowRingImage; // Glow ring
    [SerializeField] private Image progressRingBackgroundImage; // Ring background
    [SerializeField] private TextMeshProUGUI progressCounterText;
    
    [Header("Visual Settings")]
    [SerializeField] private float ringFillDuration = 0.8f;
    
    [Header("Glow Effect Settings")]
    [SerializeField] private float glowScale = 1.2f; // How much larger the glow ring is
    [SerializeField] private float maxGlowOpacity = 0.6f; // Maximum opacity for the glow
    
    [Header("Clue Colors")]
    [SerializeField] private Color waterColor = new Color(0.2f, 0.6f, 1f, 1f); // Blue
    [SerializeField] private Color electricityColor = new Color(1f, 0.9f, 0.2f, 1f); // Yellow
    [SerializeField] private Color locationColor = new Color(1f, 0.7f, 0.9f, 1f); // Pale Pink
    [SerializeField] private Color computerCodeColor = new Color(0.4f, 0.2f, 0.6f, 1f); // Dark Purple
    
    [Header("Glow Colors")]
    [SerializeField] private Color waterGlowColor = new Color(0.4f, 0.8f, 1f, 1f); // Brighter blue glow
    [SerializeField] private Color electricityGlowColor = new Color(1f, 1f, 0.6f, 1f); // Brighter yellow glow
    [SerializeField] private Color locationGlowColor = new Color(1f, 0.9f, 1f, 1f); // Brighter pink glow
    [SerializeField] private Color computerGlowColor = new Color(0.7f, 0.4f, 0.9f, 1f); // Brighter purple glow
    
    private int totalCodes = 3;
    private int enteredCodes = 0;
    private bool isCorrupted = false;
    private CodeType lastEnteredCodeType = CodeType.Water; // Track the most recent code type
    
    public enum CodeType
    {
        Water,
        Electricity, 
        Location,
        Computer
    }
    
    private void Awake()
    {
        ResetVisuals();
    }
    
    public void ResetVisuals()
    {
        enteredCodes = 0;
        isCorrupted = false;
        lastEnteredCodeType = CodeType.Water; // Reset to default
        
        if (progressRingImage != null)
        {
            progressRingImage.fillAmount = 0f;
            progressRingImage.color = Color.white;
        }
        
        if (glowRingImage != null)
        {
            glowRingImage.fillAmount = 0f;
            glowRingImage.color = Color.clear; // Fully transparent initially
        }
        
        UpdateProgressText();
    }
    
    public void AddCodeVisual(CodeType codeType)
    {
        if (isCorrupted) return;
        
        if (codeType == CodeType.Computer)
        {
            StartCoroutine(AnimateCorruption());
        }
        else
        {
            enteredCodes++;
            StartCoroutine(AnimateCodeEntry(codeType));
        }
    }
    
    private IEnumerator AnimateCodeEntry(CodeType codeType)
    {
        if (progressRingImage == null) yield break;
        
        float startFill = progressRingImage.fillAmount;
        float targetFill = (float)enteredCodes / totalCodes;
        Color targetColor = GetCodeColor(codeType);
        Color glowColor = GetGlowColor(codeType);
        
        // Store this as the most recent code type
        lastEnteredCodeType = codeType;

        float extendedDuration = ringFillDuration * 1.3f; // 130% of original duration
        float elapsed = 0f;
        while (elapsed < extendedDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / extendedDuration; // t goes from 0 to 1 over 130% duration

            // Progress ring animation
            float ringProgress = Mathf.Min(t * 1.3f, 1f); // Scale t back to 0-1 range for ring, but cap at 1
            progressRingImage.fillAmount = Mathf.Lerp(startFill, targetFill, EaseOutQuart(ringProgress));
            progressRingImage.color = Color.Lerp(progressRingImage.color, targetColor, ringProgress);
            
            // Update glow ring fill to match progress ring
            if (glowRingImage != null)
            {
                glowRingImage.fillAmount = progressRingImage.fillAmount;
                
                // Set glow color and calculate opacity based on extended animation progress (t)
                Color currentGlowColor = Color.Lerp(glowRingImage.color, glowColor, Mathf.Min(ringProgress * 2f, 1f));
                
                // Calculate glow opacity based on extended animation progress
                float glowOpacity = CalculateGlowOpacityFromProgress(t);
                currentGlowColor.a = glowOpacity;
                
                glowRingImage.color = currentGlowColor;
            }
            
            yield return null;
        }
        
        progressRingImage.fillAmount = targetFill;
        progressRingImage.color = targetColor;
        
        // Ensure glow ring is transparent at the end (animation complete)
        if (glowRingImage != null)
        {
            glowRingImage.fillAmount = targetFill;
            Color finalGlowColor = glowColor;
            finalGlowColor.a = 0f; // Always 0 at end of animation
            glowRingImage.color = finalGlowColor;
        }
        
        UpdateProgressText();
        
        // Trigger completion effect if all codes entered
        if (enteredCodes >= totalCodes)
        {
            yield return new WaitForSeconds(0.02f);
            StartCoroutine(PlayCompletionEffect());
        }
    }
    
    private IEnumerator AnimateCorruption()
    {
        isCorrupted = true;
        
        if (progressRingImage == null) yield break;
        
        float startFill = progressRingImage.fillAmount;
        Color startColor = progressRingImage.color;
        Color glowColor = GetGlowColor(CodeType.Computer);
        
        float extendedDuration = ringFillDuration * 1.3f; // 130% of original duration
        float elapsed = 0f;
        while (elapsed < extendedDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / extendedDuration;
            
            // Progress ring animation (only animate up to 100% of original duration)
            float ringProgress = Mathf.Min(t * 1.3f, 1f); // Scale t back to 0-1 range for ring, but cap at 1
            progressRingImage.fillAmount = Mathf.Lerp(startFill, 1f, EaseOutQuart(t));
            progressRingImage.color = Color.Lerp(startColor, computerCodeColor, t);
            
            // Update glow ring
            if (glowRingImage != null)
            {
                glowRingImage.fillAmount = progressRingImage.fillAmount;
                
                // Set glow color and calculate opacity based on animation progress (t)
                Color currentGlowColor = glowColor;
                
                // Calculate glow opacity based on extended animation progress
                float glowOpacity = CalculateGlowOpacityFromProgress(t);
                currentGlowColor.a = glowOpacity;
                
                glowRingImage.color = currentGlowColor;
            }
            
            yield return null;
        }
        
        progressRingImage.fillAmount = 1f;
        progressRingImage.color = computerCodeColor;
        
        // Ensure glow ring is transparent at the end (animation complete)
        if (glowRingImage != null)
        {
            glowRingImage.fillAmount = 1f;
            Color finalGlowColor = glowColor;
            finalGlowColor.a = 0f; // Always 0 at end of animation
            glowRingImage.color = finalGlowColor;
        }
        
        UpdateProgressText();
        
        yield return new WaitForSeconds(0.02f);
        StartCoroutine(PlayCompletionEffect());
    }

    private IEnumerator PlayCompletionEffect()
    {
        // Get references to all GameObjects that need scaling
        Transform progressRingTransform = progressRingImage?.transform;
        Transform glowRingTransform = glowRingImage?.transform;
        Transform progressRingBackgroundTransform = progressRingBackgroundImage?.transform;

        // Store original scales
        Vector3 progressOriginalScale = progressRingTransform?.localScale ?? Vector3.one;
        Vector3 glowOriginalScale = glowRingTransform?.localScale ?? Vector3.one;
        Vector3 backgroundOriginalScale = progressRingBackgroundTransform?.localScale ?? Vector3.one;

        // Animation parameters
        float compressionScale = 0.9f;
        float expansionScale = 1.1f;
        float mainAnimationDuration = 1f;
        float glowExtendedDuration = mainAnimationDuration * 1.3f; // 1.3 seconds total
        float shockwaveStartTime = mainAnimationDuration * 0.75f; // When main ring reaches 1.1

        float elapsed = 0f;
        bool glowShockwaveStarted = false;

        // Store glow ring's current color for shockwave effect
        Color originalGlowColor = glowRingImage?.color ?? Color.white;
        Color shockwaveGlowColor = originalGlowColor;
        shockwaveGlowColor.a = 0.6f; // Reset to visible for shockwave

        while (elapsed < glowExtendedDuration)
        {
            elapsed += Time.deltaTime;

            // Main ring animation (only for first 1 second)
            if (elapsed <= mainAnimationDuration)
            {
                float mainT = elapsed / mainAnimationDuration;
                float easedMainT = EaseInOut(mainT);

                // Calculate main ring scale: 1.0 -> 0.9 -> 1.1 -> 1.0
                float currentMainScale;
                if (easedMainT <= 0.5f)
                {
                    // First half: compress from 1.0 to 0.9
                    currentMainScale = Mathf.Lerp(1f, compressionScale, easedMainT * 2f);
                }
                else
                {
                    // Second half: expand from 0.9 to 1.1, then back to 1.0
                    float secondHalfProgress = (easedMainT - 0.5f) * 2f;
                    if (secondHalfProgress <= 0.5f)
                    {
                        // Expand from 0.9 to 1.1
                        currentMainScale = Mathf.Lerp(compressionScale, expansionScale, secondHalfProgress * 2f);
                    }
                    else
                    {
                        // Return from 1.1 to 1.0
                        currentMainScale = Mathf.Lerp(expansionScale, 1f, (secondHalfProgress - 0.5f) * 2f);
                    }
                }

                // Apply scaling to main ring and background
                if (progressRingTransform != null)
                    progressRingTransform.localScale = progressOriginalScale * currentMainScale;

                if (progressRingBackgroundTransform != null)
                    progressRingBackgroundTransform.localScale = backgroundOriginalScale * currentMainScale;

                // Glow ring follows main ring until shockwave starts
                if (!glowShockwaveStarted)
                {
                    if (glowRingTransform != null)
                        glowRingTransform.localScale = glowOriginalScale * currentMainScale;

                    // Check if we should start the shockwave (when main ring reaches 1.1)
                    if (elapsed >= shockwaveStartTime)
                    {
                        glowShockwaveStarted = true;
                        // Set glow ring to visible for shockwave effect
                        if (glowRingImage != null)
                            glowRingImage.color = shockwaveGlowColor;
                    }
                }
            }

            // Glow ring shockwave animation (starts at 75% of main animation, runs until 130%)
            if (glowShockwaveStarted && glowRingTransform != null && glowRingImage != null)
            {
                float shockwaveElapsed = elapsed - shockwaveStartTime;
                float shockwaveDuration = glowExtendedDuration - shockwaveStartTime;
                float shockwaveT = Mathf.Clamp01(shockwaveElapsed / shockwaveDuration);

                // Scale from 1.1 to 3.0
                float shockwaveScale = Mathf.Lerp(expansionScale, 3f, shockwaveT);
                glowRingTransform.localScale = glowOriginalScale * shockwaveScale;

                // Fade from 0.6 to 0 (linear)
                float currentOpacity = Mathf.Lerp(0.6f, 0f, shockwaveT);
                Color currentGlowColor = shockwaveGlowColor;
                currentGlowColor.a = currentOpacity;
                glowRingImage.color = currentGlowColor;
            }

            yield return null;
        }

        // Ensure all elements return to their final states
        if (progressRingTransform != null)
            progressRingTransform.localScale = progressOriginalScale;

        if (progressRingBackgroundTransform != null)
            progressRingBackgroundTransform.localScale = backgroundOriginalScale;

        if (glowRingTransform != null)
        {
            glowRingTransform.localScale = glowOriginalScale;
            // Ensure glow ring is fully transparent at the end
            if (glowRingImage != null)
            {
                Color finalGlowColor = originalGlowColor;
                finalGlowColor.a = 0f;
                glowRingImage.color = finalGlowColor;
            }
        }
    }

    private float EaseInOut(float t)
    {
        // Smooth ease-in-out curve (cubic)
        return t * t * (3f - 2f * t);
    }
        
    private Color GetCodeColor(CodeType codeType)
    {
        switch (codeType)
        {
            case CodeType.Water: return waterColor;
            case CodeType.Electricity: return electricityColor;
            case CodeType.Location: return locationColor;
            case CodeType.Computer: return computerCodeColor;
            default: return Color.white;
        }
    }
    
    private Color GetGlowColor(CodeType codeType)
    {
        switch (codeType)
        {
            case CodeType.Water: return waterGlowColor;
            case CodeType.Electricity: return electricityGlowColor;
            case CodeType.Location: return locationGlowColor;
            case CodeType.Computer: return computerGlowColor;
            default: return Color.clear;
        }
    }

    private void UpdateProgressText()
    {
        if (progressCounterText == null) return;

        if (isCorrupted)
        {
            progressCounterText.text = "";
            progressCounterText.color = computerCodeColor;
        }
        else
        {
            progressCounterText.text = $"{enteredCodes} / {totalCodes}";
            progressCounterText.color = Color.white;
        }
    }
    
    private float CalculateGlowOpacityFromProgress(float animationProgress)
    {
        if (animationProgress <= 0.8f)
        {
            // From 0% to 90% of animation: opacity goes from 0 to maxGlowOpacity
            return Mathf.Lerp(0f, maxGlowOpacity, animationProgress / 0.8f);
        }
        else
        {
            // From 90% to 100% of animation: opacity goes from maxGlowOpacity to 0
            float fadeProgress = (animationProgress - 0.8f) / 0.2f; // Maps 0.9-1.0 to 0.0-1.0
            return Mathf.Lerp(maxGlowOpacity, 0f, fadeProgress);
        }
    }
    
    private float EaseOutQuart(float t)
    {
        return 1f - Mathf.Pow(1f - t, 4f);
    }
    
    // Public getters for GameHUDManager
    public bool IsCompleted => enteredCodes >= totalCodes || isCorrupted;
    public bool IsCorrupted => isCorrupted;
    public int EnteredCodesCount => enteredCodes;
    
    // Method to restore progress based on used codes from GameHUDManager
    public void RestoreProgress(HashSet<string> usedCodes, string[] allClueCodes)
    {
        ResetVisuals();
        
        string waterCode = allClueCodes[0];
        string electricityCode = allClueCodes[1]; 
        string locationCode = allClueCodes[2];
        string computerCode = allClueCodes[3];
        
        // Check if computer code was used (corruption)
        if (!string.IsNullOrEmpty(computerCode) && usedCodes.Contains(computerCode))
        {
            isCorrupted = true;
            if (progressRingImage != null)
            {
                progressRingImage.fillAmount = 1f;
                progressRingImage.color = computerCodeColor;
            }
            if (glowRingImage != null)
            {
                glowRingImage.fillAmount = 1f;
                Color glowColor = GetGlowColor(CodeType.Computer);
                glowColor.a = 0f; // Always transparent when restoring corrupted state
                glowRingImage.color = glowColor;
            }
            UpdateProgressText();
            return;
        }
        
        // Count codes and find the most recently entered one
        CodeType mostRecentCodeType = CodeType.Water; // Default
        
        if (!string.IsNullOrEmpty(waterCode) && usedCodes.Contains(waterCode))
        {
            enteredCodes++;
            mostRecentCodeType = CodeType.Water;
        }
        
        if (!string.IsNullOrEmpty(electricityCode) && usedCodes.Contains(electricityCode))
        {
            enteredCodes++;
            mostRecentCodeType = CodeType.Electricity;
        }
        
        if (!string.IsNullOrEmpty(locationCode) && usedCodes.Contains(locationCode))
        {
            enteredCodes++;
            mostRecentCodeType = CodeType.Location;
        }
        
        // Set the visual state - filled to the correct amount with the most recent code's color
        if (progressRingImage != null && enteredCodes > 0)
        {
            Color ringColor = GetCodeColor(mostRecentCodeType);
            progressRingImage.fillAmount = (float)enteredCodes / totalCodes;
            progressRingImage.color = ringColor;
            lastEnteredCodeType = mostRecentCodeType;
            
            // Set glow ring to match (keep transparent in restored state)
            if (glowRingImage != null)
            {
                glowRingImage.fillAmount = progressRingImage.fillAmount;
                Color glowColor = GetGlowColor(mostRecentCodeType);
                glowColor.a = 0f; // Always transparent when restoring progress
                glowRingImage.color = glowColor;
            }
        }
        
        UpdateProgressText();
    }
}