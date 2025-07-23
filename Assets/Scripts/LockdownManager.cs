using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LockdownManager : MonoBehaviour
{
    [Header("Lockdown Timing")]
    [SerializeField] private float baseLockdownTime = 900f; // 15 minutes in seconds
    [SerializeField] private float escapeWindowDuration = 60f; // 1 minute
    [SerializeField] private float finalPhaseDuration = 300f; // 5 minutes
    [SerializeField] private float timeExtensionPerCode = 120f; // 2 minutes per code
    
    [Header("Lighting System")]
    [SerializeField] private Material[] normalServerMaterials; // Original blue materials
    [SerializeField] private Material[] lockdownServerMaterials; // Red versions of the materials
    [SerializeField] private MeshRenderer[] serverRenderers; // Objects that use server materials
    [SerializeField] private Light[] serverLights; // If you have actual Light components
    [SerializeField] private Color normalLightColor = Color.blue;
    [SerializeField] private Color lockdownLightColor = Color.red;
    [SerializeField] private float lightTransitionDuration = 2f;
    
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
        if (!lockdownStarted)
        {
            lockdownTimer += Time.deltaTime;
            
            // Check if lockdown time has been reached
            if (lockdownTimer >= totalLockdownTime)
            {
                InitiateLockdown();
            }
        }
        else
        {
            // Handle lockdown phases
            float timeSinceLockdown = lockdownTimer - totalLockdownTime;
            
            if (currentPhase == LockdownPhase.EscapeWindow && timeSinceLockdown >= escapeWindowDuration)
            {
                StartFinalLockdown();
            }
            else if (currentPhase == LockdownPhase.FinalLockdown && timeSinceLockdown >= escapeWindowDuration + finalPhaseDuration)
            {
                EndGame();
            }
            
            lockdownTimer += Time.deltaTime;
        }
    }
    
    public void OnCodeEntered()
    {
        codesEnteredCount++;
        
        // Only give extensions for first two codes
        if (codesEnteredCount <= 2)
        {
            totalLockdownTime += timeExtensionPerCode;
            Debug.Log($"Code entered! Lockdown extended by {timeExtensionPerCode/60f} minutes. New lockdown time: {FormatGameTime(totalLockdownTime)}");
            
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
        
        // Switch to lockdown lighting
        StartCoroutine(TransitionToLockdownLighting());
        
        // Notify other systems
        OnLockdownInitiated?.Invoke();
        OnLockdownPhaseChanged?.Invoke(currentPhase);
    }
    
    private IEnumerator TransitionToLockdownLighting()
    {
        Debug.Log("Starting lighting transition to lockdown mode");
        
        // Brief delay to let announcement play
        yield return new WaitForSeconds(1f);
        
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
        SwapServerMaterials();
    }
    
    private void SwapServerMaterials()
    {
        if (normalServerMaterials.Length == 0 || lockdownServerMaterials.Length == 0)
        {
            Debug.Log("No materials configured for swapping");
            return;
        }
        
        if (normalServerMaterials.Length != lockdownServerMaterials.Length)
        {
            Debug.LogError("Normal and lockdown material arrays must be the same length!");
            return;
        }
        
        Debug.Log($"Swapping {serverRenderers.Length} renderers to lockdown materials");
        
        foreach (MeshRenderer renderer in serverRenderers)
        {
            if (renderer == null) continue;
            
            Material[] currentMaterials = renderer.materials;
            
            for (int i = 0; i < currentMaterials.Length; i++)
            {
                // Find matching normal material and replace with lockdown version
                for (int j = 0; j < normalServerMaterials.Length; j++)
                {
                    if (currentMaterials[i] == normalServerMaterials[j])
                    {
                        currentMaterials[i] = lockdownServerMaterials[j];
                        break;
                    }
                }
            }
            
            renderer.materials = currentMaterials;
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

        // Trigger the lighting transition to red
        DualLightmapController lightmapController = FindObjectOfType<DualLightmapController>();
        if (lightmapController != null)
        {
            lightmapController.InitiateLockdownLighting();
            Debug.Log("Lighting transition to red initiated");
        }
        else
        {
            Debug.LogError("DualLightmapController not found!");
        }
        
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