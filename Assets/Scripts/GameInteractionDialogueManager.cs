using UnityEngine;

public class GameInteractionDialogueManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InteractionDialogueTrigger interactionDialogue;
    
    // Track interaction states
    private bool hasTapInteractionOccurred = false;
    private bool hasFoundFirstClue = false;
    
    // Singleton pattern
    public static GameInteractionDialogueManager Instance { get; private set; }
    
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
        }
    }
    
    private void Start()
    {
        // Find InteractionDialogueTrigger if not set
        if (interactionDialogue == null)
        {
            // Try to find an existing component first
            interactionDialogue = GetComponent<InteractionDialogueTrigger>();
            
            if (interactionDialogue == null)
            {
                // Add the component if it doesn't exist
                interactionDialogue = gameObject.AddComponent<InteractionDialogueTrigger>();
                Debug.LogWarning("InteractionDialogueTrigger not assigned, created a new one.");
            }
        }
    }
    
    // Memory sphere interaction - "Do as I say"
    public void OnMemorySphereFirstInteraction()
    {
        if (interactionDialogue != null)
            interactionDialogue.TriggerInteractionDialogue("do_as_i_say");
    }
    
    // Water tap interaction - "Sink no water"
    public void OnWaterTapWithValveClosed()
    {
        hasTapInteractionOccurred = true;
        if (interactionDialogue != null)
            interactionDialogue.TriggerInteractionDialogue("sink_no_water");
    }
    
    // Valve interaction - "Water on"
    public void OnValveOpened()
    {
        if (hasTapInteractionOccurred && interactionDialogue != null)
            interactionDialogue.TriggerInteractionDialogue("water_on");
    }
    
    // First clue found - "First clue"
    public void OnFirstClueFound(string clueType)
    {
        if (!hasFoundFirstClue && interactionDialogue != null)
        {
            hasFoundFirstClue = true;
            interactionDialogue.TriggerInteractionDialogue("first_clue");
        }
    }
    
    // Electricity clue solved - "Electricity solved"
    public void OnElectricityConnected()
    {
        if (interactionDialogue != null)
            interactionDialogue.TriggerInteractionDialogue("electricity_solved");
    }
    
    // Location list paper interaction - "Paper"
    public void OnLocationListExamined()
    {
        if (interactionDialogue != null)
            interactionDialogue.TriggerInteractionDialogue("paper_examined");
    }
    
    // CAPTCHA solved - "Captcha"
    public void OnCaptchaSolved()
    {
        if (interactionDialogue != null)
            interactionDialogue.TriggerInteractionDialogue("captcha_solved");
    }

    // Door interaction - "Need key card"
    public void OnDoorWithoutKeyCard()
    {
        if (interactionDialogue != null)
            interactionDialogue.TriggerInteractionDialogue("door_no_keycard");
    }

    // Key card used - "Access granted"
    public void OnKeyCardUsed()
    {
        if (interactionDialogue != null)
            interactionDialogue.TriggerInteractionDialogue("door_keycard_used");
    }
}