using UnityEngine;
using System.Collections;

public class RoomToneManager : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip stage1RoomTone; // Plays throughout at lower volume
    [SerializeField] private AudioClip stage2RoomTone; // Starts at 6 min
    [SerializeField] private AudioClip stage3RoomTone; // Starts at 9 min
    
    [Header("Timing Settings")]
    [SerializeField] private float stage2StartTime = 360f; // 6 minutes in seconds
    [SerializeField] private float stage3StartTime = 540f; // 9 minutes in seconds
    [SerializeField] private float crossfadeDuration = 3f; // Time to fade between stages
    
    [Header("Audio Settings")]
    [SerializeField] private float baseLayerVolume = 0.3f; // Lower volume for continuous base layer
    [SerializeField] private float secondaryLayerVolume = 0.5f; // Volume for secondary layers
    [SerializeField] private bool loopRoomTone = true;
    
    // Audio sources - one for base layer, one for secondary layers
    private AudioSource baseLayerSource;
    private AudioSource secondaryLayerSource;
    
    // Track the current stage
    private int currentStage = 0;
    
    // Track game timer
    private float gameTimer = 0f;
    private bool isRunning = true;
    
    private void Awake()
    {
        // Create audio sources
        baseLayerSource = gameObject.AddComponent<AudioSource>();
        secondaryLayerSource = gameObject.AddComponent<AudioSource>();
        
        // Configure audio sources
        ConfigureAudioSource(baseLayerSource, baseLayerVolume);
        ConfigureAudioSource(secondaryLayerSource, 0f); // Start with volume at 0
    }
    
    private void ConfigureAudioSource(AudioSource source, float initialVolume)
    {
        source.loop = loopRoomTone;
        source.volume = initialVolume;
        source.playOnAwake = false;
        source.spatialBlend = 0f; // 2D sound
    }
    
    private void Start()
    {
        // Start with stage 1 (base layer only)
        baseLayerSource.clip = stage1RoomTone;
        baseLayerSource.Play();
        currentStage = 1;
    }
    
    private void Update()
    {
        if (!isRunning) 
            return;
            
        // Update timer
        gameTimer += Time.deltaTime;
        
        // Check for stage transitions
        CheckStageTransitions();
    }
    
    private void CheckStageTransitions()
    {
        // Stage 1 to Stage 2 transition (adding second layer)
        if (currentStage == 1 && gameTimer >= stage2StartTime)
        {
            StartSecondaryLayer(stage2RoomTone);
            currentStage = 2;
        }
        // Stage 2 to Stage 3 transition (changing second layer)
        else if (currentStage == 2 && gameTimer >= stage3StartTime)
        {
            CrossfadeSecondaryLayer(stage3RoomTone);
            currentStage = 3;
        }
    }
    
    // Start playing a secondary layer on top of the base layer
    private void StartSecondaryLayer(AudioClip clip)
    {
        secondaryLayerSource.clip = clip;
        secondaryLayerSource.volume = 0f;
        secondaryLayerSource.Play();
        
        // Fade in the secondary layer
        StartCoroutine(FadeAudioSource(secondaryLayerSource, 0f, secondaryLayerVolume, crossfadeDuration));
    }
    
    // Crossfade the secondary layer to a new clip
    private void CrossfadeSecondaryLayer(AudioClip newClip)
    {
        StartCoroutine(CrossfadeSecondaryLayerCoroutine(newClip));
    }
    
    private IEnumerator CrossfadeSecondaryLayerCoroutine(AudioClip newClip)
    {
        // Create a temporary audio source for the new clip
        AudioSource tempSource = gameObject.AddComponent<AudioSource>();
        ConfigureAudioSource(tempSource, 0f);
        
        // Setup the new source
        tempSource.clip = newClip;
        tempSource.Play();
        
        // Fade out current secondary and fade in new one
        StartCoroutine(FadeAudioSource(secondaryLayerSource, secondaryLayerSource.volume, 0f, crossfadeDuration));
        StartCoroutine(FadeAudioSource(tempSource, 0f, secondaryLayerVolume, crossfadeDuration));
        
        // Wait for crossfade to complete
        yield return new WaitForSeconds(crossfadeDuration);
        
        // Stop and destroy the old secondary source
        secondaryLayerSource.Stop();
        Destroy(secondaryLayerSource);
        
        // Reassign the secondary layer source
        secondaryLayerSource = tempSource;
    }
    
    private IEnumerator FadeAudioSource(AudioSource source, float startVolume, float targetVolume, float duration)
    {
        float timer = 0;
        
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            
            source.volume = Mathf.Lerp(startVolume, targetVolume, t);
            
            yield return null;
        }
        
        // Ensure volume is set exactly at the end
        source.volume = targetVolume;
        
        // If faded to zero, stop the source
        if (targetVolume <= 0)
            source.Stop();
    }
    
    // Public method to pause/resume room tone
    public void SetRunning(bool running)
    {
        isRunning = running;
        
        if (running)
        {
            if (!baseLayerSource.isPlaying)
                baseLayerSource.Play();
                
            // Only restart secondary layer if we're in stage 2 or 3
            if (currentStage >= 2 && !secondaryLayerSource.isPlaying)
                secondaryLayerSource.Play();
        }
        else
        {
            baseLayerSource.Pause();
            secondaryLayerSource.Pause();
        }
    }
    
    // Method to manually change to a specific stage
    public void SetStage(int stage)
    {
        if (stage < 1 || stage > 3) return;
        if (stage == currentStage) return;
        
        switch (stage)
        {
            case 1:
                // Return to only base layer
                StartCoroutine(FadeAudioSource(secondaryLayerSource, secondaryLayerSource.volume, 0f, crossfadeDuration));
                currentStage = 1;
                break;
                
            case 2:
                if (currentStage == 1)
                {
                    // Add stage 2 secondary layer
                    StartSecondaryLayer(stage2RoomTone);
                }
                else if (currentStage == 3)
                {
                    // Switch from stage 3 to stage 2
                    CrossfadeSecondaryLayer(stage2RoomTone);
                }
                currentStage = 2;
                break;
                
            case 3:
                if (currentStage == 1)
                {
                    // Add stage 3 secondary layer directly
                    StartSecondaryLayer(stage3RoomTone);
                }
                else if (currentStage == 2)
                {
                    // Switch from stage 2 to stage 3
                    CrossfadeSecondaryLayer(stage3RoomTone);
                }
                currentStage = 3;
                break;
        }
    }
}