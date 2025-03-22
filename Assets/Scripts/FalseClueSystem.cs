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
    private UIMovementBlocker movementBlocker;

    void Start()
    {
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

        movementBlocker = FindObjectOfType<UIMovementBlocker>();
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
        
        // Show the computer screen
        if (computerScreen != null)
        {
            computerScreen.SetActive(true);
            
            // By default, show cat video tab
            SwitchTab(false);
            
            // Disable player interaction while using computer
            if (interactionManager != null)
                interactionManager.SetInteractionEnabled(false);
                
            // Unlock the cursor for UI interaction
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Block player movement
            if (movementBlocker != null)
            movementBlocker.BlockMovement();
        }
    }
    
    public void CloseComputer()
    {
        // Hide the computer screen
        if (computerScreen != null)
            computerScreen.SetActive(false);
            
        // Re-enable player interaction
        if (interactionManager != null)
            interactionManager.SetInteractionEnabled(true);

        // Unblock player movement
        if (movementBlocker != null)
        movementBlocker.UnblockMovement();
            
        // Re-lock the cursor for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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
            matrixEffectPanel.SetActive(true);
        
        // Automatically close computer after another delay
        yield return new WaitForSeconds(5.0f);
        CloseComputer();
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