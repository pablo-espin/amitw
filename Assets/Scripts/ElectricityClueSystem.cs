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
    [SerializeField] private float cableAnimationSpeed = 2f;
    [SerializeField] private AnimationCurve cableMovementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Spark Effect")]
    [SerializeField] private ParticleSystem sparkEffect; // Spark particle system
        
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

    // Cable animation positions (set from inspector or code)
    [Header("Cable Animation Settings")]
    [SerializeField] private bool useLocalCoordinates = true;
    [SerializeField] private Vector3 cableStartPosition = new Vector3(0.4295037f, -18.81537f, -14.72518f);
    [SerializeField] private Vector3 cableEndPosition = new Vector3(0.159f, -18.5294f, -14.8485f);
    [SerializeField] private Vector3 cableRotation = new Vector3(-89.98f, 90f, 0f);
    [SerializeField] private Vector3 cableScale = new Vector3(100f, 119.91f, 96.08f);

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
        
        // Setup spark effect
        if (sparkEffect != null)
        {
            // Make sure sparks don't play automatically
            sparkEffect.Stop();
            sparkEffect.Clear();
            Debug.Log("Spark effect initialized and stopped");
        }
        else
        {
            Debug.LogWarning("Spark effect not assigned! Create a Particle System and assign it for cable connection sparks.");
        }
        
        // IMPORTANT: Only set cable position if it's not already at the starting position
        // This prevents triggering any existing Animator components
        if (cableEnd != null)
        {
            // Check if cable is already at or near the starting position
            float distanceFromStart = Vector3.Distance(cableEnd.position, cableStartPosition);
            
            if (distanceFromStart > 0.1f) // Only move if it's not already there
            {
                Debug.Log($"Cable is {distanceFromStart} units from start position, moving to start");
                cableEnd.position = cableStartPosition;
                cableEnd.rotation = Quaternion.Euler(cableRotation);
                cableEnd.localScale = cableScale;
            }
            else
            {
                Debug.Log($"Cable already at starting position (distance: {distanceFromStart})");
            }
            
        }
        else
        {
            Debug.LogError("CableEnd transform not assigned!");
        }
    }
    
    // Call this when player interacts with the cable
    public void InteractWithCable()
    {
        Debug.Log($"Cable interaction triggered. Current state - Connected: {cableConnected}");
        
        if (!cableConnected)
        {
            Debug.Log("Connecting cable - starting animation");

            // Play cable connection sound
            if (InteractionSoundManager.Instance != null)
            {
                InteractionSoundManager.Instance.PlayCableConnection();
            }

            // Start the cable animation
            StartCoroutine(AnimateCableConnection());
        }
        else
        {
            Debug.Log("Cable already connected - no animation needed");
        }
    }

    private IEnumerator AnimateCableConnection() 
    {
        if (cableEnd == null)
        {
            Debug.LogError("CableEnd is null - cannot animate!");
            yield break;
        }
        
        // Temporarily disable player interaction during animation
        if (interactionManager != null) 
            interactionManager.SetInteractionEnabled(false);
        
        Debug.Log($"Starting cable animation from {cableStartPosition} to {cableEndPosition}");
        
        float animationDuration = 1f / cableAnimationSpeed;
        float elapsed = 0f;
        
        // Store starting position based on coordinate system
        Vector3 startPos = useLocalCoordinates ? cableEnd.localPosition : cableEnd.position;
        Vector3 targetPos = useLocalCoordinates ? cableEndPosition : cableEndPosition;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / animationDuration;
            
            // Use animation curve for smooth movement
            float curveValue = cableMovementCurve.Evaluate(normalizedTime);
            
            // Interpolate position using the curve
            Vector3 currentPosition = Vector3.Lerp(startPos, targetPos, curveValue);
            
            if (useLocalCoordinates)
            {
                cableEnd.localPosition = currentPosition;
                cableEnd.localRotation = Quaternion.Euler(cableRotation);
            }
            else
            {
                cableEnd.position = currentPosition;
                cableEnd.rotation = Quaternion.Euler(cableRotation);
            }
            
            cableEnd.localScale = cableScale;
            
            yield return null;
        }
        
        // Ensure final position is exactly the target
        if (useLocalCoordinates)
        {
            cableEnd.localPosition = cableEndPosition;
            cableEnd.localRotation = Quaternion.Euler(cableRotation);
        }
        else
        {
            cableEnd.position = cableEndPosition;
            cableEnd.rotation = Quaternion.Euler(cableRotation);
        }
        
        cableEnd.localScale = cableScale;
        
        Debug.Log($"Cable animation complete. Final position: {(useLocalCoordinates ? cableEnd.localPosition : cableEnd.position)}");
        
        // Trigger spark effect when cable connects
        TriggerSparkEffect();
        
        // Re-enable player interaction
        if (interactionManager != null) 
            interactionManager.SetInteractionEnabled(true);
        
        // Cable is now connected
        cableConnected = true;
        
        // Power on (with a slight delay to let sparks show)
        StartCoroutine(PowerOnAfterSparks());
    }
    
    private void TriggerSparkEffect()
    {
        if (sparkEffect != null)
        {            
            // Play the spark effect
            sparkEffect.Play();
            Debug.Log("Spark effect triggered at cable connection!");
        }
        else
        {
            Debug.LogWarning("Cannot trigger sparks - Spark effect not assigned!");
        }
    }
    
    private IEnumerator PowerOnAfterSparks()
    {
        // Wait a moment for sparks to be visible
        yield return new WaitForSeconds(0.3f);
        
        // Then power on the lights
        PowerOn();
    }

    private void PowerOn()
    {
        // Trigger dialogue for electricity connection
        if (GameInteractionDialogueManager.Instance != null)
        {
            GameInteractionDialogueManager.Instance.OnElectricityConnected();
        }

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