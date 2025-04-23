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
    [SerializeField] private Animator cableAnimator;
    [SerializeField] private string connectAnimationTrigger = "Connect";
        
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

            // Play cable connection sound
            if (InteractionSoundManager.Instance != null)
            {
                InteractionSoundManager.Instance.PlayCableConnection();
            }

            // Trigger the animation
            if (cableAnimator != null)
            {
                cableAnimator.SetTrigger(connectAnimationTrigger);
                // Wait for animation to finish before powering on
                StartCoroutine(WaitForAnimationAndPowerOn());
            }
            else
            {
                // Fallback if animator is missing
                cableConnected = true;
                PowerOn();
            }
        }
    }

    private IEnumerator WaitForAnimationAndPowerOn() 
    {
        // Temporarily disable player interaction during animation
        if (interactionManager != null) interactionManager.SetInteractionEnabled(false);
        
        // Wait for animation to finish
        AnimatorStateInfo info = cableAnimator.GetCurrentAnimatorStateInfo(0);
        yield return new WaitForSeconds(info.length + 0.1f); // Add a small buffer
        
        // Disable animator to prevent further changes
        cableAnimator.enabled = false;
                
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
            
            // Get the text renderer
            TMPro.TextMeshPro textMesh = clueTextObject.GetComponent<TMPro.TextMeshPro>();
            if (textMesh != null)
            {
                // Get the current material and modify its emission
                Material textMaterial = textMesh.fontMaterial;
                textMaterial.EnableKeyword("_EMISSION");
                textMaterial.SetColor("_EmissionColor", Color.cyan * 2.0f); // Adjust color and intensity
            }
        }
        
        // Update progress UI
        if (clueProgressUI) clueProgressUI.SolveClue("electricity", electricityClueCode);
        
        // Log for debugging
        Debug.Log("Electricity clue revealed: " + electricityClueCode);
    }
}