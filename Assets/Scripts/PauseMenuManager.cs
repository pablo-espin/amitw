using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class PauseMenuManager : MonoBehaviour
{
    [Header("Pause Menu UI")]
    [SerializeField] private GameObject pauseMenuPanel;

    [Header("Close Button")]
    [SerializeField] private Button closeButton;

    [Header("Volume Control")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private TextMeshProUGUI volumeValueText;

    [Header("Controls Text")]
    [SerializeField] private TextMeshProUGUI controlsText;

    [Header("Buttons")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button exitButton;

    // References to managers
    private UIInputController uiInputController;
    private InteractionSoundManager interactionSoundManager;
    private NarratorManager narratorManager;
    private RoomToneManager roomToneManager;
    private UISoundManager uiSoundManager;

    // State tracking
    private bool isPaused = false;
    private float timeScaleBeforePause = 1f;

    private void Awake()
    {
        // Find required references
        uiInputController = FindObjectOfType<UIInputController>();
        interactionSoundManager = FindObjectOfType<InteractionSoundManager>();
        narratorManager = FindObjectOfType<NarratorManager>();
        roomToneManager = FindObjectOfType<RoomToneManager>();
        uiSoundManager = FindObjectOfType<UISoundManager>();

        // Setup UI elements
        SetupUI();
    }

    private void SetupUI()
    {
        // Ensure pause menu is hidden initially
        if (pauseMenuPanel)
            pauseMenuPanel.SetActive(false);

        // Add button listeners
        if (closeButton)
            closeButton.onClick.AddListener(ResumeGame);

        if (restartButton)
            restartButton.onClick.AddListener(RestartGame);

        if (exitButton)
            exitButton.onClick.AddListener(ExitGame);

        // Setup controls text if not already set
        if (controlsText && string.IsNullOrEmpty(controlsText.text))
        {
            controlsText.text = "CONTROLS:\n\n" +
                                "WASD - Move\n" +
                                "Mouse - Look around\n" +
                                "Shift - Run\n" +
                                "E - Interact\n" +
                                "P - Toggle pause menu\n" +
                                "M - Show map";
        }

        // Setup volume slider
        if (masterVolumeSlider)
        {
            // Get current volume
            float currentVolume = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
            masterVolumeSlider.value = currentVolume;
            UpdateVolumeText(currentVolume);

            // Add listener
            masterVolumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }
    }

    // private void Update()
    // {
    //     // Check for pause key press (P key)
    //     if (Input.GetKeyDown(KeyCode.P))
    //     {
    //         TogglePause();
    //     }

    //     // Also allow ESC to close pause menu if it's open
    //     // if (isPaused && Input.GetKeyDown(KeyCode.Escape))
    //     // {
    //     //     ResumeGame();
    //     // }
    // }

    private void Update()
    {
        // Check for pause key press (P key)
        if (Input.GetKeyDown(KeyCode.P))
        {
            // Only toggle pause if no other UI is open
            if (!isPaused && UIStateManager.Instance != null && UIStateManager.Instance.IsAnyUIOpen)
            {
                // Don't pause if another UI is open
                Debug.Log("Pause key pressed, but another UI is open. Ignoring.");
                return;
            }
            
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        if (isPaused)
            return;

        // Store time scale before pausing
        timeScaleBeforePause = Time.timeScale;

        // Pause the game
        Time.timeScale = 0f;
        isPaused = true;

        // Show pause menu
        if (pauseMenuPanel)
            pauseMenuPanel.SetActive(true);

        // Disable player input and show cursor
        DisableGameplay();

        // Pause audio
        PauseAudio();
        
        // Register with UI state manager
        if (UIStateManager.Instance != null)
        {
            UIStateManager.Instance.RegisterOpenUI("PauseMenu");
        }

        Debug.Log("Game paused");
    }

    public void ResumeGame()
    {
        if (!isPaused)
            return;

        // Resume the game
        Time.timeScale = timeScaleBeforePause;
        isPaused = false;

        // Hide pause menu
        if (pauseMenuPanel)
            pauseMenuPanel.SetActive(false);

        // Re-enable player input and hide cursor
        EnableGameplay();

        // Resume audio
        ResumeAudio();
        
        // Unregister with UI state manager
        if (UIStateManager.Instance != null)
        {
            UIStateManager.Instance.RegisterClosedUI("PauseMenu");
        }

        Debug.Log("Game resumed");
    }

    private void DisableGameplay()
    {
        // Unlock cursor for UI
        // Cursor.lockState = CursorLockMode.None;
        // Cursor.visible = true;

        // Use CursorManager instead
        // if (CursorManager.Instance != null)
        // {
        //     CursorManager.Instance.RequestCursorUnlock("PauseMenu");
        // }

        // Disable player interaction/movement
        PlayerInteractionManager interactionManager = FindObjectOfType<PlayerInteractionManager>();
        if (interactionManager)
            interactionManager.SetInteractionEnabled(false);

        // Disable input
        if (uiInputController)
            uiInputController.DisableGameplayInput();
    }

    private void EnableGameplay()
    {
        // Lock cursor for gameplay
        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;

        // Use CursorManager instead
        // if (CursorManager.Instance != null)
        // {
        //     CursorManager.Instance.RequestCursorLock("PauseMenu");
        // }

        // Enable player interaction/movement
        PlayerInteractionManager interactionManager = FindObjectOfType<PlayerInteractionManager>();
        if (interactionManager)
            interactionManager.SetInteractionEnabled(true);

        // Enable input
        if (uiInputController)
            uiInputController.EnableGameplayInput();
    }

    private void PauseAudio()
    {
        // Pause ambient/background audio if needed
        if (roomToneManager)
            roomToneManager.SetRunning(false);

        // Stop narrator (optional, you may want to still hear narrator)
        if (narratorManager)
            narratorManager.StopDialogue();
    }

    private void ResumeAudio()
    {
        // Resume ambient/background audio
        if (roomToneManager)
            roomToneManager.SetRunning(true);
    }

    private void OnVolumeChanged(float value)
    {
        // Update volume text
        UpdateVolumeText(value);

        // Apply volume to all audio systems
        ApplyMasterVolume(value);

        // Save volume setting
        PlayerPrefs.SetFloat("MasterVolume", value);
        PlayerPrefs.Save();
    }

    private void UpdateVolumeText(float value)
    {
        if (volumeValueText)
        {
            int percentage = Mathf.RoundToInt(value * 100);
            volumeValueText.text = percentage.ToString() + "%";
        }
    }

    private void ApplyMasterVolume(float volume)
    {
        // Apply to interaction sounds
        if (interactionSoundManager)
            interactionSoundManager.SetMasterVolume(volume);

        // Apply to UI sounds
        if (uiSoundManager)
            uiSoundManager.SetMasterVolume(volume);

        // You can add more audio systems here
        AudioListener.volume = volume; // Globally affects all audio
    }

    private void RestartGame()
    {
        // Use GameManager's restart method
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
        else
        {
            // Fallback approach
            Time.timeScale = 1f;
            
            // Reset cursor state before scene reload
            if (CursorManager.Instance != null)
            {
                CursorManager.Instance.ForceLockCursor();
            }        
            
            // Reload the current scene
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    private void ExitGame()
    {
        // In editor, stop playing
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            // In build, quit application
            Application.Quit();
        #endif
    }
}