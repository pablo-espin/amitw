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
    
    void Start()
    {
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
            // Set up the document view
            documentImage.sprite = locationListSprite;
            documentTitleText.text = locationListTitle;
            documentViewPanel.SetActive(true);
            
            // Track that this document was examined
            locationListExamined = true;
            
            // Disable player interaction during document view
            if (interactionManager != null)
            {
                interactionManager.SetInteractionEnabled(false);
            }
            
            // Unlock cursor for UI interaction
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            // Check if both documents have been examined
            CheckClueReveal();
        }
    }
    
    public void ExamineTransportCard()
    {
        if (documentViewPanel != null && documentImage != null)
        {
            // Set up the document view
            documentImage.sprite = transportCardSprite;
            documentTitleText.text = transportCardTitle;
            documentViewPanel.SetActive(true);
            
            // Track that this document was examined
            transportCardExamined = true;
            
            // Disable player interaction during document view
            if (interactionManager != null)
            {
                interactionManager.SetInteractionEnabled(false);
            }
            
            // Unlock cursor for UI interaction
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            // Check if both documents have been examined
            CheckClueReveal();
        }
    }
    
    private void CloseDocumentView()
    {
        // Hide the document view
        if (documentViewPanel != null)
        {
            documentViewPanel.SetActive(false);
        }
        
        // Re-enable player interaction
        if (interactionManager != null)
        {
            interactionManager.SetInteractionEnabled(true);
        }
        
        // Re-lock cursor for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    private void CheckClueReveal()
    {
        // If both documents have been examined and clue not already revealed
        if (locationListExamined && transportCardExamined && !clueRevealed)
        {
            RevealClue();
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
        
        // Log for debugging
        Debug.Log("Location clue revealed: " + locationClueCode + " for " + correctLocation);
        
        // Optional: Add visual feedback that player has discovered the connection
        // This could be a small popup or highlight effect
    }
}