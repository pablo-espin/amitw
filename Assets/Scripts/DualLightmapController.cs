using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;

public class DualLightmapController : MonoBehaviour
{
    [Header("Lightmap Sets")]
    [SerializeField] private Texture2D[] normalLightmapColors;
    [SerializeField] private Texture2D[] normalLightmapDirections;
    [SerializeField] private Texture2D[] lockdownLightmapColors;
    [SerializeField] private Texture2D[] lockdownLightmapDirections;
    
    [Header("Transition Settings")]
    [SerializeField] private float transitionDuration = 2f;
    [SerializeField] private bool useInstantSwitch = false;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    
    [Header("Manual Testing")]
    [SerializeField] private bool testSwitchToLockdown = false;
    [SerializeField] private bool testSwitchToNormal = false;
    
    private bool isInLockdown = false;
    private LightmapData[] originalLightmaps;
    
    private void Start()
    {
        // Store the original lightmaps that are currently active
        originalLightmaps = LightmapSettings.lightmaps;
    }
    
    private void Update()
    {
        // Manual testing buttons in inspector
        if (testSwitchToLockdown)
        {
            testSwitchToLockdown = false;
            SwitchToLockdownLighting();
        }
        
        if (testSwitchToNormal)
        {
            testSwitchToNormal = false;
            SwitchToNormalLighting();
        }
    }
    
    public void SwitchToLockdownLighting()
    {
        if (isInLockdown) return;
        
        if (lockdownLightmapColors == null || lockdownLightmapColors.Length == 0)
        {
            Debug.LogError("Lockdown lightmaps not stored! Please capture lockdown lightmaps first.");
            return;
        }
        
        isInLockdown = true;
        
        if (useInstantSwitch)
        {
            ApplyStoredLightmaps(lockdownLightmapColors, lockdownLightmapDirections);
        }
        else
        {
            StartCoroutine(TransitionToStoredLightmaps(lockdownLightmapColors, lockdownLightmapDirections));
        }
    }
    
    public void SwitchToNormalLighting()
    {
        if (!isInLockdown) return;
        
        if (normalLightmapColors == null || normalLightmapColors.Length == 0)
        {
            Debug.LogError("Normal lightmaps not stored! Please capture normal lightmaps first.");
            return;
        }
        
        isInLockdown = false;
        
        if (useInstantSwitch)
        {
            ApplyStoredLightmaps(normalLightmapColors, normalLightmapDirections);
        }
        else
        {
            StartCoroutine(TransitionToStoredLightmaps(normalLightmapColors, normalLightmapDirections));
        }
    }
    
    private void ApplyStoredLightmaps(Texture2D[] colorMaps, Texture2D[] directionMaps)
    {
        LightmapData[] newLightmapData = new LightmapData[colorMaps.Length];
        
        for (int i = 0; i < colorMaps.Length; i++)
        {
            newLightmapData[i] = new LightmapData();
            newLightmapData[i].lightmapColor = colorMaps[i];
            
            if (directionMaps != null && i < directionMaps.Length)
            {
                newLightmapData[i].lightmapDir = directionMaps[i];
            }
        }
        
        LightmapSettings.lightmaps = newLightmapData;
        
        if (debugMode)
            Debug.Log($"Applied {newLightmapData.Length} stored lightmaps instantly");
    }
    
    private IEnumerator TransitionToStoredLightmaps(Texture2D[] colorMaps, Texture2D[] directionMaps)
    {
        if (debugMode)
            Debug.Log($"Starting lightmap transition over {transitionDuration} seconds");
        
        // Fade out current lighting
        yield return StartCoroutine(FadeLighting(1f, 0f, transitionDuration * 0.3f));
        
        // Switch lightmaps while screen is dark
        ApplyStoredLightmaps(colorMaps, directionMaps);
        
        // Small delay to ensure lightmaps are applied
        yield return new WaitForSeconds(0.1f);
        
        // Fade in new lighting
        yield return StartCoroutine(FadeLighting(0f, 1f, transitionDuration * 0.7f));
        
        if (debugMode)
            Debug.Log("Lightmap transition complete");
    }
    
    private IEnumerator FadeLighting(float startIntensity, float endIntensity, float duration)
    {
        float elapsed = 0f;
        float originalAmbientIntensity = RenderSettings.ambientIntensity;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float currentIntensity = Mathf.Lerp(startIntensity, endIntensity, t);
            
            // Fade ambient lighting intensity
            RenderSettings.ambientIntensity = originalAmbientIntensity * currentIntensity;
            
            yield return null;
        }
        
        RenderSettings.ambientIntensity = originalAmbientIntensity * endIntensity;
    }
    
    // Helper methods to store current lightmaps as assets
    [ContextMenu("Store Current Lightmaps as Normal")]
    public void StoreCurrentAsNormal()
    {
        StoreCurrentLightmaps(ref normalLightmapColors, ref normalLightmapDirections, "Normal");
    }
    
    [ContextMenu("Store Current Lightmaps as Lockdown")]
    public void StoreCurrentAsLockdown()
    {
        StoreCurrentLightmaps(ref lockdownLightmapColors, ref lockdownLightmapDirections, "Lockdown");
    }
    
    private void StoreCurrentLightmaps(ref Texture2D[] colorArray, ref Texture2D[] directionArray, string suffix)
    {
        LightmapData[] currentLightmaps = LightmapSettings.lightmaps;
        
        if (currentLightmaps == null || currentLightmaps.Length == 0)
        {
            Debug.LogError("No lightmaps found! Make sure lighting is baked.");
            return;
        }
        
        colorArray = new Texture2D[currentLightmaps.Length];
        directionArray = new Texture2D[currentLightmaps.Length];
        
        for (int i = 0; i < currentLightmaps.Length; i++)
        {
            if (currentLightmaps[i].lightmapColor != null)
            {
                colorArray[i] = currentLightmaps[i].lightmapColor;
            }
            
            if (currentLightmaps[i].lightmapDir != null)
            {
                directionArray[i] = currentLightmaps[i].lightmapDir;
            }
        }
        
        Debug.Log($"Stored {currentLightmaps.Length} {suffix} lightmaps");
        
        #if UNITY_EDITOR
        // Mark the object as dirty so Unity saves the changes
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }
    
    // Public method for LockdownManager to call
    public void InitiateLockdownLighting()
    {
        SwitchToLockdownLighting();
    }
    
    // Debug info
    [ContextMenu("Debug Lightmap Storage")]
    public void DebugLightmapStorage()
    {
        Debug.Log($"Normal lightmaps stored: {normalLightmapColors?.Length ?? 0}");
        Debug.Log($"Lockdown lightmaps stored: {lockdownLightmapColors?.Length ?? 0}");
        
        if (normalLightmapColors != null && normalLightmapColors.Length > 0)
        {
            Debug.Log($"Normal lightmap 0: {normalLightmapColors[0]?.name ?? "null"}");
        }
        
        if (lockdownLightmapColors != null && lockdownLightmapColors.Length > 0)
        {
            Debug.Log($"Lockdown lightmap 0: {lockdownLightmapColors[0]?.name ?? "null"}");
        }
    }
}