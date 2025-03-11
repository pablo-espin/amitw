using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

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

    [Header("Outcome Panel")]
    [SerializeField] private GameObject outcomePanel;
    [SerializeField] private TextMeshProUGUI outcomeTitle;
    [SerializeField] private TextMeshProUGUI outcomeDescription;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private Button learnMoreButton;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private string learnMoreURL = "https://example.com/datacenters";
    
    [Header("Outcome Messages")]
    [SerializeField] private string successTitle = "MEMORY DECRYPTED";
    [SerializeField] private string successDescription = "You have successfully recovered the memory from the data center.";
    [SerializeField] private string corruptionTitle = "MEMORY CORRUPTED";
    [SerializeField] private string corruptionDescription = "The memory has been corrupted due to invalid decryption codes.";
    [SerializeField] private string timeoutTitle = "MEMORY DELETED";
    [SerializeField] private string timeoutDescription = "The memory has been permanently deleted due to security timeout.";

    [Header("Clue System")]
    [SerializeField] private ClueProgressUI clueProgressUI;
    
    [Header("Stats Tracking")]
    private float energyUsed = 0f; // in kWh
    private float computeTimeUsed = 0f; // in minutes
    private int stepsTaken = 0;
    private Vector3 lastPosition;
    private float distanceTraveled = 0f; // in meters
    
    // References for interaction
    private PlayerInteractionManager interactionManager;
    private MemorySphere currentMemorySphere;
    private Transform playerTransform;

    private void Start()
    {
        // Initialize timer
        currentTime = maxTime;
        UpdateTimerDisplay();

        // Hide panels initially
        if (decryptionPanel != null) decryptionPanel.SetActive(false);
        if (outcomePanel != null) outcomePanel.SetActive(false);
        if (errorText != null) errorText.gameObject.SetActive(false);

        // Add listeners
        if (submitButton != null) submitButton.onClick.AddListener(CheckDecryption);
        if (closeButton != null) closeButton.onClick.AddListener(CloseDecryptionPanel);
        if (learnMoreButton != null) learnMoreButton.onClick.AddListener(OnLearnMoreClicked);
        if (playAgainButton != null) playAgainButton.onClick.AddListener(OnPlayAgainClicked);

        // Set encrypted text
        if (encryptedText != null) encryptedText.text = GenerateGarbledText();

        // Find references
        interactionManager = FindObjectOfType<PlayerInteractionManager>();
        
        // Find the player for stats tracking
        playerTransform = Camera.main.transform;
        if (playerTransform != null)
        {
            lastPosition = playerTransform.position;
        }
        
        // Start stats tracking
        InvokeRepeating("UpdateStats", 1f, 1f);
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
        
        if (timerText != null)
        {
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

            if (currentTime <= 60) // Last minute
            {
                timerText.color = Color.red;
            }
        }
    }

    public void ShowDecryptionPanel()
    {
        if (decryptionPanel != null)
        {
            decryptionPanel.SetActive(true);
            
            if (decryptionInput != null)
            {
                decryptionInput.text = "";
                decryptionInput.ActivateInputField();
            }
        }
        
        // Ensure cursor is visible and unlocked when panel is open
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Disable player movement
        if (interactionManager != null)
        {
            interactionManager.SetInteractionEnabled(false);
        }
    }

    public void CloseDecryptionPanel()
    {
        if (decryptionPanel != null)
        {
            decryptionPanel.SetActive(false);
        }
        
        // Restore cursor state
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Re-enable player movement
        if (interactionManager != null)
        {
            interactionManager.SetInteractionEnabled(true);
        }
    }

    private void CheckDecryption()
    {
        if (decryptionInput == null || string.IsNullOrEmpty(decryptionInput.text))
            return;
                
        string input = decryptionInput.text.Trim();
        
        // Get all clue codes
        string[] clueCodes = clueProgressUI.GetClueCodes();
        string waterClueCode = clueCodes[0];
        string electricityClueCode = clueCodes[1];
        string locationClueCode = clueCodes[2];
        string falseClueCode = clueCodes[3]; // Index 3 contains the false clue

        // Case 1: Check if false clue was used
        if (!string.IsNullOrEmpty(falseClueCode) && input.Contains(falseClueCode))
        {
            ShowCorruptionOutcome();
            return;
        }

        // Case 2: Check if all legitimate clues were used correctly
        bool allCodesFound = !string.IsNullOrEmpty(waterClueCode) && 
                            !string.IsNullOrEmpty(electricityClueCode) && 
                            !string.IsNullOrEmpty(locationClueCode);
                            
        bool allCodesUsed = input.Contains(waterClueCode) && 
                            input.Contains(electricityClueCode) && 
                            input.Contains(locationClueCode);

        if (allCodesFound && allCodesUsed)
        {
            ShowSuccessOutcome();
        }
        else
        {
            // Wrong code entered
            StartCoroutine(ShowWrongCodeFeedback("Invalid code. Try again."));
        }
    }

    private IEnumerator ShowWrongCodeFeedback(string message)
    {
        // Store original positions and colors
        Vector2 originalPosition = decryptionInput.transform.localPosition;
        Color originalInputColor = decryptionInput.image.color;
        
        // Show error message
        if (errorText != null)
        {
            errorText.gameObject.SetActive(true);
            errorText.text = message;
        }
        
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
        if (errorText != null)
        {
            errorText.gameObject.SetActive(false);
        }
    }

    private void TimeUp()
    {
        isTimerRunning = false;
        ShowTimeoutOutcome();
    }

    private void ShowSuccessOutcome()
    {
        isTimerRunning = false;
        
        // Hide decryption panel
        if (decryptionPanel != null)
        {
            decryptionPanel.SetActive(false);
        }
        
        // Decrypt memory sphere
        if (interactionManager != null)
        {
            interactionManager.DecryptCurrentSphere();
        }
        
        // Show outcome panel
        ShowOutcomePanel(successTitle, successDescription, GenerateStats());
    }
    
    private void ShowCorruptionOutcome()
    {
        isTimerRunning = false;
        
        // Hide decryption panel
        if (decryptionPanel != null)
        {
            decryptionPanel.SetActive(false);
        }
        
        // Corrupt memory sphere
        if (interactionManager != null)
        {
            interactionManager.CorruptCurrentSphere();
        }
        
        // Show outcome panel
        ShowOutcomePanel(corruptionTitle, corruptionDescription, GenerateStats());
    }
    
    private void ShowTimeoutOutcome()
    {
        isTimerRunning = false;
        
        // Hide any open panels
        if (decryptionPanel != null)
        {
            decryptionPanel.SetActive(false);
        }
        
        // Show outcome panel
        ShowOutcomePanel(timeoutTitle, timeoutDescription, GenerateStats());
    }
    
    private void ShowOutcomePanel(string title, string description, string stats)
    {
        if (outcomePanel != null)
        {
            outcomePanel.SetActive(true);
            
            if (outcomeTitle != null)
            {
                outcomeTitle.text = title;
            }
            
            if (outcomeDescription != null)
            {
                outcomeDescription.text = description;
            }
            
            if (statsText != null)
            {
                statsText.text = stats;
            }
        }
        
        // Unlock cursor for UI interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Disable player movement
        if (interactionManager != null)
        {
            interactionManager.SetInteractionEnabled(false);
        }
    }

    private void OnLearnMoreClicked()
    {
        Application.OpenURL(learnMoreURL);
    }
    
    private void OnPlayAgainClicked()
    {
        // Reload the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void UpdateStats()
    {
        // Update compute time (1 second = 1 computation unit)
        computeTimeUsed += 1f / 60f; // Convert to minutes
        
        // Update energy usage (estimated based on time and movement)
        energyUsed += 0.01f; // Base energy use
        
        // Update steps/distance
        if (playerTransform != null)
        {
            Vector3 currentPosition = playerTransform.position;
            float distance = Vector3.Distance(lastPosition, currentPosition);
            
            if (distance > 0.5f) // Minimum threshold to count as movement
            {
                distanceTraveled += distance;
                stepsTaken++;
            }
            
            lastPosition = currentPosition;
        }
    }
    
    private string GenerateStats()
    {
        string timeUsed = ((maxTime - currentTime) / 60f).ToString("F1");
        string energyStr = energyUsed.ToString("F2");
        string computeStr = computeTimeUsed.ToString("F1");
        string stepsStr = stepsTaken.ToString();
        string distanceStr = distanceTraveled.ToString("F1");
        
        return $"TIME SPENT: {timeUsed} minutes\n" +
               $"ENERGY CONSUMED: {energyStr} kWh\n" +
               $"COMPUTE TIME: {computeStr} minutes\n" +
               $"STEPS TAKEN: {stepsStr}\n" +
               $"DISTANCE TRAVELED: {distanceStr} meters";
    }

    private string GenerateGarbledText()
    {
        // Generate random characters to represent encrypted text
        string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789@#$%^&*";
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