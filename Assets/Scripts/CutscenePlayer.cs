using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Creates a smooth introduction cutscene using image sequences with crossfading transitions
/// and synchronized narration. Optimized for 3 audio clips.
/// </summary>
public class SimpleCutscenePlayer : MonoBehaviour
{
    [Header("Image Sequence")]
    [SerializeField] private Image foregroundImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Sprite[] imageSequence; // 4 images recommended
    [SerializeField] private float displayTime = 5.5f;
    [SerializeField] private float crossfadeTime = 1f;
    
    [Header("Audio")]
    [SerializeField] private AudioSource narratorSource;
    [SerializeField] private AudioClip[] narrationClips; // Your 3 audio clips
    [SerializeField] private float[] narrationTimings = new float[] { 1.0f, 11.0f, 21.0f }; // When to play each clip
    [SerializeField] private float delayBeforeGameStart = 2.0f;
    
    [Header("Transitions")]
    [SerializeField] private Image fadePanel;
    [SerializeField] private float initialFadeInTime = 1.0f;
    [SerializeField] private float finalFadeOutTime = 1.5f;
    
    // Optional camera movement effects
    [Header("Ken Burns Effect")]
    [SerializeField] private bool useKenBurnsEffect = true;
    [SerializeField] private float zoomAmount = 0.1f;
    [SerializeField] private float panAmount = 0.05f;

    [Header("Background Audio")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource ambientSource;
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private AudioClip ambientSound;
    [SerializeField] private float musicVolume = 0.5f;
    [SerializeField] private float ambientVolume = 0.3f;
    [SerializeField] private float fadeInDuration = 2.0f;
    [SerializeField] private float fadeOutDuration = 3.0f;
    
    // Cutscene state
    private int currentImageIndex = 0;
    private bool isCutsceneActive = false;
    private float cutsceneTimer = 0f;
    private float[] imageStartZooms; // Starting zoom for each image
    private float[] imageEndZooms;   // Ending zoom for each image
    //private Vector2[] imageStartPositions; // Starting positions for each image
    //private Vector2[] imageEndPositions;   // Ending positions for each image

    // Initialize these in Awake() or Start()
    private void InitializeKenBurnsParameters()
    {
        int imageCount = imageSequence.Length;
        imageStartZooms = new float[imageCount];
        imageEndZooms = new float[imageCount];
        
        for (int i = 0; i < imageCount; i++)
        {
            // Alternate between zooming in and zooming out
            if (i % 2 == 0)
            {
                // Zoom in
                imageStartZooms[i] = 1.0f;
                imageEndZooms[i] = 1.0f + zoomAmount;
            }
            else
            {
                // Zoom out
                imageStartZooms[i] = 1.0f + zoomAmount;
                imageEndZooms[i] = 1.0f;
            }
        }
    }

    private void Awake()
    {
        // Make sure we have the right number of images
        if (imageSequence == null || imageSequence.Length < 2)
        {
            Debug.LogError("Image sequence needs at least 2 images!");
            return;
        }
        
        // Initialize UI elements
        if (fadePanel != null)
        {
            fadePanel.color = new Color(0, 0, 0, 1); // Start with black screen
        }
        
        // Set up initial images
        if (foregroundImage != null && backgroundImage != null)
        {
            foregroundImage.sprite = imageSequence[0];
            backgroundImage.sprite = imageSequence[0];
            
            // Start with the foreground visible and background invisible
            foregroundImage.color = new Color(1, 1, 1, 0);
            backgroundImage.color = new Color(1, 1, 1, 0);
        }
    }

    private void Start()
    {
        // Force lock cursor using CursorManager
        if (CursorManager.Instance != null)
        {
            CursorManager.Instance.ForceLockCursor();
        }

        // Setup audio sources if needed
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
        }
        
        if (ambientSource == null)
        {
            ambientSource = gameObject.AddComponent<AudioSource>();
            ambientSource.playOnAwake = false;
            ambientSource.loop = true;
        }

        InitializeKenBurnsParameters();

        StartCoroutine(PlayCutscene());
    }
    
    private void Update()
    {
        if (isCutsceneActive)
        {
            cutsceneTimer += Time.deltaTime;
            
            // Check if we need to play any narration clips
            CheckNarrationTiming();
            
            // Apply Ken Burns effect if enabled
            if (useKenBurnsEffect)
            {
                ApplyKenBurnsEffect();
            }
        }
    }
    
    private IEnumerator PlayCutscene()
    {
        // Start background audio
        StartCoroutine(FadeInAudio(musicSource, backgroundMusic, musicVolume, fadeInDuration));
        StartCoroutine(FadeInAudio(ambientSource, ambientSound, ambientVolume, fadeInDuration));

        // Initial fade in
        yield return StartCoroutine(FadeIn(initialFadeInTime));
        
        // Start cutscene timer
        isCutsceneActive = true;
        cutsceneTimer = 0f;
        
        // Show first image
        foregroundImage.color = new Color(1, 1, 1, 1);
        
        // Play through image sequence with crossfades
        for (int i = 0; i < imageSequence.Length; i++)
        {
            // Wait for display duration 
            yield return new WaitForSeconds(displayTime);
            
            // Move to next image if available
            if (i < imageSequence.Length - 1)
            {
                yield return StartCoroutine(CrossfadeToNextImage(i + 1));
            }
        }
        
        // Wait until all narration is complete
        if (narratorSource != null && narratorSource.isPlaying)
        {
            while (narratorSource.isPlaying)
            {
                yield return null;
            }
        }
        
        // Additional delay before ending cutscene
        yield return new WaitForSeconds(delayBeforeGameStart);

        // Before the final fade out, fade out audio
        StartCoroutine(FadeOutAudio(musicSource, fadeOutDuration));
        StartCoroutine(FadeOutAudio(ambientSource, fadeOutDuration));

        // Final fade out
        yield return StartCoroutine(FadeOut(finalFadeOutTime));
        
        // Load game level
        // Use GameManager to transition to gameplay
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGameplay();
        }
        else
        {
            // Fallback if GameManager is not available
            SceneManager.LoadScene("GameLevel");
        }
    }
    
    private IEnumerator CrossfadeToNextImage(int nextIndex)
    {
        // Set up the next image in the background
        backgroundImage.sprite = imageSequence[nextIndex];
        backgroundImage.color = new Color(1, 1, 1, 0);
        
        // // Reset Ken Burns effect for the new image
        if (useKenBurnsEffect)
        {
            backgroundImage.rectTransform.localScale = Vector3.one * imageStartZooms[nextIndex];
        }
        
        // Fade in background, fade out foreground
        float elapsed = 0f;
        while (elapsed < crossfadeTime)
        {
            float t = elapsed / crossfadeTime;
            
            // Update alpha values
            backgroundImage.color = new Color(1, 1, 1, t);
            foregroundImage.color = new Color(1, 1, 1, 1 - t);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Ensure final state is correct
        backgroundImage.color = new Color(1, 1, 1, 1);
        foregroundImage.color = new Color(1, 1, 1, 0);
        
        // Swap images (foreground becomes new image, background becomes old)
        Sprite tempSprite = foregroundImage.sprite;
        foregroundImage.sprite = backgroundImage.sprite;
        backgroundImage.sprite = tempSprite;
        
        foregroundImage.color = new Color(1, 1, 1, 1);
        backgroundImage.color = new Color(1, 1, 1, 0);
        
        // Update current image index
        currentImageIndex = nextIndex;
    }
    
    private void CheckNarrationTiming()
    {
        if (narratorSource == null || narrationClips == null || narrationTimings == null)
            return;
            
        if (narrationClips.Length != narrationTimings.Length)
        {
            Debug.LogError("Narration clips and timings arrays must have the same length!");
            return;
        }
            
        // Check if we need to play a narration clip based on timing
        for (int i = 0; i < narrationClips.Length; i++)
        {
            if (cutsceneTimer >= narrationTimings[i] && narrationClips[i] != null)
            {
                // Only play if this is the right moment (within one frame)
                if (Mathf.Abs(cutsceneTimer - narrationTimings[i]) < Time.deltaTime)
                {
                    narratorSource.clip = narrationClips[i];
                    narratorSource.Play();
                    
                    if (i == 0)
                    {
                        // If this is the first clip, log the start time for debugging
                        Debug.Log("First narration clip started at: " + cutsceneTimer + " seconds");
                    }
                }
            }
        }
    }
    

    private void ApplyKenBurnsEffect()
    {
        // Only apply to visible image
        if (foregroundImage != null)
        {
            // Get which image we're on
            int imageIndex = Mathf.Min(Mathf.FloorToInt(cutsceneTimer / (displayTime + crossfadeTime)), imageSequence.Length - 1);
            
            // Calculate normalized time within the current image display (0 to 1)
            float timeInCurrentImage = cutsceneTimer - (imageIndex * (displayTime + crossfadeTime));
            float imageDisplayProgress = timeInCurrentImage / displayTime;
            
            // Clamp progress to 0-1 range to prevent values exceeding 1 during crossfade
            imageDisplayProgress = Mathf.Clamp01(imageDisplayProgress);
            
            // Determine if this image should zoom in or out (alternate)
            bool shouldZoomIn = (imageIndex % 2 == 0);
            
            // Apply the appropriate zoom effect
            float zoom;
            if (shouldZoomIn)
            {
                // Zoom in from 1.0 to 1.0 + zoomAmount
                zoom = 1.0f + (imageDisplayProgress * zoomAmount);
            }
            else
            {
                // Zoom out from 1.0 + zoomAmount to 1.0
                zoom = (1.0f + zoomAmount) - (imageDisplayProgress * zoomAmount);
            }
            
            // Apply zoom
            foregroundImage.rectTransform.localScale = Vector3.one * zoom;
            
            // Subtle panning effect (if desired)
            float panX = Mathf.Sin(imageDisplayProgress * Mathf.PI * 2) * panAmount;
            float panY = Mathf.Cos(imageDisplayProgress * Mathf.PI * 2) * panAmount;
            foregroundImage.rectTransform.localPosition = new Vector3(panX, panY, 0);
            
            // If background is visible during crossfade, set it up for its upcoming effect
            if (backgroundImage.color.a > 0)
            {
                int nextIndex = (imageIndex + 1) % imageSequence.Length;
                bool nextShouldZoomIn = (nextIndex % 2 == 0);
                
                // Set initial zoom for next image
                float nextInitialZoom = nextShouldZoomIn ? 1.0f : (1.0f + zoomAmount);
                backgroundImage.rectTransform.localScale = Vector3.one * nextInitialZoom;
                backgroundImage.rectTransform.localPosition = Vector3.zero; // Start at center
            }
        }
    }

        
    private IEnumerator FadeIn(float duration)
    {
        if (fadePanel == null)
            yield break;
            
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            fadePanel.color = new Color(0, 0, 0, 1 - t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        fadePanel.color = new Color(0, 0, 0, 0);
    }
    
    private IEnumerator FadeOut(float duration)
    {
        if (fadePanel == null)
            yield break;
            
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            fadePanel.color = new Color(0, 0, 0, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        fadePanel.color = new Color(0, 0, 0, 1);
    }

    private IEnumerator FadeInAudio(AudioSource source, AudioClip clip, float targetVolume, float duration)
    {
        if (source == null || clip == null)
            yield break;
            
        source.clip = clip;
        source.volume = 0;
        source.Play();
        
        float elapsed = 0;
        while (elapsed < duration)
        {
            source.volume = Mathf.Lerp(0, targetVolume, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        source.volume = targetVolume;
    }

    private IEnumerator FadeOutAudio(AudioSource source, float duration)
    {
        if (source == null || !source.isPlaying)
            yield break;
            
        float startVolume = source.volume;
        float elapsed = 0;
        
        while (elapsed < duration)
        {
            source.volume = Mathf.Lerp(startVolume, 0, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        source.volume = 0;
        source.Stop();
    }
}