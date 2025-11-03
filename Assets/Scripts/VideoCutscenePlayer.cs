using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using System.Collections;

/// <summary>
/// Plays a video cutscene with skip functionality and background music
/// </summary>
public class VideoCutscenePlayer : MonoBehaviour
{
    [Header("Video Settings")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private VideoClip videoClip;
    [SerializeField] private RawImage videoDisplay; // UI element to display the video
    [SerializeField] private RenderTexture videoRenderTexture; // Optional: create at runtime if null
    
    [Header("Background Music")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private float musicVolume = 0.5f;
    [SerializeField] private float musicFadeInDuration = 2.0f;
    [SerializeField] private float musicFadeOutDuration = 3.0f;
    
    [Header("Transitions")]
    [SerializeField] private Image fadePanel;
    [SerializeField] private float initialFadeInTime = 1.0f;
    [SerializeField] private float finalFadeOutTime = 1.5f;
    [SerializeField] private float delayBeforeGameStart = 2.0f;
    
    [Header("Skip Functionality")]
    [SerializeField] private CutsceneSkipManager skipManager;
    
    // State tracking
    private bool isCutsceneStopped = false;
    private bool isVideoReady = false;
    
    private void Awake()
    {
        // Initialize fade panel
        if (fadePanel != null)
        {
            fadePanel.color = new Color(0, 0, 0, 1); // Start with black screen
        }
        
        // Hide video display initially
        if (videoDisplay != null)
        {
            videoDisplay.color = new Color(1, 1, 1, 0);
        }
    }
    
    private void Start()
    {
        // Force lock cursor
        if (CursorManager.Instance != null)
        {
            CursorManager.Instance.ForceLockCursor();
        }
        
        // Setup music source if needed
        SetupAudioSource();
        
        // Setup video player
        SetupVideoPlayer();
        
        // Start the cutscene
        StartCoroutine(PlayCutscene());
    }
    
    private void SetupAudioSource()
    {
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }
        
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.volume = 0f;
    }
    
    private void SetupVideoPlayer()
    {
        // Create or setup video player
        if (videoPlayer == null)
        {
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
        }
        
        // Configure video player
        videoPlayer.playOnAwake = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = videoClip;
        videoPlayer.isLooping = false;
        videoPlayer.skipOnDrop = true;
        
        // Create render texture if not assigned
        if (videoRenderTexture == null && videoDisplay != null)
        {
            // Create render texture with resolution matching video
            int width = 1920;
            int height = 1080;
            
            videoRenderTexture = new RenderTexture(width, height, 0);
            videoRenderTexture.name = "CutsceneVideoRT";
        }
        
        // Assign render texture
        videoPlayer.targetTexture = videoRenderTexture;
        
        if (videoDisplay != null)
        {
            videoDisplay.texture = videoRenderTexture;
        }
        
        // Subscribe to video events
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.loopPointReached += OnVideoFinished;
        
        // Prepare the video
        videoPlayer.Prepare();
    }
    
    private void OnVideoPrepared(VideoPlayer source)
    {
        isVideoReady = true;
        Debug.Log("Video prepared and ready to play");
    }
    
    private void OnVideoFinished(VideoPlayer source)
    {
        if (!isCutsceneStopped)
        {
            Debug.Log("Video finished playing naturally");
        }
    }
    
    private IEnumerator PlayCutscene()
    {
        // Validate setup
        if (!ValidateSetup())
        {
            Debug.LogError("Cutscene setup validation failed!");
            yield break;
        }
        
        // Wait for video to be prepared
        while (!isVideoReady)
        {
            yield return null;
        }
        
        // Start background music
        if (musicSource != null && backgroundMusic != null)
        {
            StartCoroutine(FadeInAudio(musicSource, backgroundMusic, musicVolume, musicFadeInDuration));
        }
        
        // Initial fade in
        yield return StartCoroutine(FadeIn(initialFadeInTime));
        
        // Check for skip
        if (isCutsceneStopped) yield break;
        
        // Show video display
        if (videoDisplay != null)
        {
            videoDisplay.color = new Color(1, 1, 1, 1);
        }
        
        // Play the video
        videoPlayer.Play();
        
        // Wait for video to finish or skip
        while (videoPlayer.isPlaying && !isCutsceneStopped)
        {
            yield return null;
        }
        
        // Check for skip
        if (isCutsceneStopped) yield break;
        
        // Additional delay before ending cutscene
        yield return StartCoroutine(WaitWithSkipCheck(delayBeforeGameStart));
        
        // Check for skip
        if (isCutsceneStopped) yield break;
        
        // Disable skip functionality since cutscene is ending naturally
        if (skipManager != null)
        {
            skipManager.DisableSkip();
        }
        
        // Fade out background music
        if (musicSource != null && musicSource.isPlaying)
        {
            StartCoroutine(FadeOutAudio(musicSource, musicFadeOutDuration));
        }
        
        // Final fade out
        yield return StartCoroutine(FadeOut(finalFadeOutTime));
        
        // Load game level
        LoadGameLevel();
    }
    
    private bool ValidateSetup()
    {
        if (videoClip == null)
        {
            Debug.LogError("VideoCutscenePlayer: Video clip is not assigned!");
            return false;
        }
        
        if (videoDisplay == null)
        {
            Debug.LogError("VideoCutscenePlayer: Video display RawImage is not assigned!");
            return false;
        }
        
        if (fadePanel == null)
        {
            Debug.LogError("VideoCutscenePlayer: Fade panel is not assigned!");
            return false;
        }
        
        return true;
    }
    
    private IEnumerator WaitWithSkipCheck(float waitTime)
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < waitTime)
        {
            if (isCutsceneStopped) yield break;
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }
    
    private IEnumerator FadeIn(float duration)
    {
        if (fadePanel == null) yield break;
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (isCutsceneStopped) yield break;
            
            float t = elapsed / duration;
            fadePanel.color = new Color(0, 0, 0, 1 - t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (!isCutsceneStopped)
        {
            fadePanel.color = new Color(0, 0, 0, 0);
        }
    }
    
    private IEnumerator FadeOut(float duration)
    {
        if (fadePanel == null) yield break;
        
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
        if (source == null || clip == null) yield break;
        
        source.clip = clip;
        source.volume = 0;
        source.Play();
        
        float elapsed = 0;
        while (elapsed < duration)
        {
            if (isCutsceneStopped) yield break;
            
            source.volume = Mathf.Lerp(0, targetVolume, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (!isCutsceneStopped)
        {
            source.volume = targetVolume;
        }
    }
    
    private IEnumerator FadeOutAudio(AudioSource source, float duration)
    {
        if (source == null || !source.isPlaying) yield break;
        
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
    
    public void StopCutscene()
    {
        isCutsceneStopped = true;
        
        // Stop video playback
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }
        
        // Fade out music
        if (musicSource != null && musicSource.isPlaying)
        {
            StartCoroutine(FadeOutAudio(musicSource, 0.5f));
        }
        
        // Stop all running coroutines except the fade out
        StopAllCoroutines();
    }
    
    private void LoadGameLevel()
    {
        // Use GameManager if available
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGameplay();
        }
        else
        {
            // Fallback
            SceneManager.LoadScene("GameLevel");
        }
    }
    
    private void OnDestroy()
    {
        // Cleanup video player events
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
        
        // Cleanup render texture if created at runtime
        if (videoRenderTexture != null)
        {
            videoRenderTexture.Release();
        }
    }
}