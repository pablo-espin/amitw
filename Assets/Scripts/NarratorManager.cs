using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NarratorManager : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioSource narratorSource;
    [SerializeField] private float minTimeBetweenLines = 5f; // Cooldown between any narrator lines
    [SerializeField] private float defaultVolume = 1f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    // Track played lines to avoid repetition
    private HashSet<string> playedDialogueIDs = new HashSet<string>();
    private float lastPlayedTime = -10f; // Start negative to allow immediate play
    private AudioClip currentlyPlaying = null;
    private Coroutine fadeCoroutine = null;
    
    // Singleton pattern
    public static NarratorManager Instance { get; private set; }
    
    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Initialize audio source if not set
        if (narratorSource == null)
        {
            narratorSource = gameObject.AddComponent<AudioSource>();
            narratorSource.playOnAwake = false;
            narratorSource.spatialBlend = 0f; // 2D sound
            narratorSource.volume = defaultVolume;
        }
    }
    
    /// <summary>
    /// Play a narrator dialogue clip
    /// </summary>
    /// <param name="clip">The audio clip to play</param>
    /// <param name="dialogueID">Unique identifier for this line (to prevent repetition)</param>
    /// <param name="forcePlay">Whether to play regardless of cooldown/repetition</param>
    /// <param name="volume">Volume to play at (defaults to defaultVolume)</param>
    /// <returns>True if played, false otherwise</returns>
    public bool PlayDialogue(AudioClip clip, string dialogueID, bool forcePlay = false, float volume = -1f)
    {
        // Check if we can play this dialogue
        if (!forcePlay)
        {
            // Check cooldown
            if (Time.time - lastPlayedTime < minTimeBetweenLines)
            {
                if (showDebugInfo)
                    Debug.Log($"Narrator: Can't play {dialogueID} - On cooldown ({Time.time - lastPlayedTime} < {minTimeBetweenLines})");
                return false;
            }
            
            // Check if already played
            if (!string.IsNullOrEmpty(dialogueID) && playedDialogueIDs.Contains(dialogueID))
            {
                if (showDebugInfo)
                    Debug.Log($"Narrator: Can't play {dialogueID} - Already played");
                return false;
            }
        }
        
        // If something is already playing, fade it out
        if (narratorSource.isPlaying && fadeCoroutine == null)
        {
            fadeCoroutine = StartCoroutine(FadeOutAndPlay(clip, dialogueID, volume));
            return true;
        }
        
        // Play the clip directly
        return PlayClipDirectly(clip, dialogueID, volume);
    }
    
    private bool PlayClipDirectly(AudioClip clip, string dialogueID, float volume = -1f)
    {
        if (clip == null)
            return false;
            
        if (volume < 0)
            volume = defaultVolume;
            
        currentlyPlaying = clip;
        narratorSource.clip = clip;
        narratorSource.volume = volume;
        narratorSource.Play();
        
        lastPlayedTime = Time.time;
        
        // Add to played dialogues if it has an ID
        if (!string.IsNullOrEmpty(dialogueID))
            playedDialogueIDs.Add(dialogueID);
            
        if (showDebugInfo)
            Debug.Log($"Narrator: Playing {dialogueID} ({clip.name})");
            
        return true;
    }
    
    private IEnumerator FadeOutAndPlay(AudioClip nextClip, string dialogueID, float volume)
    {
        // Fade out current audio
        float startVolume = narratorSource.volume;
        float fadeTime = 0.5f;
        float elapsed = 0;
        
        while (elapsed < fadeTime)
        {
            narratorSource.volume = Mathf.Lerp(startVolume, 0, elapsed / fadeTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Stop current audio and play the new clip
        narratorSource.Stop();
        PlayClipDirectly(nextClip, dialogueID, volume);
        
        fadeCoroutine = null;
    }
    
    /// <summary>
    /// Check if a specific dialogue has been played already
    /// </summary>
    public bool HasDialoguePlayed(string dialogueID)
    {
        return !string.IsNullOrEmpty(dialogueID) && playedDialogueIDs.Contains(dialogueID);
    }
    
    /// <summary>
    /// Check if any dialogue is currently playing
    /// </summary>
    public bool IsPlaying()
    {
        return narratorSource.isPlaying;
    }
    
    /// <summary>
    /// Stop any currently playing dialogue
    /// </summary>
    public void StopDialogue()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
        
        narratorSource.Stop();
        currentlyPlaying = null;
    }
    
    /// <summary>
    /// Reset the narrator state (clear played dialogues)
    /// </summary>
    public void ResetState()
    {
        playedDialogueIDs.Clear();
        lastPlayedTime = -10f;
    }
}