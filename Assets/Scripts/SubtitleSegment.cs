using UnityEngine;

[System.Serializable]
public class SubtitleSegment
{
    [TextArea(2, 4)]
    public string text;
    
    public float startTime;
    public float endTime;

    public SubtitleSegment(string text, float startTime, float endTime)
    {
        this.text = text;
        this.startTime = startTime;
        this.endTime = endTime;
    }

    // Check if this segment should be active at a given time
    public bool IsActiveAt(float time)
    {
        return time >= startTime && time < endTime;
    }

    // Calculate duration
    public float Duration => endTime - startTime;
}