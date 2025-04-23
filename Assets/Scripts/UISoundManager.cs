using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UISoundManager : MonoBehaviour
{
    [System.Serializable]
    public class UISoundGroup
    {
        public string groupName;
        public AudioClip[] clips;
        [Range(0f, 1f)]
        public float volume = 1f;
    }

    [Header("Sound Groups")]
    [SerializeField] private UISoundGroup buttonClicks;
    [SerializeField] private UISoundGroup buttonHovers;
    [SerializeField] private UISoundGroup toggleSounds;
    [SerializeField] private UISoundGroup typingSounds;
    [SerializeField] private UISoundGroup notificationSounds;
    [SerializeField] private UISoundGroup errorSounds;
    [SerializeField] private UISoundGroup successSounds;

    [Header("Settings")]
    [SerializeField] private int audioSourcePoolSize = 3;
    [SerializeField] private bool randomizeClips = true;
    [SerializeField] [Range(0f, 1f)] private float masterVolume = 1f;

    // Audio source pool for playing multiple sounds simultaneously
    private List<AudioSource> audioSourcePool;
    
    // Singleton instance
    public static UISoundManager Instance { get; private set; }

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
        
        // If all sources are busy, use the first one
        return audioSourcePool[0];
    }

    // Play a random clip from a sound group
    private void PlaySound(UISoundGroup soundGroup)
    {
        if (soundGroup == null || soundGroup.clips == null || soundGroup.clips.Length == 0)
            return;
            
        AudioSource source = GetAvailableAudioSource();
        
        // Get a clip to play
        AudioClip clipToPlay;
        if (randomizeClips)
            clipToPlay = soundGroup.clips[Random.Range(0, soundGroup.clips.Length)];
        else
            clipToPlay = soundGroup.clips[0];
            
        // Set volume and play
        source.volume = soundGroup.volume * masterVolume;
        source.PlayOneShot(clipToPlay);
    }

    // Public methods for different UI sounds
    public void PlayButtonClick()
    {
        PlaySound(buttonClicks);
    }

    public void PlayButtonHover()
    {
        PlaySound(buttonHovers);
    }

    public void PlayToggle()
    {
        PlaySound(toggleSounds);
    }

    public void PlayTyping()
    {
        PlaySound(typingSounds);
    }

    public void PlayNotification()
    {
        PlaySound(notificationSounds);
    }

    public void PlayError()
    {
        PlaySound(errorSounds);
    }

    public void PlaySuccess()
    {
        PlaySound(successSounds);
    }

    // Method to play a custom UI sound if needed
    public void PlayCustomSound(AudioClip clip, float volume = 1f)
    {
        if (clip == null)
            return;
            
        AudioSource source = GetAvailableAudioSource();
        source.volume = volume * masterVolume;
        source.PlayOneShot(clip);
    }

    // Method to set master volume
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
    }
}