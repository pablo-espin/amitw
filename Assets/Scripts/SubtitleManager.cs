using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class SubtitleManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private CanvasGroup subtitleCanvasGroup;
    [SerializeField] private GameObject subtitlePanel;

    [Header("Subtitle Data")]
    [SerializeField] private List<SubtitleData> subtitleDatabase = new List<SubtitleData>();

    [Header("Settings")]
    [SerializeField] private float fadeSpeed = 5f;
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private string currentLanguage = "en";
    [SerializeField] private bool subtitlesEnabled = true;

    // Playback state
    private SubtitleData currentSubtitleData;
    private SubtitleSegment currentSegment;
    private bool isPlaying = false;
    private Coroutine fadeCoroutine;

    // Singleton
    public static SubtitleManager Instance { get; private set; }

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

        // Initial state - hide subtitles
        if (subtitlePanel != null)
            subtitlePanel.SetActive(false);
    }

    private void Update()
    {
        if (!isPlaying || currentSubtitleData == null || NarratorManager.Instance == null)
            return;

        // Check if narrator is still playing
        if (!NarratorManager.Instance.IsPlaying())
        {
            StopSubtitles();
            return;
        }

        // Get current playback time from narrator
        float currentTime = GetNarratorPlaybackTime();
        
        // Find which segment should be displayed
        SubtitleSegment activeSegment = currentSubtitleData.GetSegmentAtTime(currentTime);

        // Update subtitle if segment changed
        if (activeSegment != currentSegment)
        {
            if (activeSegment != null)
            {
                DisplaySegment(activeSegment);
            }
            else
            {
                HideSubtitle();
            }
            currentSegment = activeSegment;
        }
    }

    // Start playing subtitles for a dialogue
    public void PlaySubtitles(string dialogueID)
    {
        // Don't play if subtitles are disabled
        if (!subtitlesEnabled)
        {
            if (showDebugInfo)
                Debug.Log($"SubtitleManager: Subtitles disabled, skipping '{dialogueID}'");
            return;
        }

        // Find subtitle data for this dialogue
        SubtitleData data = GetSubtitleData(dialogueID);

        if (data == null)
        {
            if (showDebugInfo)
                Debug.LogWarning($"SubtitleManager: No subtitle data found for '{dialogueID}'");
            return;
        }

        currentSubtitleData = data;
        currentSegment = null;
        isPlaying = true;

        if (showDebugInfo)
            Debug.Log($"SubtitleManager: Started subtitles for '{dialogueID}'");
    }

    // Stop current subtitles
    public void StopSubtitles()
    {
        isPlaying = false;
        currentSubtitleData = null;
        currentSegment = null;
        HideSubtitle();

        if (showDebugInfo)
            Debug.Log("SubtitleManager: Stopped subtitles");
    }

    // Display a subtitle segment
    private void DisplaySegment(SubtitleSegment segment)
    {
        if (subtitleText != null)
        {
            subtitleText.text = segment.text;
        }

        // Fade in subtitle panel
        if (subtitlePanel != null && !subtitlePanel.activeSelf)
        {
            subtitlePanel.SetActive(true);
        }

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeIn());
    }

    // Hide subtitle
    private void HideSubtitle()
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeOut());
    }

    // Fade in animation
    private IEnumerator FadeIn()
    {
        if (subtitleCanvasGroup != null)
        {
            while (subtitleCanvasGroup.alpha < 1f)
            {
                subtitleCanvasGroup.alpha += Time.deltaTime * fadeSpeed;
                yield return null;
            }
            subtitleCanvasGroup.alpha = 1f;
        }
        fadeCoroutine = null;
    }

    // Fade out animation
    private IEnumerator FadeOut()
    {
        if (subtitleCanvasGroup != null)
        {
            while (subtitleCanvasGroup.alpha > 0f)
            {
                subtitleCanvasGroup.alpha -= Time.deltaTime * fadeSpeed;
                yield return null;
            }
            subtitleCanvasGroup.alpha = 0f;
        }

        if (subtitlePanel != null)
            subtitlePanel.SetActive(false);

        fadeCoroutine = null;
    }

    // Get subtitle data by dialogue ID
    private SubtitleData GetSubtitleData(string dialogueID)
    {
        // First try to find in database
        foreach (SubtitleData data in subtitleDatabase)
        {
            if (data.dialogueID == dialogueID && data.languageCode == currentLanguage)
            {
                return data;
            }
        }

        // If not found, try loading from JSON (for localization)
        SubtitleData loadedData = SubtitleData.LoadFromJSON(dialogueID, currentLanguage);
        if (loadedData != null)
        {
            subtitleDatabase.Add(loadedData);
            return loadedData;
        }

        // Fallback to English if current language not found
        if (currentLanguage != "en")
        {
            foreach (SubtitleData data in subtitleDatabase)
            {
                if (data.dialogueID == dialogueID && data.languageCode == "en")
                {
                    if (showDebugInfo)
                        Debug.LogWarning($"SubtitleManager: Using English fallback for '{dialogueID}'");
                    return data;
                }
            }
        }

        return null;
    }

    // Get playback time from NarratorManager
    private float GetNarratorPlaybackTime()
    {
        if (NarratorManager.Instance != null)
        {
            return NarratorManager.Instance.GetPlaybackTime();
        }
        return 0f;
    }

    // Change language (for future localization)
    public void SetLanguage(string languageCode)
    {
        currentLanguage = languageCode;
        if (showDebugInfo)
            Debug.Log($"SubtitleManager: Language set to '{languageCode}'");
    }

    // Enable or disable subtitles
    public void SetSubtitlesEnabled(bool enabled)
    {
        subtitlesEnabled = enabled;
        
        // If disabling while playing, stop current subtitles
        if (!enabled && isPlaying)
        {
            StopSubtitles();
        }
        
        if (showDebugInfo)
            Debug.Log($"SubtitleManager: Subtitles {(enabled ? "enabled" : "disabled")}");
    }

    // Get current subtitle enabled state
    public bool AreSubtitlesEnabled()
    {
        return subtitlesEnabled;
    }

    #region JSON Export Utilities (Editor use)

    // Export all subtitle data to JSON files (call from editor script or custom inspector)
    public void ExportAllToJSON()
    {
        foreach (SubtitleData data in subtitleDatabase)
        {
            string json = data.ExportToJSON();
            string path = $"Assets/Resources/Subtitles/{data.languageCode}/{data.dialogueID}.json";
            
            // Create directory if it doesn't exist
            string directory = System.IO.Path.GetDirectoryName(path);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            // Write JSON file
            System.IO.File.WriteAllText(path, json);
            Debug.Log($"Exported subtitle to: {path}");
        }

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

    #endregion
}