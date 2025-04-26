using UnityEngine;
using System.Collections.Generic;

public class InteractionDialogueTrigger : MonoBehaviour
{
    [System.Serializable]
    public class InteractionDialogue
    {
        public string eventName;
        public string interactionID;
        public AudioClip dialogueClip;
        public string dialogueID;
        [TextArea(2, 5)] public string dialogueText; // For debugging
        public bool hasPlayed = false;
        public float volume = 1f;
    }

    [Header("Dialogue Settings")]
    [SerializeField] private List<InteractionDialogue> interactionDialogues = new List<InteractionDialogue>();
    [SerializeField] private bool showDebugInfo = true;

    private Dictionary<string, InteractionDialogue> dialogueLookup = new Dictionary<string, InteractionDialogue>();

    private void Awake()
    {
        // Build dictionary for quick lookup
        foreach (InteractionDialogue dialogue in interactionDialogues)
        {
            if (!string.IsNullOrEmpty(dialogue.interactionID))
            {
                dialogueLookup[dialogue.interactionID] = dialogue;
            }
        }
    }

    // Call this method from other scripts when an interaction occurs
    public bool TriggerInteractionDialogue(string interactionID)
    {
        if (string.IsNullOrEmpty(interactionID))
            return false;

        if (dialogueLookup.TryGetValue(interactionID, out InteractionDialogue dialogue))
        {
            if (dialogue.hasPlayed)
            {
                if (showDebugInfo)
                    Debug.Log($"Interaction dialogue '{dialogue.eventName}' has already played.");
                return false;
            }

            if (NarratorManager.Instance != null && dialogue.dialogueClip != null)
            {
                bool wasPlayed = NarratorManager.Instance.PlayDialogue(
                    dialogue.dialogueClip, 
                    dialogue.dialogueID, 
                    false, 
                    dialogue.volume
                );
                
                if (wasPlayed)
                {
                    dialogue.hasPlayed = true;
                    if (showDebugInfo)
                        Debug.Log($"Playing interaction dialogue: {dialogue.eventName}");
                    return true;
                }
            }
            else if (showDebugInfo)
            {
                Debug.LogWarning($"Failed to play interaction dialogue '{dialogue.eventName}': NarratorManager not found or audio clip not assigned.");
            }
        }
        else if (showDebugInfo)
        {
            Debug.LogWarning($"No interaction dialogue found with ID: {interactionID}");
        }

        return false;
    }

    // Check if a specific interaction dialogue has already played
    public bool HasInteractionPlayed(string interactionID)
    {
        if (dialogueLookup.TryGetValue(interactionID, out InteractionDialogue dialogue))
        {
            return dialogue.hasPlayed;
        }
        return false;
    }

    // Reset a specific interaction to allow it to play again
    public void ResetInteraction(string interactionID)
    {
        if (dialogueLookup.TryGetValue(interactionID, out InteractionDialogue dialogue))
        {
            dialogue.hasPlayed = false;
        }
    }

    // Reset all interactions
    public void ResetAllInteractions()
    {
        foreach (InteractionDialogue dialogue in interactionDialogues)
        {
            dialogue.hasPlayed = false;
        }
    }
}