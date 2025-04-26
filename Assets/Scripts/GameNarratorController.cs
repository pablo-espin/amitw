using UnityEngine;
using System.Collections.Generic;

public class GameNarratorController : MonoBehaviour
{
    [System.Serializable]
    public class NarrativeEvent
    {
        public string eventName;
        public AudioClip dialogueClip;
        public string dialogueID;
        [TextArea(2, 5)] public string dialogueText; // For debugging
        public float triggerTimeSeconds;
        public bool hasPlayed = false;
    }

    [Header("Main Game Narrative")]
    [SerializeField] private NarrativeEvent introDialogue; // Plays at start
    [SerializeField] private NarrativeEvent midGameDialogue; // Plays at 5 minutes
    [SerializeField] private NarrativeEvent finalWarningDialogue; // Plays at 8 minutes

    [Header("Settings")]
    [SerializeField] private float volume = 1f;
    [SerializeField] private bool startOnEnable = true;

    private float gameTimer = 0f;
    private bool isRunning = false;
    private List<NarrativeEvent> narrativeEvents = new List<NarrativeEvent>();

    private void Awake()
    {
        // Set up default values if not configured in inspector
        SetupDefaultValues();
        
        // Add narrative events to the list for easier management
        narrativeEvents.Add(introDialogue);
        narrativeEvents.Add(midGameDialogue);
        narrativeEvents.Add(finalWarningDialogue);
    }

    private void SetupDefaultValues()
    {
        // Set default intro dialogue if not set
        if (introDialogue.dialogueID == null || introDialogue.dialogueID.Trim() == "")
        {
            introDialogue.eventName = "Intro Dialogue";
            introDialogue.dialogueID = "intro_dialogue";
            introDialogue.dialogueText = "Welcome to the data center. Your task is to decrypt the memory before it's permanently deleted.";
            introDialogue.triggerTimeSeconds = 2f; // Play shortly after start
        }

        // Set default mid-game dialogue if not set
        if (midGameDialogue.dialogueID == null || midGameDialogue.dialogueID.Trim() == "")
        {
            midGameDialogue.eventName = "Mid-Game Warning";
            midGameDialogue.dialogueID = "mid_game_warning";
            midGameDialogue.dialogueText = "You're halfway through your available time. The memory will be deleted soon if not decrypted.";
            midGameDialogue.triggerTimeSeconds = 300f; // 5 minutes
        }

        // Set default final warning dialogue if not set
        if (finalWarningDialogue.dialogueID == null || finalWarningDialogue.dialogueID.Trim() == "")
        {
            finalWarningDialogue.eventName = "Final Warning";
            finalWarningDialogue.dialogueID = "final_warning";
            finalWarningDialogue.dialogueText = "Warning: Memory deletion imminent. Decrypt now or lose access permanently.";
            finalWarningDialogue.triggerTimeSeconds = 480f; // 8 minutes
        }
    }

    private void OnEnable()
    {
        if (startOnEnable)
            StartTimer();
    }

    private void Update()
    {
        if (!isRunning)
            return;

        gameTimer += Time.deltaTime;

        // Check for dialogues that should play
        foreach (NarrativeEvent narrativeEvent in narrativeEvents)
        {
            if (!narrativeEvent.hasPlayed && gameTimer >= narrativeEvent.triggerTimeSeconds)
            {
                PlayDialogue(narrativeEvent);
            }
        }
    }

    private void PlayDialogue(NarrativeEvent narrativeEvent)
    {
        if (NarratorManager.Instance != null && narrativeEvent.dialogueClip != null)
        {
            bool wasPlayed = NarratorManager.Instance.PlayDialogue(
                narrativeEvent.dialogueClip, 
                narrativeEvent.dialogueID, 
                false, // Don't force play
                volume
            );
            
            if (wasPlayed)
            {
                narrativeEvent.hasPlayed = true;
                Debug.Log($"Playing narrative event: {narrativeEvent.eventName} at {gameTimer} seconds");
            }
        }
        else
        {
            Debug.LogWarning($"Could not play narrative event: {narrativeEvent.eventName}. NarratorManager not found or audio clip not set.");
        }
    }

    public void StartTimer()
    {
        isRunning = true;
        Debug.Log("Game narrator timer started");
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    public void ResetTimer()
    {
        gameTimer = 0f;
        
        // Reset all event played states
        foreach (NarrativeEvent narrativeEvent in narrativeEvents)
        {
            narrativeEvent.hasPlayed = false;
        }
    }

    // Allows other systems to trigger specific narrative moments
    public void TriggerNarrativeEvent(string dialogueID)
    {
        foreach (NarrativeEvent narrativeEvent in narrativeEvents)
        {
            if (narrativeEvent.dialogueID == dialogueID && !narrativeEvent.hasPlayed)
            {
                PlayDialogue(narrativeEvent);
                return;
            }
        }
    }

    // For debugging and testing
    public float GetGameTime()
    {
        return gameTimer;
    }
    
    public bool HasEventPlayed(string dialogueID)
    {
        foreach (NarrativeEvent narrativeEvent in narrativeEvents)
        {
            if (narrativeEvent.dialogueID == dialogueID)
            {
                return narrativeEvent.hasPlayed;
            }
        }
        return false;
    }
}