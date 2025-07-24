using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PostCaptchaDialogueTrigger : MonoBehaviour
{
    [System.Serializable]
    public class TimedPostCaptchaDialogue
    {
        public string eventName;
        public string dialogueID;
        public AudioClip dialogueClip;
        [TextArea(2, 5)] public string dialogueText; // For debugging
        public float delayAfterCaptcha = 5f; // Seconds after CAPTCHA solved
        public float volume = 1f;
        [HideInInspector] public bool hasPlayed = false;
    }

    [Header("Post-CAPTCHA Time-Based Dialogues")]
    [SerializeField] private List<TimedPostCaptchaDialogue> timedDialogues = new List<TimedPostCaptchaDialogue>();
    
    [Header("Settings")]
    [SerializeField] private bool showDebugInfo = true;
    
    // State tracking
    private bool captchaSolved = false;
    private float captchaSolvedTime = 0f;
    private bool isActive = false;
    
    // Singleton for easy access
    public static PostCaptchaDialogueTrigger Instance { get; private set; }

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (showDebugInfo)
                Debug.Log("PostCaptchaDialogueTrigger initialized");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Set up default dialogues if not configured
        SetupDefaultDialogues();
    }

    private void SetupDefaultDialogues()
    {
        if (timedDialogues.Count == 0)
        {
            // Create default dialogue entries with the specified IDs
            timedDialogues.Add(new TimedPostCaptchaDialogue
            {
                eventName = "Always On",
                dialogueID = "always_on",
                dialogueText = "Post-CAPTCHA dialogue: Always On",
                delayAfterCaptcha = 5f
            });

            timedDialogues.Add(new TimedPostCaptchaDialogue
            {
                eventName = "Leak",
                dialogueID = "leak",
                dialogueText = "Post-CAPTCHA dialogue: Leak",
                delayAfterCaptcha = 15f
            });

            timedDialogues.Add(new TimedPostCaptchaDialogue
            {
                eventName = "Leak Consequences",
                dialogueID = "leak_consequences",
                dialogueText = "Post-CAPTCHA dialogue: Leak Consequences",
                delayAfterCaptcha = 30f
            });

            timedDialogues.Add(new TimedPostCaptchaDialogue
            {
                eventName = "Second Level",
                dialogueID = "second_level",
                dialogueText = "Post-CAPTCHA dialogue: Second Level",
                delayAfterCaptcha = 60f
            });

            if (showDebugInfo)
                Debug.Log("Default post-CAPTCHA dialogues created. Assign audio clips in inspector.");
        }
    }

    private void Update()
    {
        if (!isActive || !captchaSolved)
            return;

        float timeSinceCaptcha = Time.time - captchaSolvedTime;

        // Check each timed dialogue
        foreach (TimedPostCaptchaDialogue dialogue in timedDialogues)
        {
            if (!dialogue.hasPlayed && timeSinceCaptcha >= dialogue.delayAfterCaptcha)
            {
                PlayTimedDialogue(dialogue);
            }
        }
    }

    // Called when CAPTCHA is solved
    public void OnCaptchaSolved()
    {
        if (captchaSolved)
        {
            if (showDebugInfo)
                Debug.Log("CAPTCHA already solved, ignoring duplicate call");
            return;
        }

        captchaSolved = true;
        captchaSolvedTime = Time.time;
        isActive = true;

        if (showDebugInfo)
            Debug.Log($"CAPTCHA solved at {captchaSolvedTime}. Post-CAPTCHA dialogue system activated.");

        // Reset all dialogue states for this playthrough
        foreach (TimedPostCaptchaDialogue dialogue in timedDialogues)
        {
            dialogue.hasPlayed = false;
        }
    }

    private void PlayTimedDialogue(TimedPostCaptchaDialogue dialogue)
    {
        if (NarratorManager.Instance != null && dialogue.dialogueClip != null)
        {
            bool wasPlayed = NarratorManager.Instance.PlayDialogue(
                dialogue.dialogueClip,
                dialogue.dialogueID,
                false, // Don't force play, follow normal cooldown rules
                dialogue.volume
            );

            if (wasPlayed)
            {
                dialogue.hasPlayed = true;
                if (showDebugInfo)
                    Debug.Log($"Playing post-CAPTCHA dialogue: {dialogue.eventName} ({dialogue.delayAfterCaptcha}s after CAPTCHA)");
            }
            else if (showDebugInfo)
            {
                Debug.Log($"Post-CAPTCHA dialogue '{dialogue.eventName}' blocked by cooldown/repetition rules");
            }
        }
        else if (showDebugInfo)
        {
            Debug.LogWarning($"Cannot play post-CAPTCHA dialogue '{dialogue.eventName}': NarratorManager not found or audio clip not assigned");
        }
    }

    // Check if CAPTCHA has been solved (for proximity triggers)
    public bool IsCaptchaSolved()
    {
        return captchaSolved;
    }

    // Reset system for new playthrough
    public void ResetSystem()
    {
        captchaSolved = false;
        captchaSolvedTime = 0f;
        isActive = false;

        foreach (TimedPostCaptchaDialogue dialogue in timedDialogues)
        {
            dialogue.hasPlayed = false;
        }

        if (showDebugInfo)
            Debug.Log("Post-CAPTCHA dialogue system reset");
    }

    // Debug method to manually trigger CAPTCHA solved (for testing)
    [ContextMenu("Test: Trigger CAPTCHA Solved")]
    public void TestTriggerCaptchaSolved()
    {
        OnCaptchaSolved();
    }

    // Debug method to show current state
    [ContextMenu("Debug: Show Current State")]
    public void DebugShowState()
    {
        Debug.Log($"=== Post-CAPTCHA Dialogue System State ===");
        Debug.Log($"CAPTCHA Solved: {captchaSolved}");
        Debug.Log($"System Active: {isActive}");
        if (captchaSolved)
        {
            float timeSince = Time.time - captchaSolvedTime;
            Debug.Log($"Time since CAPTCHA: {timeSince:F1} seconds");
        }
        
        Debug.Log($"Dialogue States:");
        foreach (TimedPostCaptchaDialogue dialogue in timedDialogues)
        {
            string status = dialogue.hasPlayed ? "PLAYED" : "PENDING";
            Debug.Log($"  {dialogue.eventName} ({dialogue.delayAfterCaptcha}s): {status}");
        }
        Debug.Log($"==========================================");
    }
}