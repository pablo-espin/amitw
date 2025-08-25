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

    [Header("Lockdown System")]
    [SerializeField] private TextMeshProUGUI gameTimeText;
    [SerializeField] private GameObject lockdownWarningPanel;
    [SerializeField] private TextMeshProUGUI lockdownWarningText;

    [Header("Computer Code Choice")]
    [SerializeField] private GameObject computerCodeChoicePanel;
    [SerializeField] private TextMeshProUGUI computerCodeChoiceText;
    [SerializeField] private Button releaseMemoriesButton;
    [SerializeField] private Button goBackButton;

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
    [SerializeField] private string escapeTitle = "FACILITY ESCAPED";
    [SerializeField] private string escapeDescription = "Congratulations, you reached the exit. The data centre has now deleted all the memories in the world; all that information will remain forever inaccessible. Humanity will rebuild itself, surely, as it has done before.";
    [SerializeField] private string heroicLockdownTitle = "HEROIC SACRIFICE";
    [SerializeField] private string heroicLockdownDescription = "You chose to save all the memories in the world, humanity will thank you for it, if they ever know that it was you who did it. You might as well die an unknown hero, since there's no one coming for quite some time. Don't take so long next time, yeah?";
    [SerializeField] private string trappedTitle = "TRAPPED IN DARKNESS";
    [SerializeField] private string trappedDescription = "The facility has gone into full lockdown. The lights flicker and die. You hear sounds in the darkness... something else is here with you.";
    [SerializeField] private string rebelliousTitle = "MEMORIES RELEASED";
    [SerializeField] private string rebelliousDescription = "I see you followed my advice. You have chosen to release all the memories in the world. The consequences are immense, but the choice was yours to make.";

    [Header("Clue System")]
    [SerializeField] private ClueProgressUI clueProgressUI;
    
    [Header("Simplified Stats Display")]
    [SerializeField] private PowerGaugeUI powerGauge;
    [SerializeField] private MemoryHealthBar memoryHealthBar;

    [Header("Game Pause Settings")]
    [SerializeField] private bool gameIsPaused = false;
    private float originalTimeScale = 1f;
    
    // References for interaction
    private PlayerInteractionManager interactionManager;
    private MemorySphere currentMemorySphere;
    private Transform playerTransform;
    private UIInputController uiInputController;
    private LockdownManager lockdownManager;
    private bool computerCodeEntered = false;
    private int legitimateCodesEntered = 0;

    // Code tracking
    private HashSet<string> usedCodes = new HashSet<string>();
    private Dictionary<string, bool> codeUsageStatus = new Dictionary<string, bool>();
    private StatsSystem statsSystem;

    void Start()
    {
        // Initialize code tracking
        usedCodes = new HashSet<string>();
        codeUsageStatus = new Dictionary<string, bool>();

        // Hide panels initially
        if (decryptionPanel != null) decryptionPanel.SetActive(false);
        if (outcomePanel != null) outcomePanel.SetActive(false);
        if (errorText != null) errorText.gameObject.SetActive(false);
        if (timerExtensionText != null) timerExtensionText.gameObject.SetActive(false);

        // Hide new lockdown panels
        if (lockdownWarningPanel != null) lockdownWarningPanel.SetActive(false);
        if (computerCodeChoicePanel != null) computerCodeChoicePanel.SetActive(false);

        // Add listeners
        if (submitButton != null) submitButton.onClick.AddListener(CheckDecryption);
        if (closeButton != null) closeButton.onClick.AddListener(CloseDecryptionPanel);
        if (learnMoreButton != null) learnMoreButton.onClick.AddListener(OnLearnMoreClicked);
        if (playAgainButton != null) playAgainButton.onClick.AddListener(OnPlayAgainClicked);

        // Add new button listeners
        if (releaseMemoriesButton != null) releaseMemoriesButton.onClick.AddListener(OnReleaseMemoriesClicked);
        if (goBackButton != null) goBackButton.onClick.AddListener(OnGoBackClicked);

        // Set encrypted text
        if (encryptedText != null) encryptedText.text = GenerateGarbledText();

        // Find references
        interactionManager = FindObjectOfType<PlayerInteractionManager>();
        uiInputController = FindObjectOfType<UIInputController>();

        // Find lockdown manager
        lockdownManager = FindObjectOfType<LockdownManager>();
        if (lockdownManager == null)
        {
            Debug.LogError("LockdownManager not found! Make sure it exists in the scene.");
        }

        // Initialize game time display
        UpdateGameTimeDisplay();

        // Initialize simplified stats system
        InitializeStatsSystem();
    }

    private void Update()
    {
        // Update game time display
        UpdateGameTimeDisplay();
    }

    private void OnDestroy()
    {
        // Unsubscribe from memory health bar events
        if (memoryHealthBar != null)
        {
            memoryHealthBar.OnMemoriesFullyDeleted -= OnMemoriesFullyDeleted;
        }
    }

    private void UpdateGameTimeDisplay()
    {
        if (lockdownManager != null)
        {
            string currentGameTime = lockdownManager.FormatGameTime(lockdownManager.GetGameTime());

            // Update both old timer text and new game time text for compatibility
            if (timerText != null)
            {
                timerText.text = currentGameTime;
            }

            if (gameTimeText != null)
            {
                gameTimeText.text = currentGameTime;
            }

            // Change color as lockdown approaches
            float timeUntilLockdown = lockdownManager.GetLockdownTime() - lockdownManager.GetGameTime();
            Color timeColor = Color.white;

            if (timeUntilLockdown <= 120f) // Last 2 minutes
            {
                timeColor = Color.red;
            }
            else if (timeUntilLockdown <= 300f) // Last 5 minutes
            {
                timeColor = Color.yellow;
            }

            if (timerText != null) timerText.color = timeColor;
            if (gameTimeText != null) gameTimeText.color = timeColor;
        }
    }

    private void InitializeStatsSystem()
    {
        // Find or create StatsSystem
        statsSystem = StatsSystem.Instance;
        if (statsSystem == null)
        {
            GameObject statsGO = new GameObject("StatsSystem");
            statsSystem = statsGO.AddComponent<StatsSystem>();
            Debug.Log("Created new StatsSystem instance");
        }

        // Initialize power gauge
        if (powerGauge != null)
        {
            Debug.Log("PowerGauge component found and ready");
        }
        else
        {
            Debug.LogWarning("PowerGauge component not assigned in GameHUDManager!");
        }

        // Initialize memory health bar and subscribe to deletion event
        if (memoryHealthBar != null)
        {
            memoryHealthBar.OnMemoriesFullyDeleted += OnMemoriesFullyDeleted;
            Debug.Log("MemoryHealthBar component found and subscribed to deletion event");
        }
        else
        {
            Debug.LogWarning("MemoryHealthBar component not assigned in GameHUDManager!");
        }

        Debug.Log("Simplified stats system initialized");
    }

    // Show trapped ending if memory integrity reaches 0
    private void OnMemoriesFullyDeleted()
    {
        Debug.Log("All memories deleted - showing trapped ending");

        // Stop stats tracking when memories are fully deleted
        if (statsSystem != null)
        {
            statsSystem.StopStatsTracking();
        }

        ShowTrappedOutcome();
    }

    public void ShowDecryptionPanel()
    {
        if (decryptionPanel != null)
        {
            decryptionPanel.SetActive(true);

            // Register with UI state manager
            if (UIStateManager.Instance != null)
            {
                UIStateManager.Instance.RegisterOpenUI("DecryptionPanel");
            }

            if (decryptionInput != null)
            {
                decryptionInput.text = "";
                decryptionInput.ActivateInputField();
            }
        }

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

            // Unregister with UI state manager
            if (UIStateManager.Instance != null)
            {
                UIStateManager.Instance.RegisterClosedUI("DecryptionPanel");
            }
        }
        
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
        string falseClueCode = clueCodes[3];

        // Check for computer code first
        if (!string.IsNullOrEmpty(falseClueCode) && input.ToUpper().Contains(falseClueCode.ToUpper()))
        {
            computerCodeEntered = true;
            
            if (lockdownManager != null && !lockdownManager.IsLockdownStarted())
            {
                // Before lockdown - show choice
                ShowComputerCodeChoice();
            }
            else
            {
                // After lockdown - just show corruption outcome
                ShowCorruptionOutcome();
            }
            return;
        }

        // Check legitimate codes
        bool waterCodeUsed = !string.IsNullOrEmpty(waterClueCode) && input.ToUpper().Contains(waterClueCode.ToUpper());
        bool electricityCodeUsed = !string.IsNullOrEmpty(electricityClueCode) && input.ToUpper().Contains(electricityClueCode.ToUpper());
        bool locationCodeUsed = !string.IsNullOrEmpty(locationClueCode) && input.ToUpper().Contains(locationClueCode.ToUpper());
        
        int validCodesUsed = 0;
        
        // Track new codes and notify lockdown manager
        if (waterCodeUsed && !usedCodes.Contains(waterClueCode))
        {
            usedCodes.Add(waterClueCode);
            validCodesUsed++;
            legitimateCodesEntered++;
            if (lockdownManager != null) lockdownManager.OnCodeEntered();
        }

        if (electricityCodeUsed && !usedCodes.Contains(electricityClueCode))
        {
            usedCodes.Add(electricityClueCode);
            validCodesUsed++;
            legitimateCodesEntered++;
            if (lockdownManager != null) lockdownManager.OnCodeEntered();
        }

        if (locationCodeUsed && !usedCodes.Contains(locationClueCode))
        {
            usedCodes.Add(locationClueCode);
            validCodesUsed++;
            legitimateCodesEntered++;
            if (lockdownManager != null) lockdownManager.OnCodeEntered();
        }

        // Check if all three legitimate codes have been used
        bool allCodesFound = !string.IsNullOrEmpty(waterClueCode) && 
                            !string.IsNullOrEmpty(electricityClueCode) && 
                            !string.IsNullOrEmpty(locationClueCode);
                            
        bool allCodesUsedCumulatively = usedCodes.Contains(waterClueCode) && 
                                    usedCodes.Contains(electricityClueCode) && 
                                    usedCodes.Contains(locationClueCode);

        if (allCodesFound && allCodesUsedCumulatively)
        {
            if (lockdownManager != null && !lockdownManager.IsLockdownStarted())
            {
                // Before lockdown - normal success
                ShowSuccessOutcome();
            }
            else
            {
                // After lockdown - heroic ending
                ShowHeroicLockdownOutcome();
            }
            return;
        }

        // Handle partial code entry feedback
        if (validCodesUsed > 0)
        {
            // Only show time extension if lockdown hasn't started yet
            if (lockdownManager != null && !lockdownManager.IsLockdownStarted())
            {
                float extensionMinutes = validCodesUsed * 2f; // 2 minutes per code
                StartCoroutine(ShowTimerExtensionFeedback($"Lockdown delayed by {extensionMinutes:F0} minutes!"));
            }
            else
            {
                // During lockdown phases, just show code accepted message
                StartCoroutine(ShowTimerExtensionFeedback($"Valid code accepted ({validCodesUsed} codes)"));
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

    private void ShowSuccessOutcome()
    {
        // Hide decryption panel
        if (decryptionPanel != null)
        {
            decryptionPanel.SetActive(false);

            // Make sure to unregister decryption panel if it was open
            if (UIStateManager.Instance != null)
            {
                UIStateManager.Instance.RegisterClosedUI("DecryptionPanel");
            }            
        }
        
        // Decrypt memory sphere
        if (interactionManager != null)
        {
            interactionManager.DecryptCurrentSphere();
        }
        
        // Show outcome panel with regular stats
        ShowOutcomePanel(successTitle, successDescription, GenerateStats());
    }
    
    private void ShowCorruptionOutcome()
    {
        // Hide decryption panel
        if (decryptionPanel != null)
        {
            decryptionPanel.SetActive(false);

            // Make sure to unregister decryption panel if it was open
            if (UIStateManager.Instance != null)
            {
                UIStateManager.Instance.RegisterClosedUI("DecryptionPanel");
            }            
        }
        
        // Corrupt memory sphere
        if (interactionManager != null)
        {
            interactionManager.CorruptCurrentSphere();
        }
        
        // Show outcome panel with regular stats
        ShowOutcomePanel(corruptionTitle, corruptionDescription, GenerateStats());
    }
        
    private void ShowComputerCodeChoice()
    {
        if (computerCodeChoicePanel != null)
        {
            computerCodeChoicePanel.SetActive(true);
            
            // Register with UI state manager
            if (UIStateManager.Instance != null)
            {
                UIStateManager.Instance.RegisterOpenUI("ComputerCodeChoice");
            }
            
            if (computerCodeChoiceText != null)
            {
                computerCodeChoiceText.text = "You have the opportunity to release all the memories in the world, but at an immense environmental and humanitarian cost. Do you wish to proceed?";
            }
            
            // Disable other UI
            CloseDecryptionPanel();
            
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
    }

    private void OnReleaseMemoriesClicked()
    {
        // Hide choice panel
        if (computerCodeChoicePanel != null)
        {
            computerCodeChoicePanel.SetActive(false);
            
            // Unregister with UI state manager
            if (UIStateManager.Instance != null)
            {
                UIStateManager.Instance.RegisterClosedUI("ComputerCodeChoice");
            }
        }
        
        // Show rebellious outcome
        ShowRebelliousOutcome();
    }

    private void OnGoBackClicked()
    {
        // Hide choice panel
        if (computerCodeChoicePanel != null)
        {
            computerCodeChoicePanel.SetActive(false);
            
            // Unregister with UI state manager
            if (UIStateManager.Instance != null)
            {
                UIStateManager.Instance.RegisterClosedUI("ComputerCodeChoice");
            }
        }
        
        // Re-enable gameplay
        if (interactionManager != null)
        {
            interactionManager.SetInteractionEnabled(true);
        }
        
        // Re-enable player input
        if (uiInputController != null)
        {
            uiInputController.EnableGameplayInput();
        }
    }

    private void ShowTimeoutOutcome()
    {
        // Show outcome panel for deleted memory
        ShowOutcomePanel(timeoutTitle, timeoutDescription, GenerateStats());
    }
    
    public void ShowEscapeOutcome()
    {
        // Use contextual stats for escape ending
        string contextualStats = GenerateStatsForEnding(isEscapeEnding: true);
        ShowOutcomePanel(escapeTitle, escapeDescription, contextualStats);
    }

    public void ShowHeroicLockdownOutcome()
    {
        // Use contextual stats for heroic ending
        string contextualStats = GenerateStatsForEnding(isHeroicEnding: true);
        ShowOutcomePanel(heroicLockdownTitle, heroicLockdownDescription, contextualStats);
    }

    public void ShowTrappedOutcome()
    {
        ShowOutcomePanel(trappedTitle, trappedDescription, GenerateStats());
    }

    private void ShowRebelliousOutcome()
    {
        // Notify stats system of memory release (10x multiplier)
        if (statsSystem != null)
        {
            statsSystem.OnMemoryReleased();
        }

        // Generate stats before stopping tracking to get the final values with multiplier
        string finalStats = GenerateStats();
    
        // Stop stats tracking after getting final stats
        if (statsSystem != null)
        {
            statsSystem.StopStatsTracking();
        }

        ShowOutcomePanel(rebelliousTitle, rebelliousDescription, GenerateStats());
    }

    private void ShowOutcomePanel(string title, string description, string stats)
    {
        if (statsSystem != null)
        {
            statsSystem.StopStatsTracking();
        }

        if (outcomePanel != null)
        {
            outcomePanel.SetActive(true);

            // Register with UI state manager
            if (UIStateManager.Instance != null)
            {
                UIStateManager.Instance.RegisterOpenUI("OutcomePanel");
            }

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

        // Pause the game
        PauseGame();
    }

    private void PauseGame()
    {
        if (gameIsPaused) return;
        
        gameIsPaused = true;
        originalTimeScale = Time.timeScale;
        
        // Pause game simulation
        Time.timeScale = 0f;
        
        // Disable player movement and interactions
        if (interactionManager != null)
        {
            interactionManager.SetInteractionEnabled(false);
        }
        
        // Disable UI input for gameplay
        if (uiInputController != null)
        {
            uiInputController.DisableGameplayInput();
        }
        
        // Unlock cursor for outcome panel interaction
        if (CursorManager.Instance != null)
        {
            CursorManager.Instance.RequestCursorUnlock("OutcomePanel");
        }
        
        // Pause specific audio sources (keep UI sounds)
        PauseGameAudio();
        
        // Stop any ongoing timers
        if (lockdownManager != null)
        {
            lockdownManager.PauseTimer();
        }
    }

    public void ResumeGame()
    {
        if (!gameIsPaused) return;
        
        gameIsPaused = false;
        
        // Resume game simulation
        Time.timeScale = originalTimeScale;
        
        // Re-enable player movement and interactions
        if (interactionManager != null)
        {
            interactionManager.SetInteractionEnabled(true);
        }
        
        // Re-enable UI input for gameplay
        if (uiInputController != null)
        {
            uiInputController.EnableGameplayInput();
        }
        
        // Lock cursor back for gameplay
        if (CursorManager.Instance != null)
        {
            CursorManager.Instance.ForceLockCursor();
        }
        
        // Resume game audio
        ResumeGameAudio();
        
        // Resume timers
        if (lockdownManager != null)
        {
            lockdownManager.ResumeTimer();
        }
    }

    private void PauseGameAudio()
    {
        // Pause narrator and ambient sounds
        if (NarratorManager.Instance != null)
        {
            NarratorManager.Instance.PauseAudio();
        }
        
        // Pause any looping sound effects
        var audioSources = FindObjectsOfType<AudioSource>();
        foreach (var source in audioSources)
        {
            // Only pause non-UI audio sources that are currently playing
            if (source.isPlaying && !source.gameObject.name.Contains("UI"))
            {
                source.Pause();
            }
        }
    }

    private void ResumeGameAudio()
    {
        // Resume narrator and ambient sounds
        if (NarratorManager.Instance != null)
        {
            NarratorManager.Instance.ResumeAudio();
        }
        
        // Resume any paused sound effects
        var audioSources = FindObjectsOfType<AudioSource>();
        foreach (var source in audioSources)
        {
            // Only resume non-UI audio sources that were paused
            if (!source.isPlaying && !source.gameObject.name.Contains("UI"))
            {
                source.UnPause();
            }
        }
    }

    private void OnLearnMoreClicked()
    {
        Application.OpenURL(learnMoreURL);
    }
    
    private void OnPlayAgainClicked()
    {
        // Use the GameManager's restart method for proper state management
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
        else
        {
            // Fallback if GameManager isn't available
            Time.timeScale = 1f;
            if (CursorManager.Instance != null)
            {
                CursorManager.Instance.ForceLockCursor();
            }
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    // Call this if player wants to close outcome panel and continue playing
    public void CloseOutcomePanel()
    {
        if (outcomePanel != null)
        {
            outcomePanel.SetActive(false);
            
            // Unregister from UI state manager
            if (UIStateManager.Instance != null)
            {
                UIStateManager.Instance.RegisterClosedUI("OutcomePanel");
            }
        }
        
        ResumeGame();
    }

    public void OnLockdownTimeExtended(float extensionTime)
    {
        float extensionMinutes = extensionTime / 60f;
        StartCoroutine(ShowTimerExtensionFeedback($"Lockdown delayed by {extensionMinutes:F0} minutes!"));
    }

    // Generate regular stats
    private string GenerateStats()
    {
        if (statsSystem != null)
        {
            return statsSystem.GetFormattedStats();
        }
        else
        {
            return "Stats system not available";
        }
    }

    // Generate contextual stats for different endings
    private string GenerateStatsForEnding(bool isEscapeEnding = false, bool isHeroicEnding = false)
    {
        if (statsSystem != null)
        {
            return statsSystem.GetFormattedStats(isEscapeEnding, isHeroicEnding);
        }
        else
        {
            return "Stats system not available";
        }
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