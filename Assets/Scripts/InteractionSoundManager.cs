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

    public AudioSource StartWaterRunning()
    {
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