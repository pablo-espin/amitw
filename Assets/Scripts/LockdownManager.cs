using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class LockdownManager : MonoBehaviour
{
    [Header("Lockdown Timing")]
    [SerializeField] private float baseLockdownTime = 900f; // 15 minutes in seconds
    [SerializeField] private float escapeWindowDuration = 60f; // 1 minute
    [SerializeField] private float finalPhaseDuration = 540f; // 9 minutes
    [SerializeField] private float timeExtensionPerCode = 60f; // 1 minute per code
    
    [Header("Lighting System")]
    [SerializeField] private Material[] normalServerMaterials; // Original blue materials
    [SerializeField] private Material[] lockdownServerMaterials; // Red versions of the materials
    [SerializeField] private MeshRenderer[] serverRenderers; // Objects that use server materials
    [SerializeField] private Light[] serverLights; // If you have actual Light components
    [SerializeField] private Color normalLightColor = Color.blue;
    [SerializeField] private Color lockdownLightColor = Color.red;
    [SerializeField] private float lightTransitionDuration = 2f;

    [Header("Ceiling Light System")]
    [SerializeField] private Material[] normalCeilingLightMaterials; // Lights turned on material
    [SerializeField] private Material[] lockdownCeilingLightMaterials; // Lights turned off material  
    [SerializeField] private MeshRenderer[] ceilingLightRenderers; // Objects using ceiling light materials
    [SerializeField] private float ceilingLightFadeDuration = 1.5f; // How long the emission fade takes
    [SerializeField] private bool useCeilingLightFade = true; // Toggle the effect
    
    [Header("Post Processing")]
    [SerializeField] private UnityEngine.Rendering.Universal.ColorLookup colorLookup; // Optional: for post-processing tint
    
    [Header("Audio")]
    [SerializeField] private AudioSource facilityAudioSource;
    [SerializeField] private AudioClip lockdownAnnouncementClip;
    [SerializeField] private AudioClip[] creepyAmbientSounds; // Footsteps, mechanical sounds
    [SerializeField] private float ambientSoundInterval = 10f; // Seconds between creepy sounds
    
    public enum LockdownPhase 
    { 
        Normal,          // Before lockdown time
        EscapeWindow,    // 1 minute escape window
        FinalLockdown    // Final 5 minutes with creepy sounds
    }
    
    // Events
    public System.Action<LockdownPhase> OnLockdownPhaseChanged;
    public System.Action OnLockdownInitiated;
    public System.Action OnEscapeWindowClosed;
    
    // State
    private LockdownPhase currentPhase = LockdownPhase.Normal;
    private float lockdownTimer = 0f;
    private float totalLockdownTime;
    private bool lockdownStarted = false;
    private int codesEnteredCount = 0;
    private Coroutine ambientSoundCoroutine;
    private bool isPaused = false;
    private float pausedTimeRemaining;
    
    // References
    private GameHUDManager hudManager;
    private ExitDoorController exitDoor;
    
    // Singleton
    public static LockdownManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // Calculate total lockdown time (base + any extensions from codes)
        totalLockdownTime = baseLockdownTime;
        
        // Find references
        hudManager = FindObjectOfType<GameHUDManager>();
        exitDoor = FindObjectOfType<ExitDoorController>();
        
        // Setup audio source if not assigned
        if (facilityAudioSource == null)
        {
            facilityAudioSource = gameObject.AddComponent<AudioSource>();
            facilityAudioSource.spatialBlend = 0f; // 2D sound for facility-wide announcement
        }
        
        Debug.Log($"Lockdown system initialized. Lockdown at: {FormatGameTime(totalLockdownTime)}");
    }
    
    private void Update()
    {
        // Only update timer if not paused
        if (!isPaused)
        {
            if (!lockdownStarted)
            {
                // Pre-lockdown phase: count up to lockdown time
                lockdownTimer += Time.deltaTime;
                
                // Check if lockdown time has been reached
                if (lockdownTimer >= totalLockdownTime)
                {
                    InitiateLockdown();
                }
            }
            else
            {
                // Post-lockdown phase: handle lockdown phases
                lockdownTimer += Time.deltaTime;
                float timeSinceLockdown = lockdownTimer - totalLockdownTime;
                
                // Check for phase transitions
                if (currentPhase == LockdownPhase.EscapeWindow && 
                    timeSinceLockdown >= escapeWindowDuration)
                {
                    StartFinalLockdown();
                }
                else if (currentPhase == LockdownPhase.FinalLockdown && 
                         timeSinceLockdown >= escapeWindowDuration + finalPhaseDuration)
                {
                    EndGame();
                }
            }
        }
    }
    
    public void PauseTimer()
    {
        if (!isPaused)
        {
            isPaused = true;
            // Store current time remaining when paused
            pausedTimeRemaining = lockdownTimer;
            Debug.Log($"LockdownManager paused at {FormatGameTime(lockdownTimer)}");
        }
    }
    
    public void ResumeTimer()
    {
        if (isPaused)
        {
            isPaused = false;
            // Restore the time remaining from when we paused
            lockdownTimer = pausedTimeRemaining;
            Debug.Log($"LockdownManager resumed at {FormatGameTime(lockdownTimer)}");
        }
    }

    public void OnCodeEntered()
    {
        codesEnteredCount++;

        // Only give extensions for first two codes
        if (codesEnteredCount <= 2)
        {
            totalLockdownTime += timeExtensionPerCode;
            Debug.Log($"Code entered! Lockdown extended by {timeExtensionPerCode / 60f} minutes. New lockdown time: {FormatGameTime(totalLockdownTime)}");

            // Notify HUD of time extension
            if (hudManager != null)
            {
                hudManager.OnLockdownTimeExtended(timeExtensionPerCode);
            }
        }
    }
    
    private void InitiateLockdown()
    {
        lockdownStarted = true;
        currentPhase = LockdownPhase.EscapeWindow;
        
        Debug.Log("LOCKDOWN INITIATED - Escape window open");
        
        // Store ceiling light emission values before any transitions
        if (useCeilingLightFade)
        {
            StoreCeilingEmissionValues();
        }

        // Play facility announcement
        if (facilityAudioSource != null && lockdownAnnouncementClip != null)
        {
            facilityAudioSource.PlayOneShot(lockdownAnnouncementClip);
        }
        
        // Trigger narrator dialogue about escape
        if (GameInteractionDialogueManager.Instance != null)
        {
            GameInteractionDialogueManager.Instance.OnLockdownInitiated();
        }
        
        // Enable exit door
        if (exitDoor != null)
        {
            exitDoor.SetEscapeWindowActive(true);
        }

        // Start the sequenced lighting transitions
        StartCoroutine(SequencedLightingTransition());

        // Switch to lockdown lighting
        // StartCoroutine(TransitionToLockdownLighting());
        
        // Notify other systems
        OnLockdownInitiated?.Invoke();
        OnLockdownPhaseChanged?.Invoke(currentPhase);
    }
    
    private IEnumerator SequencedLightingTransition()
    {
        Debug.Log("Starting lighting transition to lockdown mode");
        
        // Brief delay to let announcement play
        yield return new WaitForSeconds(1f);
        
        // Step 1: Ceiling lights fade and switch (happens first)
        yield return StartCoroutine(TransitionCeilingLights());

        // Start lighting transition
        yield return StartCoroutine(AnimateLightingTransition());
        
        Debug.Log("Lockdown lighting transition complete");
    }
    
    private IEnumerator AnimateLightingTransition()
    {
        float elapsed = 0f;
        
        // Store original colors for smooth transition
        Color[] originalLightColors = new Color[serverLights.Length];
        for (int i = 0; i < serverLights.Length; i++)
        {
            if (serverLights[i] != null)
                originalLightColors[i] = serverLights[i].color;
        }
        
        while (elapsed < lightTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / lightTransitionDuration;
            
            // Animate Light components
            for (int i = 0; i < serverLights.Length; i++)
            {
                if (serverLights[i] != null)
                {
                    serverLights[i].color = Color.Lerp(originalLightColors[i], lockdownLightColor, t);
                }
            }
            
            // Animate ambient lighting
            RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, lockdownLightColor * 0.3f, t);
            
            yield return null;
        }
        
        // Ensure final state is set
        for (int i = 0; i < serverLights.Length; i++)
        {
            if (serverLights[i] != null)
            {
                serverLights[i].color = lockdownLightColor;
            }
        }
        
        // Swap materials if provided
        // SwapServerMaterials();
    }
    
    private void SwapCeilingLightMaterials()
    {
        if (normalCeilingLightMaterials.Length == 0 || lockdownCeilingLightMaterials.Length == 0)
        {
            Debug.Log("No ceiling light materials configured for swapping");
            return;
        }
        
        Debug.Log($"Attempting to swap {ceilingLightRenderers.Length} ceiling light renderers to lockdown materials");
        
        foreach (MeshRenderer renderer in ceilingLightRenderers)
        {
            if (renderer == null) continue;
            
            // Direct material swap
            for (int j = 0; j < normalCeilingLightMaterials.Length; j++)
            {
                // Check if the shared material matches our normal material
                if (renderer.sharedMaterial == normalCeilingLightMaterials[j] || 
                    renderer.sharedMaterial.name.Replace(" (Instance)", "") == normalCeilingLightMaterials[j].name)
                {
                    Debug.Log($"Swapping {renderer.name} from {renderer.material.name} to {lockdownCeilingLightMaterials[j].name}");
                    renderer.material = lockdownCeilingLightMaterials[j];
                    break;
                }
            }
        }
    }
    
    // Store original emission values for precise fading
    private Dictionary<Material, Color> originalCeilingEmissionColors = new Dictionary<Material, Color>();

    private void StoreCeilingEmissionValues()
    {
        foreach (MeshRenderer renderer in ceilingLightRenderers)
        {
            if (renderer == null) continue;
            
            foreach (Material mat in renderer.materials)
            {
                if (mat.HasProperty("_EmissionColor") && !originalCeilingEmissionColors.ContainsKey(mat))
                {
                    originalCeilingEmissionColors[mat] = mat.GetColor("_EmissionColor");
                }
            }
        }
    }

    private void FadeCeilingEmission(float intensity)
    {
        foreach (MeshRenderer renderer in ceilingLightRenderers)
        {
            if (renderer == null) continue;
            
            Material[] materials = renderer.materials;
            
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i].HasProperty("_EmissionColor"))
                {
                    // Use stored original color or fallback to blue
                    Color originalEmission = originalCeilingEmissionColors.ContainsKey(materials[i]) 
                        ? originalCeilingEmissionColors[materials[i]] 
                        : Color.blue * 2f; // Adjust this to match your emission intensity
                    
                    Color fadedEmission = originalEmission * intensity;
                    materials[i].SetColor("_EmissionColor", fadedEmission);
                }
            }
            
            renderer.materials = materials;
        }
    }

    private IEnumerator TransitionCeilingLights()
    {
        if (!useCeilingLightFade || ceilingLightRenderers.Length == 0)
        {
            // Skip fade, just do immediate swap if materials are set up
            SwapCeilingLightMaterials();
            // Trigger lightmap switch immediately
            TriggerLightmapSwitch();
            yield break;
        }
        
        Debug.Log("Starting ceiling light fade transition");
        
        // Phase 1: Fade emission down
        float elapsed = 0f;
        while (elapsed < ceilingLightFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / ceilingLightFadeDuration;
            float intensity = Mathf.Lerp(1f, 0f, t); // Fade from full to zero
            
            FadeCeilingEmission(intensity);
            
            yield return null;
        }
        
        // Ensure emission is fully off
        FadeCeilingEmission(0f);
        
        // Small pause to emphasize the "power down" moment
        yield return new WaitForSeconds(0.02f);
        
        // Phase 2: Swap to lockdown materials
        SwapCeilingLightMaterials();
        TriggerLightmapSwitch();
        
        Debug.Log("Ceiling light transition complete");
    }

    private void TriggerLightmapSwitch()
    {
        // Trigger the lighting transition to lockdown lightmaps
        DualLightmapController lightmapController = FindObjectOfType<DualLightmapController>();
        if (lightmapController != null)
        {
            lightmapController.InitiateLockdownLighting();
            Debug.Log("Lightmap transition initiated with ceiling light swap");
        }
        else
        {
            Debug.LogError("DualLightmapController not found!");
        }
    }

    private void StartFinalLockdown()
    {
        currentPhase = LockdownPhase.FinalLockdown;

        Debug.Log("Final lockdown phase - Escape window closed");

        // Disable exit door
        if (exitDoor != null)
        {
            exitDoor.SetEscapeWindowActive(false);
        }

        // Trigger server rack emergency mode
        ServerRackMaterialController.SetAllRacksEmergencyMode(true, true, 0.05f);

        // Start creepy ambient sounds
        if (creepyAmbientSounds != null && creepyAmbientSounds.Length > 0)
        {
            ambientSoundCoroutine = StartCoroutine(PlayCreepyAmbientSounds());
        }

        // Notify other systems
        OnEscapeWindowClosed?.Invoke();
        OnLockdownPhaseChanged?.Invoke(currentPhase);
    }
    
    private IEnumerator PlayCreepyAmbientSounds()
    {
        while (currentPhase == LockdownPhase.FinalLockdown)
        {
            yield return new WaitForSeconds(ambientSoundInterval + Random.Range(-3f, 3f));
            
            if (creepyAmbientSounds.Length > 0)
            {
                AudioClip randomSound = creepyAmbientSounds[Random.Range(0, creepyAmbientSounds.Length)];
                
                // Play at random position around player for immersion
                if (InteractionSoundManager.Instance != null)
                {
                    // Use 3D positioned sound for footsteps/mechanical sounds
                    Vector3 randomPosition = GetRandomPositionAroundPlayer();
                    // We'd need to extend InteractionSoundManager for this, or use a simpler approach
                    facilityAudioSource.PlayOneShot(randomSound);
                }
            }
        }
    }
    
    private Vector3 GetRandomPositionAroundPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Vector3 playerPos = player.transform.position;
            Vector2 randomCircle = Random.insideUnitCircle * 10f; // 10 meter radius
            return new Vector3(playerPos.x + randomCircle.x, playerPos.y, playerPos.z + randomCircle.y);
        }
        return Vector3.zero;
    }
    
    private void EndGame()
    {
        Debug.Log("Final lockdown complete - Player trapped");
        
        // Stop ambient sounds
        if (ambientSoundCoroutine != null)
        {
            StopCoroutine(ambientSoundCoroutine);
        }
        
        // Show trapped ending
        if (hudManager != null)
        {
            hudManager.ShowTrappedOutcome();
        }
    }
    
    // Public getters
    public float GetGameTime() => lockdownTimer;
    public float GetLockdownTime() => totalLockdownTime;
    public LockdownPhase GetCurrentPhase() => currentPhase;
    public bool IsLockdownStarted() => lockdownStarted;
    
    // Convert real time to game time format (5:00 PM to 6:00 PM)
    public string FormatGameTime(float realTimeSeconds)
    {
        // 15 real minutes (900 seconds) = 60 game minutes (1 game hour)
        // So: 1 real second = 60/900 = 1/15 game minutes
        float gameMinutesElapsed = realTimeSeconds / 15f; // Convert real seconds to game minutes
        
        float startTimeMinutes = 17f * 60f; // 5:00 PM = 17:00 = 1020 minutes since midnight
        float currentGameTimeMinutes = startTimeMinutes + gameMinutesElapsed;
        
        int totalMinutes = Mathf.FloorToInt(currentGameTimeMinutes);
        int hours = totalMinutes / 60;
        int minutes = totalMinutes % 60;
        
        // Convert to 12-hour format
        string ampm = hours >= 12 ? "PM" : "AM";
        if (hours > 12) hours -= 12;
        if (hours == 0) hours = 12;
        
        return $"{hours}:{minutes:00} {ampm}";
    }
}