using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class AnimatedGlowBorder : MonoBehaviour
{
    [Header("Border Configuration")]
    [SerializeField] private RectTransform borderContainer;
    [SerializeField] private Image glowDot;
    [SerializeField] private Image glowTrail;
    [SerializeField] private float animationSpeed = 1f;
    [SerializeField] private float normalSpeed = 1f;
    [SerializeField] private float excitedSpeed = 2.5f;
    [SerializeField] private float excitedDuration = 1f;
    
    [Header("Visual Properties")]
    [SerializeField] private Color normalGlowColor = new Color(0.2f, 0.4f, 0.8f, 1f); // Dark blue
    [SerializeField] private Color excitedGlowColor = new Color(0.4f, 0.7f, 1f, 1f); // Brighter blue
    [SerializeField] private float dotSize = 10f;
    [SerializeField] private float trailLength = 0.15f; // Percentage of border
    [SerializeField] private AnimationCurve trailFadeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    
    [Header("Trail Effect")]
    [SerializeField] private int trailSegments = 20;
    [SerializeField] private float trailWidth = 3f;
    
    private float currentProgress = 0f;
    private bool isExcited = false;
    private Vector2[] borderPoints;
    private Image[] trailImages;
    private Coroutine excitementCoroutine;
    
    private void Awake()
    {
        InitializeBorder();
    }
    
    private void Start()
    {
        animationSpeed = normalSpeed;
    }
    
    private void InitializeBorder()
    {
        if (borderContainer == null)
        {
            Debug.LogError("Border container not assigned!");
            return;
        }
        
        CalculateBorderPoints();
        SetupGlowDot();
        SetupTrailEffect();
    }
    
    private void CalculateBorderPoints()
    {
        if (borderContainer == null) return;
        
        Rect rect = borderContainer.rect;
        float width = rect.width;
        float height = rect.height;
        
        // Calculate perimeter points (clockwise from top-left)
        int pointsPerSide = 50; // Smooth curve
        borderPoints = new Vector2[pointsPerSide * 4];
        
        int index = 0;
        
        // Top side (left to right)
        for (int i = 0; i < pointsPerSide; i++)
        {
            float t = (float)i / (pointsPerSide - 1);
            borderPoints[index++] = new Vector2(Mathf.Lerp(-width/2, width/2, t), height/2);
        }
        
        // Right side (top to bottom)
        for (int i = 0; i < pointsPerSide; i++)
        {
            float t = (float)i / (pointsPerSide - 1);
            borderPoints[index++] = new Vector2(width/2, Mathf.Lerp(height/2, -height/2, t));
        }
        
        // Bottom side (right to left)
        for (int i = 0; i < pointsPerSide; i++)
        {
            float t = (float)i / (pointsPerSide - 1);
            borderPoints[index++] = new Vector2(Mathf.Lerp(width/2, -width/2, t), -height/2);
        }
        
        // Left side (bottom to top)
        for (int i = 0; i < pointsPerSide; i++)
        {
            float t = (float)i / (pointsPerSide - 1);
            borderPoints[index++] = new Vector2(-width/2, Mathf.Lerp(-height/2, height/2, t));
        }
    }
    
    private void SetupGlowDot()
    {
        if (glowDot == null)
        {
            // Create glow dot if not assigned
            GameObject dotObj = new GameObject("GlowDot");
            dotObj.transform.SetParent(borderContainer);
            glowDot = dotObj.AddComponent<Image>();
        }
        
        glowDot.color = normalGlowColor;
        glowDot.rectTransform.sizeDelta = Vector2.one * dotSize;
        glowDot.rectTransform.anchorMin = Vector2.one * 0.5f;
        glowDot.rectTransform.anchorMax = Vector2.one * 0.5f;
    }
    
    private void SetupTrailEffect()
    {
        // Create trail segments
        trailImages = new Image[trailSegments];
        
        for (int i = 0; i < trailSegments; i++)
        {
            GameObject trailSegment = new GameObject($"TrailSegment_{i}");
            trailSegment.transform.SetParent(borderContainer);
            
            Image segmentImage = trailSegment.AddComponent<Image>();
            segmentImage.color = normalGlowColor;
            segmentImage.rectTransform.sizeDelta = Vector2.one * trailWidth;
            segmentImage.rectTransform.anchorMin = Vector2.one * 0.5f;
            segmentImage.rectTransform.anchorMax = Vector2.one * 0.5f;
            
            trailImages[i] = segmentImage;
        }
    }
    
    private void Update()
    {
        AnimateBorder();
    }
    
    private void AnimateBorder()
    {
        if (borderPoints == null || borderPoints.Length == 0) return;
        
        // Update progress
        currentProgress += Time.deltaTime * animationSpeed;
        if (currentProgress >= 1f)
        {
            currentProgress -= 1f;
        }
        
        // Update glow dot position
        UpdateGlowDotPosition();
        
        // Update trail effect
        UpdateTrailEffect();
    }
    
    private void UpdateGlowDotPosition()
    {
        if (glowDot == null || borderPoints == null) return;
        
        Vector2 position = GetPositionOnBorder(currentProgress);
        glowDot.rectTransform.anchoredPosition = position;
    }
    
    private void UpdateTrailEffect()
    {
        if (trailImages == null) return;
        
        for (int i = 0; i < trailImages.Length; i++)
        {
            float trailProgress = currentProgress - (trailLength * (float)(i + 1) / trailSegments);
            if (trailProgress < 0f) trailProgress += 1f;
            
            Vector2 position = GetPositionOnBorder(trailProgress);
            trailImages[i].rectTransform.anchoredPosition = position;
            
            // Fade trail based on distance from dot
            float fadeAmount = trailFadeCurve.Evaluate((float)i / trailSegments);
            Color trailColor = isExcited ? excitedGlowColor : normalGlowColor;
            trailColor.a *= fadeAmount;
            trailImages[i].color = trailColor;
        }
    }
    
    private Vector2 GetPositionOnBorder(float progress)
    {
        if (borderPoints == null || borderPoints.Length == 0) return Vector2.zero;
        
        progress = Mathf.Clamp01(progress);
        float exactIndex = progress * (borderPoints.Length - 1);
        int index1 = Mathf.FloorToInt(exactIndex);
        int index2 = (index1 + 1) % borderPoints.Length;
        float t = exactIndex - index1;
        
        return Vector2.Lerp(borderPoints[index1], borderPoints[index2], t);
    }
    
    public void TriggerExcitement()
    {
        if (excitementCoroutine != null)
        {
            StopCoroutine(excitementCoroutine);
        }
        
        excitementCoroutine = StartCoroutine(ExcitementEffect());
    }
    
    private IEnumerator ExcitementEffect()
    {
        isExcited = true;
        animationSpeed = excitedSpeed;
        
        // Update colors
        if (glowDot != null)
            glowDot.color = excitedGlowColor;
        
        yield return new WaitForSeconds(excitedDuration);
        
        // Return to normal
        isExcited = false;
        animationSpeed = normalSpeed;
        
        if (glowDot != null)
            glowDot.color = normalGlowColor;
        
        excitementCoroutine = null;
    }
    
    public void SetBorderSpeed(float speed)
    {
        normalSpeed = speed;
        if (!isExcited)
        {
            animationSpeed = normalSpeed;
        }
    }
    
    public void SetGlowColor(Color color)
    {
        normalGlowColor = color;
        if (!isExcited && glowDot != null)
        {
            glowDot.color = color;
        }
    }
}