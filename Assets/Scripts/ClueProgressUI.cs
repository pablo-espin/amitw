using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ClueProgressUI : MonoBehaviour
{
    [Header("Memory Sphere Icon")]
    [SerializeField] private Image memorySphereIcon;
    [SerializeField] private Button memorySphereButton;
  
    [Header("Clue Panel")]
    [SerializeField] private GameObject cluePanel;
    [SerializeField] private Button closePanelButton;
    
    [Header("Clue Status")]
    [SerializeField] private Image waterClueIcon;
    [SerializeField] private Image electricityClueIcon;
    [SerializeField] private Image locationClueIcon;
    [SerializeField] private Image falseClueIcon;
    
    [SerializeField] private TextMeshProUGUI waterClueText;
    [SerializeField] private TextMeshProUGUI electricityClueText;
    [SerializeField] private TextMeshProUGUI locationClueText;
    [SerializeField] private TextMeshProUGUI falseClueText;
    
    [Header("Clue Settings")]
    [SerializeField] private Color unsolvedColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Gray, semi-transparent
    [SerializeField] private Color solvedColor = Color.white;
    [SerializeField] private string blurredTextPlaceholder = "███-███";
    
    // Clue states
    private bool waterClueSolved = false;
    private bool electricityClueSolved = false;
    private bool locationClueSolved = false;
    private bool falseClueDiscovered = false;
    
    // Clue codes (will be set when clues are solved)
    private string waterClueCode = "";
    private string electricityClueCode = "";
    private string locationClueCode = "";
    private string falseClueCode = "";
    
    // Private variables
    private bool isPanelOpen = false;
    private int solvedClueCount = 0;
    
    void Start()
    {
        // Set up button listeners
        closePanelButton.onClick.AddListener(CloseCluePanel);
        
        // Initialize UI state
        cluePanel.SetActive(false);
        
        // Initialize clue icons and text
        UpdateClueVisuals();
        
        // Hide false clue initially
        falseClueIcon.gameObject.SetActive(false);
        falseClueText.gameObject.SetActive(false);
    }
        
    // Close the clue panel
    public void CloseCluePanel()
    {
        isPanelOpen = false;
        cluePanel.SetActive(false);
    }
    
    // Call this when a clue is solved
    public void SolveClue(string clueType, string clueCode)
    {
        switch (clueType.ToLower())
        {
            case "water":
                waterClueSolved = true;
                waterClueCode = clueCode;
                break;
                
            case "electricity":
                electricityClueSolved = true;
                electricityClueCode = clueCode;
                break;
                
            case "location":
                locationClueSolved = true;
                locationClueCode = clueCode;
                break;
                
            case "false":
                falseClueDiscovered = true;
                falseClueCode = clueCode;
                // Show false clue elements
                falseClueIcon.gameObject.SetActive(true);
                falseClueText.gameObject.SetActive(true);
                break;
        }
        
        // Trigger first clue found dialogue
        if (GameInteractionDialogueManager.Instance != null)
        {
            int previousSolvedCount = solvedClueCount;
            
            // Update solved clue count (excluding false clue)
            solvedClueCount = (waterClueSolved ? 1 : 0) + 
                            (electricityClueSolved ? 1 : 0) + 
                            (locationClueSolved ? 1 : 0);
            
            // If this is the first solved clue (excluding false clue)
            if (previousSolvedCount == 0 && solvedClueCount == 1)
            {
                GameInteractionDialogueManager.Instance.OnFirstClueFound(clueType);
            }
        }
        else
        {
            // Update solved clue count (excluding false clue)
            solvedClueCount = (waterClueSolved ? 1 : 0) + 
                            (electricityClueSolved ? 1 : 0) + 
                            (locationClueSolved ? 1 : 0);
        }
        
        // Update intensity of memory sphere icon based on progress
        Color iconColor = memorySphereIcon.color;
        iconColor.a = 0.3f + (0.7f * (solvedClueCount / 3f));
        memorySphereIcon.color = iconColor;
        
        // Update UI visuals
        UpdateClueVisuals();
    }
    
    // Update the visual state of clue icons and text
    private void UpdateClueVisuals()
    {
        // Update icons
        waterClueIcon.color = waterClueSolved ? solvedColor : unsolvedColor;
        electricityClueIcon.color = electricityClueSolved ? solvedColor : unsolvedColor;
        locationClueIcon.color = locationClueSolved ? solvedColor : unsolvedColor;
        if (falseClueDiscovered)
        {
            falseClueIcon.color = solvedColor;
        }
        
        // Update text
        waterClueText.text = waterClueSolved ? waterClueCode : blurredTextPlaceholder;
        electricityClueText.text = electricityClueSolved ? electricityClueCode : blurredTextPlaceholder;
        locationClueText.text = locationClueSolved ? locationClueCode : blurredTextPlaceholder;
        falseClueText.text = falseClueDiscovered ? falseClueCode : "";
    }
    
    // Return all clue codes for memory decryption
    public string[] GetClueCodes()
    {
        return new string[] 
        {
            waterClueSolved ? waterClueCode : "",
            electricityClueSolved ? electricityClueCode : "",
            locationClueSolved ? locationClueCode : "",
            falseClueDiscovered ? falseClueCode : ""
        };
    }
    
    // Check if all legitimate clues are solved
    public bool AreAllCluesSolved()
    {
        return waterClueSolved && electricityClueSolved && locationClueSolved;
    }

    // Check if false clue is discovered
    public bool IsFalseClueDiscovered()
    {
        return falseClueDiscovered;
    }
    
    public int GetDiscoveredClueCount()
    {
        int count = 0;
        
        if (waterClueSolved) count++;
        if (electricityClueSolved) count++;
        if (locationClueSolved) count++;
        
        return count;
    }
}