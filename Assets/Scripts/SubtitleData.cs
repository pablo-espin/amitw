using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SubtitleData", menuName = "Subtitles/Subtitle Data")]
public class SubtitleData : ScriptableObject
{
    [Header("Dialogue Reference")]
    [Tooltip("Must match the dialogueID used in NarratorManager")]
    public string dialogueID;
    
    [Tooltip("Reference to the audio clip (optional, for editor convenience)")]
    public AudioClip audioClip;

    [Header("Subtitle Segments")]
    [Tooltip("List of subtitle segments with timing")]
    public List<SubtitleSegment> segments = new List<SubtitleSegment>();

    [Header("Localization")]
    [Tooltip("Language code (e.g., 'en', 'es', 'fr')")]
    public string languageCode = "en";

    // Get the active segment at a specific time
    public SubtitleSegment GetSegmentAtTime(float time)
    {
        foreach (SubtitleSegment segment in segments)
        {
            if (segment.IsActiveAt(time))
            {
                return segment;
            }
        }
        return null;
    }

    // Get total duration of all segments
    public float GetTotalDuration()
    {
        if (segments.Count == 0) return 0f;
        
        float maxEndTime = 0f;
        foreach (SubtitleSegment segment in segments)
        {
            if (segment.endTime > maxEndTime)
                maxEndTime = segment.endTime;
        }
        return maxEndTime;
    }

    #region JSON Import/Export for Localization

    [System.Serializable]
    public class SubtitleDataJSON
    {
        public string dialogueID;
        public string languageCode;
        public List<SubtitleSegmentJSON> segments;
    }

    [System.Serializable]
    public class SubtitleSegmentJSON
    {
        public string text;
        public float startTime;
        public float endTime;
    }

    // Export to JSON string
    public string ExportToJSON()
    {
        SubtitleDataJSON jsonData = new SubtitleDataJSON
        {
            dialogueID = this.dialogueID,
            languageCode = this.languageCode,
            segments = new List<SubtitleSegmentJSON>()
        };

        foreach (SubtitleSegment segment in segments)
        {
            jsonData.segments.Add(new SubtitleSegmentJSON
            {
                text = segment.text,
                startTime = segment.startTime,
                endTime = segment.endTime
            });
        }

        return JsonUtility.ToJson(jsonData, true);
    }

    // Import from JSON string
    public void ImportFromJSON(string json)
    {
        SubtitleDataJSON jsonData = JsonUtility.FromJson<SubtitleDataJSON>(json);
        
        this.dialogueID = jsonData.dialogueID;
        this.languageCode = jsonData.languageCode;
        this.segments.Clear();

        foreach (SubtitleSegmentJSON segmentJSON in jsonData.segments)
        {
            segments.Add(new SubtitleSegment(
                segmentJSON.text,
                segmentJSON.startTime,
                segmentJSON.endTime
            ));
        }
    }

    // Load from JSON file in Resources folder
    public static SubtitleData LoadFromJSON(string dialogueID, string languageCode)
    {
        string path = $"Subtitles/{languageCode}/{dialogueID}";
        TextAsset jsonFile = Resources.Load<TextAsset>(path);
        
        if (jsonFile != null)
        {
            SubtitleData data = CreateInstance<SubtitleData>();
            data.ImportFromJSON(jsonFile.text);
            return data;
        }
        
        Debug.LogWarning($"Subtitle JSON not found: {path}");
        return null;
    }

    #endregion
}