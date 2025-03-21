using UnityEngine;
using System.Collections;

public class TimedDialogueTrigger : MonoBehaviour
{
    [System.Serializable]
    public class TimedDialogue
    {
        public AudioClip dialogueClip;
        public string dialogueID;
        public float triggerTimeSeconds; // Time in seconds when this should play
        [TextArea(2, 5)] public string dialogueText; // For debugging
        public bool hasPlayed = false;
    }
    
    [Header("Dialogue Settings")]
    [SerializeField] private TimedDialogue[] dialogues;
    [SerializeField] private float volume = 1f;
    [SerializeField] private bool startOnEnable = true;
    
    private float gameTimer = 0f;
    private bool isRunning = false;
    
    private void OnEnable()
    {
        if (startOnEnable)
            StartTimer();
    }
    
    private void OnDisable()
    {
        StopTimer();
    }
    
    private void Update()
    {
        if (!isRunning)
            return;
            
        gameTimer += Time.deltaTime;
        
        // Check for dialogues that should play
        foreach (TimedDialogue dialogue in dialogues)
        {
            if (!dialogue.hasPlayed && gameTimer >= dialogue.triggerTimeSeconds)
            {
                PlayDialogue(dialogue);
            }
        }
    }
    
    private void PlayDialogue(TimedDialogue dialogue)
    {
        if (NarratorManager.Instance != null && dialogue.dialogueClip != null)
        {
            bool wasPlayed = NarratorManager.Instance.PlayDialogue(dialogue.dialogueClip, dialogue.dialogueID, false, volume);
            dialogue.hasPlayed = wasPlayed;
        }
    }
    
    public void StartTimer()
    {
        isRunning = true;
    }
    
    public void StopTimer()
    {
        isRunning = false;
    }
    
    public void ResetTimer()
    {
        gameTimer = 0f;
        
        // Reset all dialogues
        foreach (TimedDialogue dialogue in dialogues)
        {
            dialogue.hasPlayed = false;
        }
    }
    
    // For debugging
    public float GetCurrentTime()
    {
        return gameTimer;
    }
}