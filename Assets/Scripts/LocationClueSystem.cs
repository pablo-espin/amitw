using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LocationClueSystem : MonoBehaviour
{
    [Header("State Management")]
    private bool locationListExamined = false;
    private bool transportCardExamined = false;
    private bool clueRevealed = false;
    
    [Header("Document References")]
    [SerializeField] private GameObject locationListObject;
    [SerializeField] private GameObject transportCardObject;
    
    [Header("UI Elements")]
    [SerializeField] private GameObject documentViewPanel;
    [SerializeField] private Image documentImage;
    [SerializeField] private TextMeshProUGUI documentTitleText;
    [SerializeField] private Button closeButton;
    
    [Header("Document Data")]
    [SerializeField] private Sprite locationListSprite;
    [SerializeField] private Sprite transportCardSprite;
    [SerializeField] private string locationListTitle = "Data Center Locations";
    [SerializeField] private string transportCardTitle = "Transport Card";
    
    [Header("Clue Settings")]
    [SerializeField] private string locationClueCode = "NYC-527";
    [SerializeField] private string correctLocation = "New York";
    [SerializeField] private ClueProgressUI clueProgressUI;
    
    // References for interaction
    private PlayerInteractionManager interactionManager;
    private UIInputController uiInputController;
    
    void Start()
    {
        // Get references
        uiInputController = FindObjectOfType<UIInputController>();

        // Find the interaction manager
        interactionManager = FindObjectOfType<PlayerInteractionManager>();
        
        // Set up UI
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseDocumentView);
        }
        
        // Hide document view initially
        if (documentViewPanel != null)
        {
            documentViewPanel.SetActive(false);
        }
    }
    
    public void ExamineLocationList()
    {
        if (documentViewPanel != null && documentImage != null)
        {
            // Trigger dialogue for examining the location list
            if (GameInteractionDialogueManager.Instance != null)
            {
                GameInteractionDialogueManager.Instance.OnLocationListExamined();
            }

            // Play location list examine sound
            if (InteractionSoundManager.Instance != null)
            {
                InteractionSoundManager.Instance.PlayLocationListExamine();
            }

            // Set up the document view
            documentImage.sprite = locationListSprite;
            documentTitleText.text = locationListTitle;
            documentViewPanel.SetActive(true);
            
            // Register with UI state manager
            if (UIStateManager.Instance != null)
            {
                UIStateManager.Instance.RegisterOpenUI("LocationDocument");
            }

            // Track that this document was examined
            locationListExamined = true;
            
            // Disable player interaction during document view
            if (interactionManager != null)
            {
                interactionManager.SetInteractionEnabled(false);
            }
            
            // Unlock cursor for UI interaction
            // Cursor.lockState = CursorLockMode.None;
            // Cursor.visible = true;
            
            // Use CursorManager instead
            // if (CursorManager.Instance != null)
            // {
            //     CursorManager.Instance.RequestCursorUnlock("LocationClueSystem");
            // }

            if (uiInputController != null)
            {
                uiInputController.DisableGameplayInput();
            }

            // Check if both documents have been examined
            CheckClueReveal();
        }
    }
    
    public void ExamineTransportCard()
    {
        if (documentViewPanel != null && documentImage != null)
        {
            // Play bus card examine sound
            if (InteractionSoundManager.Instance != null)
            {
                InteractionSoundManager.Instance.PlayBusCardExamine();
            }
            
            // Set up the document view
            documentImage.sprite = transportCardSprite;
            documentTitleText.text = transportCardTitle;
            documentViewPanel.SetActive(true);
            
            // Register with UI state manager
            if (UIStateManager.Instance != null)
            {
                UIStateManager.Instance.RegisterOpenUI("LocationDocument");
            }

            // Track that this document was examined
            transportCardExamined = true;

            
            // Disable player interaction during document view
            if (interactionManager != null)
            {
                interactionManager.SetInteractionEnabled(false);
            }
            
            // Unlock cursor for UI interaction
            // Cursor.lockState = CursorLockMode.None;
            // Cursor.visible = true;

            // Use CursorManager instead
            // if (CursorManager.Instance != null)
            // {
            //     CursorManager.Instance.RequestCursorUnlock("LocationClueSystem");
            // }

            // Disable player input
            if (uiInputController != null)
            {
                uiInputController.DisableGameplayInput();
            }

            // Check if both documents have been examined
            CheckClueReveal();
        }
    }
    
    public void CloseDocumentView()
    {
        // Hide the document view
        if (documentViewPanel != null)
        {
            documentViewPanel.SetActive(false);

            // Unregister with UI state manager
            if (UIStateManager.Instance != null)
            {
                UIStateManager.Instance.RegisterClosedUI("LocationDocument");
            }
        }

        // Re-enable player interaction
        if (interactionManager != null)
        {
            interactionManager.SetInteractionEnabled(true);
        }
        
        // Enable player input
        if (uiInputController != null)
        {
            uiInputController.EnableGameplayInput();
        }

        // Check if clue should be revealed now that UI is closed
        if (locationListExamined && transportCardExamined && !clueRevealed)
        {
            RevealClue();
        }
    }
    
    private void CheckClueReveal()
    {
        if (locationListExamined && transportCardExamined && !clueRevealed)
        {
            Debug.Log("Both documents examined - clue ready to reveal when UI closes");
        }
    }
    
    private void RevealClue()
    {
        clueRevealed = true;
        
        // Update progress UI
        if (clueProgressUI != null)
        {
            clueProgressUI.SolveClue("location", locationClueCode);
        }

        // Then show code found feedback
        if (ItemFoundFeedbackManager.Instance != null)
        {
            ItemFoundFeedbackManager.Instance.ShowCodeFoundSequence();
        }
        
        // Log for debugging
        Debug.Log("Location clue revealed: " + locationClueCode + " for " + correctLocation);
        
        // Optional: Add visual feedback that player has discovered the connection
        // This could be a small popup or highlight effect
    }
}