using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class GameHUDManager : MonoBehaviour
{
    [Header("Timer")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private float maxTime = 300f; // 5 minutes in seconds
    private float currentTime;
    private bool isTimerRunning = true;

    [Header("Decryption Interface")]
    [SerializeField] private GameObject decryptionPanel;
    [SerializeField] private TextMeshProUGUI encryptedText;
    [SerializeField] private TMP_InputField decryptionInput;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI errorText;
    [SerializeField] private float errorDisplayTime = 2f;
    [SerializeField] private Color errorColor = Color.red;

    [Header("Outcome Messages")]
    [SerializeField] private GameObject outcomePanel;
    [SerializeField] private TextMeshProUGUI outcomeText;
    [SerializeField] private Button learnMoreButton;

    [Header("Clue System")]
    [SerializeField] private ClueProgressUI clueProgressUI;
    [SerializeField] private bool requireAllCluesForDecryption = true;
    
    private PlayerInteractionManager playerInteraction;

    private void Start()
    {
        // Initialize timer
        currentTime = maxTime;
        UpdateTimerDisplay();

        // Hide panels initially
        decryptionPanel.SetActive(false);
        outcomePanel.SetActive(false);
        if (errorText != null) errorText.gameObject.SetActive(false);

        // Add listeners
        submitButton.onClick.AddListener(CheckDecryption);
        closeButton.onClick.AddListener(CloseDecryptionPanel);
        learnMoreButton.onClick.AddListener(OnLearnMoreClicked);

        // Set encrypted text
        encryptedText.text = GenerateGarbledText();

        playerInteraction = FindObjectOfType<PlayerInteractionManager>();
    }

    private void Update()
    {
        if (isTimerRunning)
        {
            currentTime -= Time.deltaTime;
            UpdateTimerDisplay();

            if (currentTime <= 0)
            {
                TimeUp();
            }
        }
    }

    private void UpdateTimerDisplay()
    {
        int minutes = Mathf.FloorToInt(currentTime / 60);
        int seconds = Mathf.FloorToInt(currentTime % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

        if (currentTime <= 60) // Last minute
        {
            timerText.color = Color.red;
        }
    }

    public void ShowDecryptionPanel()
    {
        decryptionPanel.SetActive(true);
        decryptionInput.text = "";
        
        // Ensure cursor is visible and unlocked
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseDecryptionPanel()
    {
        decryptionPanel.SetActive(false);
    }

    private void CheckDecryption()
    {
        // Check if player has found clues
        if (requireAllCluesForDecryption && !clueProgressUI.AreAllCluesSolved())
        {
            // If all clues required but not found
            StartCoroutine(ShowWrongCodeFeedback("Missing clues. Find all clues first."));
            return;
        }
        
        string[] clueCodes = clueProgressUI.GetClueCodes();
        string inputCode = decryptionInput.text;
        
        // Option 1: Success - all legitimate clues used
        if (IsCorrectDecryptionCode(inputCode, clueCodes))
        {
            ShowOutcome("Success! Memory decrypted");
            playerInteraction.DecryptCurrentSphere();
        }
        // Option 2: Corruption - false clue used
        else if (ContainsFalseClue(inputCode, clueCodes[3]))
        {
            ShowOutcome("Decryption failed. Memory corrupted");
            playerInteraction.CorruptCurrentSphere(); // You'll need to add this method
        }
        // Option 3: Wrong code entered
        else
        {
            StartCoroutine(ShowWrongCodeFeedback("Invalid code. Please Try again"));
        }
    }
    
    private bool IsCorrectDecryptionCode(string inputCode, string[] clueCodes)
    {
        // This method would check if the input code correctly combines the three legitimate clues
        // For now, a simple implementation - modify based on your specific requirements
        string correctCombination = CombineLegitimateClueCodes(clueCodes);
        return inputCode == correctCombination;
    }
    
    private string CombineLegitimateClueCodes(string[] clueCodes)
    {
        // Method to combine the three legitimate clue codes (first three in the array)
        // This is a simplified example - customize as needed
        return clueCodes[0] + clueCodes[1] + clueCodes[2];
    }
    
    private bool ContainsFalseClue(string inputCode, string falseClue)
    {
        // Check if the input contains the false clue
        return !string.IsNullOrEmpty(falseClue) && inputCode.Contains(falseClue);
    }

    private IEnumerator ShowWrongCodeFeedback(string message)
    {
        // Store original positions and colors
        Vector2 originalPosition = decryptionInput.transform.localPosition;
        Color originalInputColor = decryptionInput.image.color;
        
        // Show error message
        errorText.gameObject.SetActive(true);
        errorText.text = message;
        
        // Shake effect
        float elapsed = 0f;
        float duration = 0.5f;
        float magnitude = 5f;
        
        // Change input field color
        decryptionInput.image.color = errorColor;
        
        while (elapsed < duration)
        {
            float x = originalPosition.x + Random.Range(-1f, 1f) * magnitude;
            float y = originalPosition.y + Random.Range(-1f, 1f) * magnitude;
            
            decryptionInput.transform.localPosition = new Vector2(x, y);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Reset position and color
        decryptionInput.transform.localPosition = originalPosition;
        decryptionInput.image.color = originalInputColor;
        decryptionInput.text = "";
        
        // Hide error message after delay
        yield return new WaitForSeconds(errorDisplayTime);
        errorText.gameObject.SetActive(false);
    }

    private void TimeUp()
    {
        isTimerRunning = false;
        ShowOutcome("Memory permanently deleted. Time expired.");
    }

    private void ShowOutcome(string message)
    {
        isTimerRunning = false;
        decryptionPanel.SetActive(false);
        outcomePanel.SetActive(true);
        outcomeText.text = message;
    }

    private void OnLearnMoreClicked()
    {
        Application.OpenURL("YOUR_URL_HERE");
    }

    private string GenerateGarbledText()
    {
        // Generate random characters to represent encrypted text
        string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789@#$%^&*ã¥Ñƒœ£^&*Žl□|ñ³ÐÖ%$‰¾½¼‡¶™Ë[úã€";
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        System.Random random = new System.Random();

        for (int i = 0; i < 200; i++)
        {
            result.Append(characters[random.Next(characters.Length)]);
            if (i % 50 == 0 && i > 0) result.Append('\n');
        }

        return result.ToString();
    }
}