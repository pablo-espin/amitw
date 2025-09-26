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

    [Header("Lightbulb Models")]
    [SerializeField] private MeshRenderer[] lightbulbRenderers; // 3D lightbulb models
    [SerializeField] private Material[] lightbulbOnMaterials;   // Materials for "on" state
    [SerializeField] private Material[] lightbulbOffMaterials;  // Materials for "off" state
    
    [Header("Server Rack Integration")]
    [SerializeField] private ServerRackMaterialController[] serversToActivate; // Specific servers to power on
    [SerializeField] private bool useServerRackSystem = true; // Toggle server rack integration
    [SerializeField] private bool debugServerSystem = true; // Debug server system
    [SerializeField] private float serverActivationDelay = 0.1f; // Delay between server activations
    
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
    private bool lightbulbsRegisteredWithLockdown = false;
    private bool areaLightsCurrentlyOn = false;
    
    void Start()
    {
        // Find the interaction manager
        interactionManager = FindObjectOfType<PlayerInteractionManager>();
        
        // Get clue text renderer
        if (clueTextObject) clueTextRenderer = clueTextObject.GetComponent<MeshRenderer>();
        
        // Set initial states
        SetLightsState(false);

        // Initialize lightbulbs to "off" state
        InitializeLightbulbs();
        
        // Hide clue text
        if (clueTextObject) clueTextObject.SetActive(false);
        
        // Subscribe to lockdown events
        SubscribeToLockdownEvents();

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
        
        // Debug server rack system
        if (useServerRackSystem && debugServerSystem)
        {
            Debug.Log($"ElectricityClueSystem: Server rack integration enabled");
            Debug.Log($"Servers to activate: {(serversToActivate != null ? serversToActivate.Length : 0)}");
            
            if (serversToActivate != null && serversToActivate.Length > 0)
            {
                Debug.Log("Registered servers:");
                for (int i = 0; i < serversToActivate.Length; i++)
                {
                    var server = serversToActivate[i];
                    if (server != null)
                    {
                        Debug.Log($"  [{i}] {server.gameObject.name} - Current State: {server.GetCurrentState()}");
                    }
                    else
                    {
                        Debug.LogWarning($"  [{i}] NULL SERVER REFERENCE");
                    }
                }
            }
            else
            {
                Debug.LogWarning("No servers registered for activation!");
            }
        }
    }
    
    private void SubscribeToLockdownEvents()
    {
        LockdownManager lockdownManager = FindObjectOfType<LockdownManager>();
        if (lockdownManager != null)
        {
            // Subscribe to lockdown phase changes
            lockdownManager.OnLockdownPhaseChanged += HandleLockdownPhaseChange;
            Debug.Log("ElectricityClueSystem subscribed to lockdown events");
        }
        else
        {
            Debug.LogWarning("LockdownManager not found - cannot subscribe to lockdown events");
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        LockdownManager lockdownManager = FindObjectOfType<LockdownManager>();
        if (lockdownManager != null)
        {
            lockdownManager.OnLockdownPhaseChanged -= HandleLockdownPhaseChange;
        }
    }
    
    private void HandleLockdownPhaseChange(LockdownManager.LockdownPhase newPhase)
    {
        Debug.Log($"ElectricityClueSystem received lockdown phase change: {newPhase}");
        
        if (newPhase != LockdownManager.LockdownPhase.Normal && areaLightsCurrentlyOn)
        {
            // Lockdown started and our lights are on - turn them off
            Debug.Log("Lockdown detected - turning off electricity area lights");
            TurnOffAreaLights();
        }
        else if (newPhase == LockdownManager.LockdownPhase.Normal && cableConnected)
        {
            // Lockdown ended and cable is connected - turn lights back on
            Debug.Log("Lockdown ended - turning electricity area lights back on");
            TurnOnAreaLights();
        }
    }
    
    private void TurnOffAreaLights()
    {
        for (int i = 0; i < areaLights.Length; i++)
        {
            if (areaLights[i] != null)
            {
                areaLights[i].enabled = false;
                Debug.Log($"Turned off area light [{i}] {areaLights[i].gameObject.name} due to lockdown");
            }
        }
        areaLightsCurrentlyOn = false;
    }
    
    private void TurnOnAreaLights()
    {
        for (int i = 0; i < areaLights.Length; i++)
        {
            if (areaLights[i] != null)
            {
                areaLights[i].enabled = true;
                areaLights[i].intensity = areaLightIntensity;
                Debug.Log($"Turned on area light [{i}] {areaLights[i].gameObject.name}");
            }
        }
        areaLightsCurrentlyOn = true;
    }

    private void InitializeLightbulbs()
    {
        // Validate lightbulb arrays
        if (!ValidateLightbulbArrays())
        {
            Debug.LogError("Lightbulb array validation failed! Check inspector assignments.");
            return;
        }

        // Set all lightbulbs to "off" state initially using the shared off material
        Material offMaterial = lightbulbOffMaterials[0]; // Use first (and likely only) off material

        for (int i = 0; i < lightbulbRenderers.Length; i++)
        {
            if (lightbulbRenderers[i] != null && offMaterial != null)
            {
                lightbulbRenderers[i].material = offMaterial;
                Debug.Log($"Initialized lightbulb [{i}] {lightbulbRenderers[i].gameObject.name} to OFF state");
            }
        }

        Debug.Log($"Initialized {lightbulbRenderers.Length} lightbulbs to OFF state using shared material");
    }
    
    private bool ValidateLightbulbArrays()
    {
        if (lightbulbRenderers == null || lightbulbOnMaterials == null || lightbulbOffMaterials == null)
        {
            Debug.LogWarning("Lightbulb arrays not assigned - lightbulb material swapping will be disabled");
            return false;
        }
        
        // Check that we have at least one material of each type
        if (lightbulbOnMaterials.Length == 0 || lightbulbOffMaterials.Length == 0)
        {
            Debug.LogError($"Missing materials! On Materials: {lightbulbOnMaterials.Length}, Off Materials: {lightbulbOffMaterials.Length}");
            return false;
        }
        
        // Check for null material references
        if (lightbulbOnMaterials[0] == null)
        {
            Debug.LogError("Lightbulb ON material is null!");
            return false;
        }
        
        if (lightbulbOffMaterials[0] == null)
        {
            Debug.LogError("Lightbulb OFF material is null!");
            return false;
        }
        
        // Check for null renderer references
        for (int i = 0; i < lightbulbRenderers.Length; i++)
        {
            if (lightbulbRenderers[i] == null)
            {
                Debug.LogWarning($"Lightbulb renderer [{i}] is null!");
            }
        }
        
        Debug.Log($"Lightbulb validation passed: {lightbulbRenderers.Length} renderers, shared materials (ON: {lightbulbOnMaterials[0].name}, OFF: {lightbulbOffMaterials[0].name})");
        return true;
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
    
    // Public method for ElectricityInteractable to check if cable is connected
    public bool IsCableConnected()
    {
        return cableConnected;
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
        Debug.Log("PowerOn() called - starting power sequence");

        // Notify stats system of electricity connection
        if (StatsSystem.Instance != null)
        {
            StatsSystem.Instance.OnElectricityConnected();
            Debug.Log("Notified StatsSystem of electricity connection");
        }

        // Trigger dialogue for electricity connection
        if (GameInteractionDialogueManager.Instance != null)
        {
            GameInteractionDialogueManager.Instance.OnElectricityConnected();
        }

        // Check if lockdown is active to determine behavior
        LockdownManager lockdownManager = FindObjectOfType<LockdownManager>();
        bool isLockdownActive = lockdownManager != null && lockdownManager.GetCurrentPhase() != LockdownManager.LockdownPhase.Normal;

        if (isLockdownActive)
        {
            Debug.Log("Lockdown detected - initiating momentary power-on sequence");
            StartCoroutine(PostLockdownPowerSequence());
            // Note: PostLockdownPowerSequence() handles its own server activation
        }
        else
        {
            Debug.Log("Normal phase - initiating standard power-on sequence");
            // Turn on lights and lightbulbs with synchronized timing
            StartCoroutine(SequentialLightUp());

            // Turn on registered servers (only during normal phase)
            if (useServerRackSystem)
            {
                if (debugServerSystem)
                {
                    Debug.Log($"Attempting to power on {(serversToActivate != null ? serversToActivate.Length : 0)} registered servers");
                }

                if (serversToActivate != null && serversToActivate.Length > 0)
                {
                    StartCoroutine(ActivateRegisteredServers());
                }
                else
                {
                    Debug.LogWarning("No servers registered for activation in ElectricityClueSystem!");
                }
            }
            else
            {
                Debug.Log("Server rack system disabled in ElectricityClueSystem");
            }

            // Register lightbulbs with lockdown manager for future lockdown events
            RegisterLightbulbsWithLockdown();
        }
    }
    
    private void RegisterLightbulbsWithLockdown()
    {
        if (lightbulbsRegisteredWithLockdown || lightbulbRenderers == null || lightbulbRenderers.Length == 0)
            return;
            
        // Find lockdown manager
        LockdownManager lockdownManager = FindObjectOfType<LockdownManager>();
        if (lockdownManager != null)
        {
            // Create arrays for lockdown registration - each lightbulb uses the same shared materials
            Material[] onMaterials = new Material[lightbulbRenderers.Length];
            Material[] offMaterials = new Material[lightbulbRenderers.Length];
            
            Material sharedOnMaterial = lightbulbOnMaterials[0];
            Material sharedOffMaterial = lightbulbOffMaterials[0];
            
            for (int i = 0; i < lightbulbRenderers.Length; i++)
            {
                onMaterials[i] = sharedOnMaterial;
                offMaterials[i] = sharedOffMaterial;
            }
            
            // Add our lightbulbs to the lockdown system
            lockdownManager.AddCeilingLights(lightbulbRenderers, onMaterials, offMaterials);
            lightbulbsRegisteredWithLockdown = true;
            Debug.Log($"Registered {lightbulbRenderers.Length} electricity-area lightbulbs with LockdownManager using shared materials");
            
            // If lockdown is already active, immediately apply lockdown state to our newly registered lightbulbs
            if (lockdownManager.GetCurrentPhase() != LockdownManager.LockdownPhase.Normal)
            {
                Debug.Log("Lockdown already active - applying lockdown state to newly connected lightbulbs");
                for (int i = 0; i < lightbulbRenderers.Length; i++)
                {
                    if (lightbulbRenderers[i] != null)
                    {
                        lightbulbRenderers[i].material = sharedOffMaterial;
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("LockdownManager not found - lightbulbs will not participate in lockdown events");
        }
    }   

    private IEnumerator ActivateRegisteredServers()
    {
        Debug.Log($"Starting server activation sequence for {serversToActivate.Length} servers");

        int activatedCount = 0;
        for (int i = 0; i < serversToActivate.Length; i++)
        {
            var server = serversToActivate[i];
            if (server != null)
            {
                if (debugServerSystem)
                {
                    Debug.Log($"Activating server [{i}] {server.gameObject.name} - Current state: {server.GetCurrentState()}");
                }

                // Power on the server (change from PoweredOff to Normal)
                server.SetState(ServerRackMaterialController.ServerState.Normal);
                activatedCount++;

                // Small delay between activations for visual effect
                if (serverActivationDelay > 0 && i < serversToActivate.Length - 1)
                {
                    yield return new WaitForSeconds(serverActivationDelay);
                }
            }
            else
            {
                Debug.LogWarning($"Server [{i}] is null! Please assign all server references in the inspector.");
            }
        }

        Debug.Log($"Server activation complete! Activated {activatedCount} out of {serversToActivate.Length} servers");

        if (debugServerSystem)
        {
            // Show updated states after a brief delay
            yield return new WaitForSeconds(0.5f);
            Debug.Log("=== Server states after activation ===");
            for (int i = 0; i < serversToActivate.Length; i++)
            {
                var server = serversToActivate[i];
                if (server != null)
                {
                    Debug.Log($"  {server.gameObject.name}: {server.GetCurrentState()}");
                }
            }
        }
    }
    
    private IEnumerator SequentialLightUp()
    {
        Debug.Log("Starting sequential light activation");
        
        // Validate that we have valid lightbulbs
        bool hasValidLightbulbs = ValidateLightbulbArrays();
        int lightCount = areaLights?.Length ?? 0;
        int lightbulbCount = lightbulbRenderers?.Length ?? 0;
        
        if (hasValidLightbulbs && lightCount != lightbulbCount)
        {
            Debug.LogWarning($"Mismatch between area lights ({lightCount}) and lightbulbs ({lightbulbCount}). " +
                           "Lights and lightbulbs should be paired for synchronized activation.");
        }
        
        int maxCount = Mathf.Max(lightCount, lightbulbCount);
        
        // Get shared materials for all lightbulbs
        Material onMaterial = hasValidLightbulbs ? lightbulbOnMaterials[0] : null;
        
        for (int i = 0; i < maxCount; i++)
        {
            // Activate area light (if exists)
            if (areaLights != null && i < areaLights.Length && areaLights[i] != null)
            {
                Debug.Log($"Turning on area light [{i}] {areaLights[i].gameObject.name}");
                areaLights[i].enabled = true; // ← ENABLE THE LIGHT FIRST!
                StartCoroutine(FadeInLight(areaLights[i], areaLightIntensity, 0.3f));
            }
            
            // Activate lightbulb material (if exists) - happens immediately at start of light fade
            if (hasValidLightbulbs && i < lightbulbRenderers.Length && 
                lightbulbRenderers[i] != null && onMaterial != null)
            {
                lightbulbRenderers[i].material = onMaterial;
                Debug.Log($"Switched lightbulb [{i}] {lightbulbRenderers[i].gameObject.name} to ON material");
            }
            
            // Wait before next light activation
            yield return new WaitForSeconds(0.1f);
        }
        
        // Mark that area lights are now on
        areaLightsCurrentlyOn = true;

        // Activate server lights with same timing
        for (int i = 0; i < serverLights.Length; i++)
        {
            if (serverLights[i] != null)
            {
                serverLights[i].enabled = true; // ← ENABLE THE LIGHT FIRST!
                serverLights[i].color = lightColor; // Set color
                StartCoroutine(FadeInLight(serverLights[i], lightIntensity, 0.3f));
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        yield return new WaitForSeconds(1f);
        
        // Finally reveal the clue
        RevealClue();
        
        Debug.Log("Sequential light activation complete");
    }
    
    private IEnumerator PostLockdownPowerSequence()
    {
        // Phase 1: Turn everything on momentarily (2 seconds of hope)
        Debug.Log("Phase 1: Momentary power activation - giving false hope");
        
        Material onMaterial = ValidateLightbulbArrays() ? lightbulbOnMaterials[0] : null;
        
        // Turn on all lights and swap all lightbulb materials to ON simultaneously
        for (int i = 0; i < areaLights.Length; i++)
        {
            if (areaLights[i] != null)
            {
                areaLights[i].enabled = true;
                areaLights[i].intensity = areaLightIntensity;
                Debug.Log($"Momentarily turned on area light [{i}] {areaLights[i].gameObject.name}");
            }
        }
        
        for (int i = 0; i < lightbulbRenderers.Length; i++)
        {
            if (lightbulbRenderers[i] != null && onMaterial != null)
            {
                lightbulbRenderers[i].material = onMaterial;
                Debug.Log($"Momentarily switched lightbulb [{i}] {lightbulbRenderers[i].gameObject.name} to ON material");
            }
        }
        
        // Also turn on server lights for the full effect
        for (int i = 0; i < serverLights.Length; i++)
        {
            if (serverLights[i] != null)
            {
                serverLights[i].enabled = true;
                serverLights[i].color = lightColor;
                serverLights[i].intensity = lightIntensity;
            }
        }

        // Turn on registered servers (momentarily switch to Normal materials)
        if (useServerRackSystem && serversToActivate != null && serversToActivate.Length > 0)
        {
            Debug.Log($"Momentarily activating {serversToActivate.Length} servers to Normal state");
            for (int i = 0; i < serversToActivate.Length; i++)
            {
                var server = serversToActivate[i];
                if (server != null)
                {
                    server.SetState(ServerRackMaterialController.ServerState.Normal);
                    Debug.Log($"Momentarily activated server [{i}] {server.gameObject.name} to Normal state");
                }
            }
        }
        // if (useServerRackSystem && serversToActivate != null && serversToActivate.Length > 0)
        // {
        //     Debug.Log($"Reverting {serversToActivate.Length} servers back to Emergency/Lockdown state");
        //     for (int i = 0; i < serversToActivate.Length; i++)
        //     {
        //         var server = serversToActivate[i];
        //         if (server != null)
        //         {
        //             // Log current state before changing
        //             var currentState = server.GetCurrentState();
        //             Debug.Log($"Server [{i}] {server.gameObject.name} current state: {currentState} -> changing to Emergency");
                    
        //             server.SetState(ServerRackMaterialController.ServerState.Emergency);
                    
        //             // Verify the change worked
        //             var newState = server.GetCurrentState();
        //             Debug.Log($"Server [{i}] {server.gameObject.name} new state: {newState}");
                    
        //             if (newState != ServerRackMaterialController.ServerState.Emergency)
        //             {
        //                 Debug.LogWarning($"Server [{i}] {server.gameObject.name} failed to change to Emergency state! Still: {newState}");
        //             }
        //         }
        //         else
        //         {
        //             Debug.LogWarning($"Server [{i}] is null - cannot revert to Emergency state");
        //         }
        //     }
        // }

        // Reveal clue during the hope phase (clue logic should remain the same)
        RevealClue();
        
        // Wait for 3 seconds of "false hope"
        yield return new WaitForSeconds(3f);
        
        // Phase 2: Lockdown reasserts - turn everything back off
        Debug.Log("Phase 2: Lockdown reasserting - hope crushed");
        
        Material offMaterial = ValidateLightbulbArrays() ? lightbulbOffMaterials[0] : null;
        
        // Turn off all lights and swap lightbulb materials back to OFF
        for (int i = 0; i < areaLights.Length; i++)
        {
            if (areaLights[i] != null)
            {
                areaLights[i].enabled = false;
                Debug.Log($"Lockdown turned off area light [{i}] {areaLights[i].gameObject.name}");
            }
        }
        
        for (int i = 0; i < lightbulbRenderers.Length; i++)
        {
            if (lightbulbRenderers[i] != null && offMaterial != null)
            {
                lightbulbRenderers[i].material = offMaterial;
                Debug.Log($"Lockdown switched lightbulb [{i}] {lightbulbRenderers[i].gameObject.name} back to OFF material");
            }
        }
        
        // Turn off server lights too
        for (int i = 0; i < serverLights.Length; i++)
        {
            if (serverLights[i] != null)
            {
                serverLights[i].enabled = false;
            }
        }
        
        // Revert registered servers back to Emergency/Lockdown state
        if (useServerRackSystem && serversToActivate != null && serversToActivate.Length > 0)
        {
            Debug.Log($"Reverting {serversToActivate.Length} servers back to Emergency/Lockdown state");
            for (int i = 0; i < serversToActivate.Length; i++)
            {
                var server = serversToActivate[i];
                if (server != null)
                {
                    server.SetState(ServerRackMaterialController.ServerState.Emergency);
                    Debug.Log($"Reverted server [{i}] {server.gameObject.name} back to Emergency state");
                }
            }
        }

        Debug.Log("Post-lockdown power sequence complete - back to lockdown state");
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

        // Show code found text
        if (ItemFoundFeedbackManager.Instance != null)
        {
            ItemFoundFeedbackManager.Instance.ShowCodeFoundSequence();
        }

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
    
    // Debug methods
    [ContextMenu("Test Server Activation")]
    private void TestServerActivation()
    {
        if (serversToActivate != null && serversToActivate.Length > 0)
        {
            Debug.Log($"Testing activation of {serversToActivate.Length} registered servers");
            StartCoroutine(ActivateRegisteredServers());
        }
        else
        {
            Debug.LogWarning("No servers registered for activation! Assign servers in the inspector.");
        }
    }
    
    [ContextMenu("Test Lightbulb Activation")]
    private void TestLightbulbActivation()
    {
        if (ValidateLightbulbArrays())
        {
            Debug.Log("Testing lightbulb material swapping");
            StartCoroutine(SequentialLightUp());
        }
        else
        {
            Debug.LogWarning("Lightbulb arrays not properly configured!");
        }
    }

    [ContextMenu("Show Registered Server States")]
    private void ShowRegisteredServerStates()
    {
        if (serversToActivate != null && serversToActivate.Length > 0)
        {
            Debug.Log("=== Registered Server States ===");
            for (int i = 0; i < serversToActivate.Length; i++)
            {
                var server = serversToActivate[i];
                if (server != null)
                {
                    Debug.Log($"[{i}] {server.gameObject.name}: {server.GetCurrentState()}");
                }
                else
                {
                    Debug.Log($"[{i}] NULL REFERENCE");
                }
            }
            Debug.Log("================================");
        }
        else
        {
            Debug.LogWarning("No servers registered!");
        }
    }
    
    [ContextMenu("Reset Registered Servers to PoweredOff")]
    private void ResetRegisteredServersToPoweredOff()
    {
        if (serversToActivate != null && serversToActivate.Length > 0)
        {
            Debug.Log($"Resetting {serversToActivate.Length} registered servers to PoweredOff state");
            for (int i = 0; i < serversToActivate.Length; i++)
            {
                var server = serversToActivate[i];
                if (server != null)
                {
                    server.SetState(ServerRackMaterialController.ServerState.PoweredOff);
                    Debug.Log($"Reset {server.gameObject.name} to PoweredOff");
                }
            }
        }
        else
        {
            Debug.LogWarning("No servers registered!");
        }
    }
}