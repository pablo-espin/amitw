using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameHUDManager : MonoBehaviour
{
    [Header("Timer")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private float maxTime = 600f; // 10 minutes in seconds
    [SerializeField] private float timeExtensionPerCode = 180f; // 3 minutes in seconds
    private float currentTime;
    private bool isTimerRunning = true;
    private bool memoryDeleted = false;

    [Header("Decryption Interface")]
    [SerializeField] private GameObject decryptionPanel;
    [SerializeField] private TextMeshProUGUI encryptedText;
    [SerializeField] private TMP_InputField decryptionInput;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI errorText;
    [SerializeField] private TextMeshProUGUI timerExtensionText;
    [SerializeField] private float errorDisplayTime = 2f;
    [SerializeField] private float extensionDisplayTime = 3f;
    [SerializeField] private Color errorColor = Color.red;
    [SerializeField] private Color extensionColor = Color.green;

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
    [SerializeField] private string timeoutDescription = "The memory has been deleted due to security timeout and cannot be recovered.";

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
    private UIInputController uiInputController;

    // Code tracking
    private HashSet<string> usedCodes = new HashSet<string>();
    private Dictionary<string, bool> codeUsageStatus = new Dictionary<string, bool>();

    void Start()
    {
        // Initialize timer
        currentTime = maxTime;
        UpdateTimerDisplay();

        // Initialize code tracking
        usedCodes = new HashSet<string>();
        codeUsageStatus = new Dictionary<string, bool>();

        // Hide panels initially
        if (decryptionPanel != null) decryptionPanel.SetActive(false);
        if (outcomePanel != null) outcomePanel.SetActive(false);
        if (errorText != null) errorText.gameObject.SetActive(false);
        if (timerExtensionText != null) timerExtensionText.gameObject.SetActive(false);

        // Add listeners
        if (submitButton != null) submitButton.onClick.AddListener(CheckDecryption);
        if (closeButton != null) closeButton.onClick.AddListener(CloseDecryptionPanel);
        if (learnMoreButton != null) learnMoreButton.onClick.AddListener(OnLearnMoreClicked);
        if (playAgainButton != null) playAgainButton.onClick.AddListener(OnPlayAgainClicked);

        // Set encrypted text
        if (encryptedText != null) encryptedText.text = GenerateGarbledText();

        // Find references
        interactionManager = FindObjectOfType<PlayerInteractionManager>();
        uiInputController = FindObjectOfType<UIInputController>();
        
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
            
            // Clamp currentTime to 0 if it goes below
            if (currentTime < 0) 
            {
                currentTime = 0;
                if (!memoryDeleted)
                {
                    Debug.Log("Time reached zero - calling TimeUp()");
                    TimeUp();
                }
            }
            
            UpdateTimerDisplay();
        }
    }

    private void UpdateTimerDisplay()
    {
        int minutes = Mathf.FloorToInt(currentTime / 60);
        int seconds = Mathf.FloorToInt(currentTime % 60);
        
        if (timerText != null)
        {
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

            if (currentTime <= 60 && currentTime > 0) // Last minute
            {
                timerText.color = Color.red;
            }
            else if (currentTime <= 0)
            {
                // Keep showing 00:00 when timer is up
                timerText.text = "00:00";
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
        // Cursor.lockState = CursorLockMode.None;
        // Cursor.visible = true;
        // Use CursorManager instead
        // if (CursorManager.Instance != null)
        // {
        //     CursorManager.Instance.RequestCursorUnlock("DecryptionPanel");
        // }
        
        // Disable player movement
        if (interactionManager != null)
        {
            interactionManager.SetInteractionEnabled(false);
        }

        // Disable player input
        if (uiInputController != null)
        {
            uiInputController.DisableGameplayInput();
        }
    }

    public void CloseDecryptionPanel()
    {
        if (decryptionPanel != null)
        {
            decryptionPanel.SetActive(false);
        }

        // Restore cursor state
        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;

        // Use CursorManager instead
        // if (CursorManager.Instance != null)
        // {
        //     CursorManager.Instance.RequestCursorLock("DecryptionPanel");
        // }
        
        // Re-enable player movement
        if (interactionManager != null)
        {
            interactionManager.SetInteractionEnabled(true);
        }

        // Enable player input
        if (uiInputController != null)
        {
            uiInputController.EnableGameplayInput();
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

        // Check if memory is already deleted (timer reached zero)
        if (memoryDeleted)
        {
            // Show deleted memory outcome when they try to enter any code after time has expired
            CloseDecryptionPanel();
            ShowTimeoutOutcome();
            return;
        }

        // Case 1: Check if false clue was used
        if (!string.IsNullOrEmpty(falseClueCode) && input.Contains(falseClueCode))
        {
            ShowCorruptionOutcome();
            return;
        }

        // Track which codes were used and extend timer accordingly
        bool waterCodeUsed = !string.IsNullOrEmpty(waterClueCode) && input.Contains(waterClueCode);
        bool electricityCodeUsed = !string.IsNullOrEmpty(electricityClueCode) && input.Contains(electricityClueCode);
        bool locationCodeUsed = !string.IsNullOrEmpty(locationClueCode) && input.Contains(locationClueCode);
        
        int validCodesUsed = 0;
        float timeExtension = 0f;

        // Check and track each code individually
        if (waterCodeUsed && !usedCodes.Contains(waterClueCode))
        {
            usedCodes.Add(waterClueCode);
            validCodesUsed++;
            timeExtension += timeExtensionPerCode;
        }

        if (electricityCodeUsed && !usedCodes.Contains(electricityClueCode))
        {
            usedCodes.Add(electricityClueCode);
            validCodesUsed++;
            timeExtension += timeExtensionPerCode;
        }

        if (locationCodeUsed && !usedCodes.Contains(locationClueCode))
        {
            usedCodes.Add(locationClueCode);
            validCodesUsed++;
            timeExtension += timeExtensionPerCode;
        }

        // Check if all three genuine codes have been discovered and used cumulatively
        // (either in this attempt or previous attempts)
        bool allCodesFound = !string.IsNullOrEmpty(waterClueCode) && 
                            !string.IsNullOrEmpty(electricityClueCode) && 
                            !string.IsNullOrEmpty(locationClueCode);
                            
        bool allCodesUsedCumulatively = usedCodes.Contains(waterClueCode) && 
                                       usedCodes.Contains(electricityClueCode) && 
                                       usedCodes.Contains(locationClueCode);

        // If all codes have been used (even across multiple interactions), decrypt the memory
        // But only if the memory hasn't been deleted yet
        if (allCodesFound && allCodesUsedCumulatively && !memoryDeleted)
        {
            ShowSuccessOutcome();
            return;
        }

        // Extend timer if valid codes were used in this attempt
        if (validCodesUsed > 0)
        {
            ExtendTimer(timeExtension, validCodesUsed);
            StartCoroutine(ShowTimerExtensionFeedback($"Time extended by {timeExtension/60:0.0} minutes!"));
            
            // Also provide feedback that they've entered X/3 correct codes total
            int totalValidCodesUsed = usedCodes.Count;
            if (totalValidCodesUsed < 3)
            {
                StartCoroutine(ShowCodeProgressFeedback($"Valid codes: {totalValidCodesUsed}/3"));
            }
            
            decryptionInput.text = "";
        }
        else if (waterCodeUsed || electricityCodeUsed || locationCodeUsed)
        {
            // Code already used
            StartCoroutine(ShowWrongCodeFeedback("Code already used. Try another."));
        }
        else
        {
            // Wrong code entered
            StartCoroutine(ShowWrongCodeFeedback("Invalid code. Try again."));
        }
    }

    private void ExtendTimer(float extension, int codesUsed)
    {
        currentTime += extension;
        Debug.Log($"Timer extended by {extension} seconds for {codesUsed} codes");
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

    private IEnumerator ShowTimerExtensionFeedback(string message)
    {
        // Show timer extension message
        if (timerExtensionText != null)
        {
            timerExtensionText.gameObject.SetActive(true);
            timerExtensionText.text = message;
            timerExtensionText.color = extensionColor;
        }
        
        // Hide message after delay
        yield return new WaitForSeconds(extensionDisplayTime);
        if (timerExtensionText != null)
        {
            timerExtensionText.gameObject.SetActive(false);
        }
    }

    private IEnumerator ShowCodeProgressFeedback(string message)
    {
        // Show code progress message
        if (timerExtensionText != null)
        {
            timerExtensionText.gameObject.SetActive(true);
            timerExtensionText.text = message;
            timerExtensionText.color = new Color(0.2f, 0.6f, 1f); // Light blue color
        }
        
        // Hide message after delay
        yield return new WaitForSeconds(extensionDisplayTime);
        if (timerExtensionText != null)
        {
            timerExtensionText.gameObject.SetActive(false);
        }
    }

    private void TimeUp()
    {
        Debug.Log("TimeUp() method called");
        // Stop the timer
        isTimerRunning = false;
        
        // Set the deleted state
        memoryDeleted = true;
        
        // Try to get the memory sphere through GetCurrentMemorySphere first
        MemorySphere sphere = null;
        
        if (interactionManager != null)
        {
            sphere = interactionManager.GetCurrentMemorySphere();
            Debug.Log("Using interactionManager.GetCurrentMemorySphere(): " + (sphere != null ? "Found" : "Not Found"));
        }
        
        // If that didn't work, try to find it directly in the scene
        if (sphere == null)
        {
            sphere = FindObjectOfType<MemorySphere>();
            Debug.Log("Using FindObjectOfType<MemorySphere>(): " + (sphere != null ? "Found" : "Not Found"));
        }
        
        // Now try to delete it
        if (sphere != null)
        {
            sphere.Delete();
            Debug.Log("Successfully called Delete() on memory sphere");
        }
        else
        {
            Debug.LogError("Could not find memory sphere to delete by any method");
        }
        
        Debug.Log("Timer reached zero - memory silently deleted");
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
        // Show outcome panel for deleted memory
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
        // Cursor.lockState = CursorLockMode.None;
        // Cursor.visible = true;

        // Use CursorManager instead
        // if (CursorManager.Instance != null)
        // {
        //     CursorManager.Instance.RequestCursorUnlock("OutcomePanel");
        // }
        
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
        string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789@#$%^&*+øïÁ&½ÛÕµõ¬³¶Ð";
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