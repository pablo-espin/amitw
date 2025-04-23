using UnityEngine;
using System.Collections.Generic;

public class InteractionSoundManager : MonoBehaviour
{
    [System.Serializable]
    public class InteractionSoundCategory
    {
        public string categoryName;
        public AudioClip[] clips;
        [Range(0f, 1f)]
        public float volume = 1f;
    }

    [Header("Memory Sphere Sounds")]
    [SerializeField] private InteractionSoundCategory memorySphereInteraction;

    [Header("Water System Sounds")]
    [SerializeField] private InteractionSoundCategory tapToggle;
    [SerializeField] private InteractionSoundCategory waterRunning;
    [SerializeField] private InteractionSoundCategory valveInteraction;

    [Header("Electricity System Sounds")]
    [SerializeField] private InteractionSoundCategory cableConnection;
    [SerializeField] private InteractionSoundCategory powerUp;

    [Header("Location System Sounds")]
    [SerializeField] private InteractionSoundCategory busCardExamine;
    [SerializeField] private InteractionSoundCategory locationListExamine;

    [Header("False Clue System Sounds")]
    [SerializeField] private InteractionSoundCategory computerBoot;
    [SerializeField] private InteractionSoundCategory falseClueReveal;
    [SerializeField] private InteractionSoundCategory matrixAnimation;

    [Header("Settings")]
    [SerializeField] private int audioSourcePoolSize = 5;
    [SerializeField] [Range(0f, 1f)] private float masterVolume = 1f;

    // Dictionary to store running audio sources for looping sounds
    private Dictionary<string, AudioSource> loopingSources = new Dictionary<string, AudioSource>();
    
    // Audio source pool for playing multiple sounds simultaneously
    private List<AudioSource> audioSourcePool;
    
    // Singleton instance
    public static InteractionSoundManager Instance { get; private set; }

    // Dictionary to track 3D positional sound sources
    private Dictionary<string, PositionalSoundInfo> positionalSounds = new Dictionary<string, PositionalSoundInfo>();

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

        // Class to store info about positional sounds
    private class PositionalSoundInfo
    {
        public AudioSource source;
        public Transform targetTransform;
        public float maxDistance;
        public float baseVolume;
    }
        
    private Transform playerTransform;
    
    private void Start()
    {
        // Find player transform - usually the camera for audio purposes
        playerTransform = Camera.main.transform;
        
        // Start updating positional sounds
        InvokeRepeating("UpdatePositionalSounds", 0.1f, 0.1f);
    }
    
    // Update positional sound volumes based on distance
    private void UpdatePositionalSounds()
    {
        if (playerTransform == null) return;
        
        foreach (var entry in positionalSounds)
        {
            PositionalSoundInfo info = entry.Value;
            if (info.source != null && info.targetTransform != null)
            {
                float distance = Vector3.Distance(playerTransform.position, info.targetTransform.position);
                float volumeFactor = Mathf.Clamp01(1.0f - (distance / info.maxDistance));
                
                // Update volume based on distance
                info.source.volume = info.baseVolume * volumeFactor * masterVolume;
                
                // Update position
                info.source.transform.position = info.targetTransform.position;
            }
        }
    }
        
    // Start a positional looping sound that changes volume with distance
    public AudioSource StartPositionalLoopingSound(InteractionSoundCategory category, string soundId, Transform sourceTransform, float maxDistance = 10f)
    {
        if (category == null || category.clips == null || category.clips.Length == 0 || sourceTransform == null)
            return null;
            
        // Stop previous instance if it exists
        StopLoopingSound(soundId);
        
        // Create a new audio source for this looping sound
        AudioSource loopSource = gameObject.AddComponent<AudioSource>();
        loopSource.playOnAwake = false;
        loopSource.loop = true;
        
        // Configure for 3D sound
        loopSource.spatialBlend = 1.0f; // Full 3D
        loopSource.rolloffMode = AudioRolloffMode.Linear;
        loopSource.minDistance = 1f;
        loopSource.maxDistance = maxDistance * 2f; // Set Unity's max distance for fall-off calculation
        
        // Get the first clip from the category (for looping sounds, we typically use the first one)
        loopSource.clip = category.clips[0];
        loopSource.volume = category.volume * masterVolume;
        loopSource.Play();
        
        // Store the source for later reference
        loopingSources[soundId] = loopSource;
        
        // Store positional information
        positionalSounds[soundId] = new PositionalSoundInfo
        {
            source = loopSource,
            targetTransform = sourceTransform,
            maxDistance = maxDistance,
            baseVolume = category.volume
        };
        
        // Update position immediately
        loopSource.transform.position = sourceTransform.position;
        
        return loopSource;
    }

    private void InitializeAudioSources()
    {
        audioSourcePool = new List<AudioSource>();
        
        // Create audio source pool
        for (int i = 0; i < audioSourcePoolSize; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f; // 2D sound
            audioSourcePool.Add(source);
        }
    }

    // Get an available audio source from the pool
    private AudioSource GetAvailableAudioSource()
    {
        foreach (AudioSource source in audioSourcePool)
        {
            if (!source.isPlaying)
                return source;
        }
        
        // If all sources are busy, create a new one
        AudioSource newSource = gameObject.AddComponent<AudioSource>();
        newSource.playOnAwake = false;
        newSource.spatialBlend = 0f;
        audioSourcePool.Add(newSource);
        
        return newSource;
    }

    // Play a random clip from a sound category
    private void PlaySound(InteractionSoundCategory category)
    {
        if (category == null || category.clips == null || category.clips.Length == 0)
            return;
            
        AudioSource source = GetAvailableAudioSource();
        
        // Get a random clip from the category
        AudioClip clipToPlay = category.clips[Random.Range(0, category.clips.Length)];
            
        // Set volume and play
        source.volume = category.volume * masterVolume;
        source.PlayOneShot(clipToPlay);
    }

    // Start a looping sound
    private AudioSource StartLoopingSound(InteractionSoundCategory category, string soundId)
    {
        if (category == null || category.clips == null || category.clips.Length == 0)
            return null;
            
        // Stop previous instance if it exists
        StopLoopingSound(soundId);
        
        // Create a new audio source for this looping sound
        AudioSource loopSource = gameObject.AddComponent<AudioSource>();
        loopSource.playOnAwake = false;
        loopSource.spatialBlend = 0f;
        loopSource.loop = true;
        
        // Get the first clip from the category (for looping sounds, we typically use the first one)
        loopSource.clip = category.clips[0];
        loopSource.volume = category.volume * masterVolume;
        loopSource.Play();
        
        // Store the source for later reference
        loopingSources[soundId] = loopSource;
        
        return loopSource;
    }

    // Stop a looping sound
    public void StopLoopingSound(string soundId)
    {
        if (loopingSources.TryGetValue(soundId, out AudioSource source))
        {
            source.Stop();
            Destroy(source);
            loopingSources.Remove(soundId);
        }
    }

    // Public methods for different interaction sounds

    // Memory Sphere
    public void PlayMemorySphereInteraction()
    {
        PlaySound(memorySphereInteraction);
    }

    // Water System
    public void PlayTapToggle()
    {
        PlaySound(tapToggle);
    }

    // Modified method to start water running with position
    public AudioSource StartWaterRunning(Transform waterSource)
    {
        return StartPositionalLoopingSound(waterRunning, "water_running", waterSource, 15f);
    }
        
    // Override the original method to ensure it doesn't get called without a position
    public AudioSource StartWaterRunning()
    {
        Debug.LogWarning("StartWaterRunning called without position - water sound will not be positional");
        return StartLoopingSound(waterRunning, "water_running");
    }

    public void StopWaterRunning()
    {
        StopLoopingSound("water_running");
    }

    public void PlayValveInteraction()
    {
        PlaySound(valveInteraction);
    }

    // Electricity System
    public void PlayCableConnection()
    {
        PlaySound(cableConnection);
    }

    public void PlayPowerUp()
    {
        PlaySound(powerUp);
    }

    // Location System
    public void PlayBusCardExamine()
    {
        PlaySound(busCardExamine);
    }

    public void PlayLocationListExamine()
    {
        PlaySound(locationListExamine);
    }

    // False Clue System
    public void PlayComputerBoot()
    {
        PlaySound(computerBoot);
    }

    public void PlayFalseClueReveal()
    {
        PlaySound(falseClueReveal);
    }

    public AudioSource StartMatrixAnimation()
    {
        return StartLoopingSound(matrixAnimation, "matrix_animation");
    }

    public void StopMatrixAnimation()
    {
        StopLoopingSound("matrix_animation");
    }

    // Method to set master volume
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        
        // Update volume of all looping sounds
        foreach (var source in loopingSources.Values)
        {
            if (source != null)
            {
                // Preserve the relative volume of each source
                InteractionSoundCategory category = null;
                // Find which category this source belongs to
                if (source.clip == waterRunning.clips[0]) category = waterRunning;
                else if (source.clip == matrixAnimation.clips[0]) category = matrixAnimation;
                
                if (category != null)
                {
                    source.volume = category.volume * masterVolume;
                }
            }
        }
    }
}