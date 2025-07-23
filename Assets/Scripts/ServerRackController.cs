using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ServerRackController : MonoBehaviour
{
    [Header("State")]
    [SerializeField] private bool isEmergencyMode = false;
    
    [Header("Normal State Colors")]
    [SerializeField] private Color normalPowerColor = Color.green;
    [SerializeField] private Color normalHDDColor = new Color(0, 0.5f, 1f);
    [SerializeField] private Color normalNetworkColor = new Color(1f, 0.8f, 0f);
    
    [Header("Emergency State Colors")]
    [SerializeField] private Color emergencyPowerColor = Color.red;
    [SerializeField] private float emergencyPowerIntensity = 3f;
    [SerializeField] private float emergencyPulseSpeed = 2f; // Faster pulse in emergency
    
    [Header("Transition Settings")]
    [SerializeField] private float transitionDuration = 2f;
    [SerializeField] private bool cascadeEffect = true;
    [SerializeField] private float cascadeDelay = 0.05f;
    
    // Cache renderer and material property block
    private Renderer rackRenderer;
    private MaterialPropertyBlock propBlock;
    private static List<ServerRackController> allRackControllers = new List<ServerRackController>();

    // Store original values
    private Texture originalBaseMap;
    private Color originalBaseColor;
    private Texture originalMetallicGlossMap;
    private float originalMetallic;
    private float originalSmoothness;
    private Texture originalBumpMap;
    private float originalBumpScale;
    private Color originalPowerColor;
    private float originalPowerIntensity;
    private float originalPowerPulseSpeed;
    private Color originalHDDColor;
    private float originalHDDBaseIntensity;
    private float originalHDDIntensity;
    private Color originalNetworkColor;
    private float originalNetworkBaseIntensity;
    private float originalNetworkIntensity;
    private float originalNetworkFlickerSpeed;


    
    private void Awake()
    {
        // Register this controller
        if (!allRackControllers.Contains(this))
            allRackControllers.Add(this);
    }
    
    private void OnDestroy()
    {
        // Unregister when destroyed
        allRackControllers.Remove(this);
    }
    
    private void Start()
    {
        // Get renderer and create property block
        rackRenderer = GetComponent<Renderer>();
        
        // For static batched objects, renderer might be on a child
        if (rackRenderer == null)
        {
            rackRenderer = GetComponentInChildren<Renderer>();
        }
        
        if (rackRenderer == null)
        {
            Debug.LogError("No Renderer found on " + gameObject.name);
            return;
        }
        
        propBlock = new MaterialPropertyBlock();
        
        // Store original values from the material
        Material mat = rackRenderer.sharedMaterial;
        if (mat != null)
        {
            // Base
            originalBaseMap = mat.GetTexture("_BaseMap");
            originalBaseColor = mat.GetColor("_BaseColor");

            // Metallic and Smoothness
            originalMetallicGlossMap = mat.GetTexture("_MetallicGlossMap");
            originalMetallic = mat.GetFloat("_Metallic");
            originalSmoothness = mat.GetFloat("_Smoothness");

            // Normal
            originalBumpMap = mat.GetTexture("_BumpMap");
            originalBumpScale = mat.GetFloat("_BumpScale");

            // Power
            originalPowerColor = mat.GetColor("_PowerLightColor");
            originalPowerIntensity = mat.GetFloat("_PowerLightIntensity");
            originalPowerPulseSpeed = mat.GetFloat("_PowerPulseSpeed");

            // HDD
            originalHDDColor = mat.GetColor("_HDDLightColor");
            originalHDDBaseIntensity = mat.GetFloat("_HDDBaseIntensity");
            originalHDDIntensity = mat.GetFloat("_HDDActiveIntensity");

            // Network
            originalNetworkColor = mat.GetColor("_NetworkLightColor");
            originalNetworkBaseIntensity = mat.GetFloat("_NetworkBaseIntensity");
            originalNetworkIntensity = mat.GetFloat("_NetworkActiveIntensity");
            originalNetworkFlickerSpeed = mat.GetFloat("_NetworkFlickerSpeed");

        }
    }
    
    // Called by individual rack to switch states
    public void SetEmergencyMode(bool emergency, float delay = 0f)
    {
        if (delay > 0)
        {
            StartCoroutine(SetEmergencyModeDelayed(emergency, delay));
        }
        else
        {
            StartCoroutine(TransitionToEmergencyMode(emergency));
        }
    }
    
    private IEnumerator SetEmergencyModeDelayed(bool emergency, float delay)
    {
        yield return new WaitForSeconds(delay);
        StartCoroutine(TransitionToEmergencyMode(emergency));
    }
    
    private IEnumerator TransitionToEmergencyMode(bool emergency)
    {
        isEmergencyMode = emergency;
        
        if (rackRenderer == null || propBlock == null) yield break;
        
        float elapsedTime = 0f;
                
        // Starting values
        Color startPowerColor = emergency ? normalPowerColor : emergencyPowerColor;
        Color targetPowerColor = emergency ? emergencyPowerColor : normalPowerColor;
        
        float startNetworkIntensity = emergency ? originalNetworkIntensity : 0f;
        float targetNetworkIntensity = emergency ? 0f : originalNetworkIntensity;
        
        float startHDDIntensity = emergency ? originalHDDIntensity : 0f;
        float targetHDDIntensity = emergency ? 0f : originalHDDIntensity;
        
        float startPulseSpeed = emergency ? originalPowerPulseSpeed : emergencyPulseSpeed;
        float targetPulseSpeed = emergency ? emergencyPulseSpeed : originalPowerPulseSpeed;
        
        // Transition
        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / transitionDuration;
            
            // Smooth curve
            t = Mathf.SmoothStep(0, 1, t);
            
            // Interpolate values
            propBlock.SetColor("_PowerLightColor", Color.Lerp(startPowerColor, targetPowerColor, t));
            propBlock.SetFloat("_PowerPulseSpeed", Mathf.Lerp(startPulseSpeed, targetPulseSpeed, t));
            
            // For network and HDD, fade out quickly in emergency
            if (emergency)
            {
                float fadeOut = 1f - t;
                propBlock.SetFloat("_Network1Intensity", originalNetworkIntensity * fadeOut);
                propBlock.SetFloat("_Network2Intensity", originalNetworkIntensity * fadeOut);
                propBlock.SetFloat("_HDD1Intensity", originalHDDIntensity * fadeOut);
                propBlock.SetFloat("_HDD2Intensity", originalHDDIntensity * fadeOut);
            }
            else
            {
                // Fade back in
                propBlock.SetFloat("_Network1Intensity", originalNetworkIntensity * t);
                propBlock.SetFloat("_Network2Intensity", originalNetworkIntensity * t);
                propBlock.SetFloat("_HDD1Intensity", originalHDDIntensity * t);
                propBlock.SetFloat("_HDD2Intensity", originalHDDIntensity * t);
            }
            
            // Apply changes
            rackRenderer.SetPropertyBlock(propBlock);
            
            yield return null;
        }
        
        // Ensure final values
        propBlock.SetColor("_PowerLightColor", targetPowerColor);
        propBlock.SetFloat("_PowerPulseSpeed", targetPulseSpeed);
        propBlock.SetTexture("_BaseMap", originalBaseMap);
        propBlock.SetColor("_BaseColor", originalBaseColor);
        propBlock.SetTexture("_MetallicGlossMap", originalMetallicGlossMap);
        propBlock.SetFloat("_Metallic", originalMetallic);
        propBlock.SetFloat("_Smoothness", originalSmoothness);
        propBlock.SetTexture("_BumpMap", originalBumpMap);
        propBlock.SetFloat("_BumpScale", originalBumpScale);
        
        if (emergency)
        {
            propBlock.SetTexture("_BaseMap", originalBaseMap);
            propBlock.SetColor("_BaseColor", originalBaseColor);
            propBlock.SetTexture("_MetallicGlossMap", originalMetallicGlossMap);
            propBlock.SetFloat("_Metallic", originalMetallic);
            propBlock.SetFloat("_Smoothness", originalSmoothness);
            propBlock.SetTexture("_BumpMap", originalBumpMap);
            propBlock.SetFloat("_BumpScale", originalBumpScale);
            propBlock.SetFloat("_Network1Intensity", 0);
            propBlock.SetFloat("_Network2Intensity", 0);
            propBlock.SetFloat("_HDD1Intensity", 0);
            propBlock.SetFloat("_HDD2Intensity", 0);
            propBlock.SetFloat("_PowerLightIntensity", emergencyPowerIntensity);
        }
        else
        {
            propBlock.SetFloat("_Network1Intensity", originalNetworkIntensity);
            propBlock.SetFloat("_Network2Intensity", originalNetworkIntensity);
            propBlock.SetFloat("_HDD1Intensity", originalHDDIntensity);
            propBlock.SetFloat("_HDD2Intensity", originalHDDIntensity);
        }
        
        rackRenderer.SetPropertyBlock(propBlock);
    }
    
    // Static methods to control all racks at once
    public static void SetAllRacksEmergencyMode(bool emergency, bool cascade = true, float cascadeMultiplier = 0.1f)
    {
        if (cascade)
        {
            // Create a cascade effect starting from the center
            float maxDelay = 0f;
            Vector3 center = GetCenterPosition();
            
            foreach (var controller in allRackControllers)
            {
                if (controller != null)
                {
                    float distance = Vector3.Distance(controller.transform.position, center);
                    float delay = distance * cascadeMultiplier; // Use configurable multiplier
                    controller.SetEmergencyMode(emergency, delay);
                    maxDelay = Mathf.Max(maxDelay, delay);
                }
            }
            
            Debug.Log($"Server rack emergency mode cascade started. Will complete in {maxDelay:F1} seconds");
        }
        else
        {
            // All at once
            foreach (var controller in allRackControllers)
            {
                if (controller != null)
                {
                    controller.SetEmergencyMode(emergency);
                }
            }
        }
    }
    
    private static Vector3 GetCenterPosition()
    {
        if (allRackControllers.Count == 0) return Vector3.zero;
        
        Vector3 sum = Vector3.zero;
        int count = 0;
        
        foreach (var controller in allRackControllers)
        {
            if (controller != null)
            {
                sum += controller.transform.position;
                count++;
            }
        }
        
        return count > 0 ? sum / count : Vector3.zero;
    }
    
    // Get count of all racks
    public static int GetRackCount()
    {
        return allRackControllers.Count;
    }
}