using UnityEngine;
using System.Collections;

public class ElectricityClueSystem : MonoBehaviour
{
    [Header("State Management")]
    private bool cableConnected = false;
    private bool clueRevealed = false;
    
    [Header("Cable Components")]
    [SerializeField] private Transform cableEnd;
    [SerializeField] private Transform connectionPoint;
    [SerializeField] private float connectionDistance = 0.1f; // How close cable needs to be to connection
    [SerializeField] private float connectionSpeed = 2f;
    
    [Header("Server Rack Components")]
    [SerializeField] private Light[] serverLights;
    [SerializeField] private float lightIntensity = 2f;
    [SerializeField] private Color lightColor = Color.green;
    
    [Header("Area Lighting")]
    [SerializeField] private Light[] areaLights;
    [SerializeField] private float areaLightIntensity = 1f;
    
    [Header("Clue Settings")]
    [SerializeField] private string electricityClueCode = "KWH-365";
    [SerializeField] private GameObject clueTextObject;
    [SerializeField] private Material emissiveMaterial;
    [SerializeField] private ClueProgressUI clueProgressUI;
    
    // References for interaction
    private PlayerInteractionManager interactionManager;
    private MeshRenderer clueTextRenderer;
    
    void Start()
    {
        // Find the interaction manager
        interactionManager = FindObjectOfType<PlayerInteractionManager>();
        
        // Get clue text renderer
        if (clueTextObject) clueTextRenderer = clueTextObject.GetComponent<MeshRenderer>();
        
        // Set initial states
        SetLightsState(false);
        
        // Hide clue text
        if (clueTextObject) clueTextObject.SetActive(false);
    }
    
    // Call this when player interacts with the cable
    public void InteractWithCable()
    {
        Debug.Log("Cable interaction triggered");
        
        if (!cableConnected)
        {
            Debug.Log("Connecting cable");
            StartCoroutine(ConnectCable());
        }
    }
    
    private IEnumerator ConnectCable()
    {
        // Temporarily disable player interaction during animation
        if (interactionManager != null) interactionManager.SetInteractionEnabled(false);
        
        float time = 0;
        Vector3 startPosition = cableEnd.position;
        Quaternion startRotation = cableEnd.rotation;
        
        // Animate the cable connecting to the connection point
        while (time < 1)
        {
            time += Time.deltaTime * connectionSpeed;
            cableEnd.position = Vector3.Lerp(startPosition, connectionPoint.position, time);
            cableEnd.rotation = Quaternion.Slerp(startRotation, connectionPoint.rotation, time);
            yield return null;
        }
        
        // Re-enable player interaction
        if (interactionManager != null) interactionManager.SetInteractionEnabled(true);
        
        // Cable is now connected
        cableConnected = true;
        
        // Power on
        PowerOn();
    }
    
    private void PowerOn()
    {
        // Turn on lights with a brief delay between them for effect
        StartCoroutine(SequentialLightUp());
    }
    
    private IEnumerator SequentialLightUp()
    {
        // Turn on area lights first
        foreach (Light light in areaLights)
        {
            if (light != null)
            {
                light.enabled = true;
                // Optional: Fade in the light
                StartCoroutine(FadeInLight(light, areaLightIntensity, 0.5f));
                yield return new WaitForSeconds(0.2f);
            }
        }
        
        yield return new WaitForSeconds(0.5f);
        
        // Then turn on server lights
        foreach (Light light in serverLights)
        {
            if (light != null)
            {
                light.enabled = true;
                light.color = lightColor;
                // Optional: Fade in the light
                StartCoroutine(FadeInLight(light, lightIntensity, 0.3f));
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        yield return new WaitForSeconds(1f);
        
        // Finally reveal the clue
        RevealClue();
    }
    
    private IEnumerator FadeInLight(Light light, float targetIntensity, float duration)
    {
        float startIntensity = 0;
        light.intensity = startIntensity;
        
        float time = 0;
        while (time < duration)
        {
            time += Time.deltaTime;
            light.intensity = Mathf.Lerp(startIntensity, targetIntensity, time / duration);
            yield return null;
        }
        
        light.intensity = targetIntensity;
    }
    
    private void SetLightsState(bool state)
    {
        // Set all server lights
        foreach (Light light in serverLights)
        {
            if (light != null)
            {
                light.enabled = state;
                if (state)
                {
                    light.color = lightColor;
                    light.intensity = lightIntensity;
                }
            }
        }
        
        // Set all area lights
        foreach (Light light in areaLights)
        {
            if (light != null)
            {
                light.enabled = state;
                if (state)
                {
                    light.intensity = areaLightIntensity;
                }
            }
        }
    }
    
    private void RevealClue()
    {
        if (clueRevealed) return;
        
        clueRevealed = true;
        
        // Show clue text
        if (clueTextObject)
        {
            clueTextObject.SetActive(true);
            
            // Apply emissive material if available
            if (clueTextRenderer != null && emissiveMaterial != null)
            {
                clueTextRenderer.material = emissiveMaterial;
            }
        }
        
        // Update progress UI
        if (clueProgressUI) clueProgressUI.SolveClue("electricity", electricityClueCode);
        
        // Log for debugging
        Debug.Log("Electricity clue revealed: " + electricityClueCode);
    }
}