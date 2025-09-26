using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class FalseClueSystem : MonoBehaviour
{
    [Header("State Management")]
    private bool captchaSolved = false;
    private bool clueRevealed = false;
    private bool computerLocked = false;
    
    [Header("Computer UI")]
    [SerializeField] private GameObject computerScreen;
    [SerializeField] private GameObject captchaPanel;
    [SerializeField] private GameObject cluePanel;
    [SerializeField] private GameObject matrixEffectPanel;
    [SerializeField] private GameObject tabBar;
    [SerializeField] private Button captchaTabButton;
    [SerializeField] private Button catVideoTabButton;
    [SerializeField] private GameObject catVideoPanel;
    
    [Header("CAPTCHA Components")]
    [SerializeField] private TextMeshProUGUI captchaText;
    [SerializeField] private TMP_InputField captchaInput;
    [SerializeField] private Button submitCaptchaButton;
    [SerializeField] private string[] possibleCaptchas;
    private string currentCaptcha;
    
    [Header("Clue Settings")]
    [SerializeField] private string falseClueCode = "ERR-404";
    [SerializeField] private TextMeshProUGUI clueText;
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
        if (submitCaptchaButton != null)
            submitCaptchaButton.onClick.AddListener(CheckCaptcha);
            
        // Setup tab buttons
        if (captchaTabButton != null)
            captchaTabButton.onClick.AddListener(() => SwitchTab(true));
            
        if (catVideoTabButton != null)
            catVideoTabButton.onClick.AddListener(() => SwitchTab(false));
        
        // Initialize UI states
        if (computerScreen != null)
            computerScreen.SetActive(false);
            
        if (captchaPanel != null)
            captchaPanel.SetActive(false);
            
        if (catVideoPanel != null)
            catVideoPanel.SetActive(true);
            
        if (cluePanel != null)
            cluePanel.SetActive(false);
            
        if (matrixEffectPanel != null)
            matrixEffectPanel.SetActive(false);
            
        // Generate initial CAPTCHA
        GenerateNewCaptcha();
    }
    
    // Public method to check if computer is locked
    public bool IsComputerLocked()
    {
        return computerLocked;
    }

    public void InteractWithComputer()
    {
        // If computer is locked, don't allow interaction
        if (computerLocked)
        {
            Debug.Log("Computer is locked");
            return;
        }

        // Play computer boot sound
        if (InteractionSoundManager.Instance != null)
        {
            InteractionSoundManager.Instance.PlayComputerBoot();
        }

        // Show the computer screen
        if (computerScreen != null)
        {
            computerScreen.SetActive(true);
            
            // Register with UI state manager
            if (UIStateManager.Instance != null)
            {
                UIStateManager.Instance.RegisterOpenUI("ComputerScreen");
            }

            // By default, show cat video tab
            SwitchTab(false);
            
            // Disable player interaction while using computer
            if (interactionManager != null)
                interactionManager.SetInteractionEnabled(false);
                
            // Unlock the cursor for UI interaction
            // Cursor.lockState = CursorLockMode.None;
            // Cursor.visible = true;

            // Use CursorManager instead
            // if (CursorManager.Instance != null)
            // {
            //     CursorManager.Instance.RequestCursorUnlock("FalseClueSystem");
            // }

        }

        // Disable player input
        if (uiInputController != null)
        {
            uiInputController.DisableGameplayInput();
        }
    }
    
    public void CloseComputer()
    {
        // Hide the computer screen
        if (computerScreen != null)
        {
            computerScreen.SetActive(false);

            // Unregister with UI state manager
            if (UIStateManager.Instance != null)
            {
                UIStateManager.Instance.RegisterClosedUI("ComputerScreen");
            }        
        }

        // Re-enable player interaction
        if (interactionManager != null)
        {
            interactionManager.SetInteractionEnabled(true);
        }    
        // Re-lock the cursor for gameplay
        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;

        // Use CursorManager instead
        //if (CursorManager.Instance != null)
        //{
        //    CursorManager.Instance.RequestCursorLock("FalseClueSystem");
        //}

        // Enable player input
        if (uiInputController != null)
        {
            uiInputController.EnableGameplayInput();
        }
    }
    
    private void SwitchTab(bool showCaptcha)
    {
        if (captchaPanel != null)
            captchaPanel.SetActive(showCaptcha);
            
        if (catVideoPanel != null)
            catVideoPanel.SetActive(!showCaptcha);
            
        // Update tab button visual states
        if (captchaTabButton != null && catVideoTabButton != null)
        {
            // Implement visual indication of active tab
            // For example, changing colors or background
        }
    }
    
    private void GenerateNewCaptcha()
    {
        // Pick a random CAPTCHA from the possible options
        if (possibleCaptchas != null && possibleCaptchas.Length > 0)
        {
            currentCaptcha = possibleCaptchas[Random.Range(0, possibleCaptchas.Length)];
            
            if (captchaText != null)
                captchaText.text = currentCaptcha;
        }
    }
    
    private void CheckCaptcha()
    {
        if (captchaInput != null && !string.IsNullOrEmpty(captchaInput.text))
        {
            // Check if input matches CAPTCHA (case-insensitive)
            if (captchaInput.text.Trim().ToLower() == currentCaptcha.ToLower())
            {
                SolveCaptcha();
            }
            else
            {
                // Wrong CAPTCHA
                captchaInput.text = "";
                GenerateNewCaptcha();
            }
        }
    }
    
    private void SolveCaptcha()
    {
        captchaSolved = true;
        computerLocked = true;

        // Notify stats system of CAPTCHA solving
        if (StatsSystem.Instance != null)
        {
            StatsSystem.Instance.OnCaptchaSolved();
            Debug.Log("Notified StatsSystem of CAPTCHA solution - power increased to 4x base");
        }

        // Trigger dialogue for solving the CAPTCHA
        if (GameInteractionDialogueManager.Instance != null)
        {
            GameInteractionDialogueManager.Instance.OnCaptchaSolved();
        }

        // Play false clue reveal sound
        if (InteractionSoundManager.Instance != null)
        {
            InteractionSoundManager.Instance.PlayFalseClueReveal();
        }

        // Show the clue panel briefly
        if (captchaPanel != null)
            captchaPanel.SetActive(false);
            
        if (cluePanel != null)
        {
            cluePanel.SetActive(true);
            
            // Set the clue text
            if (clueText != null)
                clueText.text = "System Access Code: " + falseClueCode;
        }
        
        // Hide tab bar
        if (tabBar != null)
            tabBar.SetActive(false);
        
        RevealClue();
        
        // Start matrix effect after a delay
        StartCoroutine(ShowMatrixEffectDelayed(3.0f));
    }

    private IEnumerator ShowMatrixEffectDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Hide all panels
        if (captchaPanel != null) captchaPanel.SetActive(false);
        if (catVideoPanel != null) catVideoPanel.SetActive(false);
        if (cluePanel != null) cluePanel.SetActive(false);

        // Show matrix effect
        if (matrixEffectPanel != null)
        {
            matrixEffectPanel.SetActive(true);

            // Play matrix animation sound
            if (InteractionSoundManager.Instance != null)
            {
                InteractionSoundManager.Instance.StartMatrixAnimation();
            }
        }

        // Automatically close computer after another delay
        yield return new WaitForSeconds(5.0f);

        // Stop matrix animation sound
        if (InteractionSoundManager.Instance != null)
        {
            InteractionSoundManager.Instance.StopMatrixAnimation();
        }

        CloseComputer();
        
        // Show code found text
        if (ItemFoundFeedbackManager.Instance != null)
        {
            ItemFoundFeedbackManager.Instance.ShowCodeFoundSequence();
        }
    }
    
    private void RevealClue()
    {
        if (clueRevealed) 
            return;
            
        clueRevealed = true;
        
        // Update the clue progress UI
        if (clueProgressUI != null)
            clueProgressUI.SolveClue("false", falseClueCode);
            
        Debug.Log("False clue revealed: " + falseClueCode);
    }
}