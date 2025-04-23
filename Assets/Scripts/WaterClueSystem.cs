using UnityEngine;
using System.Collections;

public class WaterClueSystem : MonoBehaviour
{
    [Header("State Management")]
    private bool valveOpened = false;
    private bool tapOpened = false;
    private bool clueRevealed = false;
    
    [Header("Valve Components")]
    [SerializeField] private Transform valveWheel;
    [SerializeField] private float valveRotationAmount = 90f;
    [SerializeField] private float valveRotationSpeed = 1f;
    
    [Header("Water Effects")]
    [SerializeField] private ParticleSystem waterFlowParticles;
    [SerializeField] private GameObject basinWaterObject;
    [SerializeField] private float waterFillTime = 3f;
    [SerializeField] private float waterDrainTime = 2f;
    
    [Header("Clue Settings")]
    [SerializeField] private string waterClueCode = "H2O-781";
    [SerializeField] private GameObject clueTextObject;
    [SerializeField] private ClueProgressUI clueProgressUI;
    
    // References for interaction
    private PlayerInteractionManager interactionManager;
    private Coroutine waterFillCoroutine;
    
    // Store original basin water scale
    private Vector3 originalBasinScale;
    
    void Start()
    {
        // Find the interaction manager
        interactionManager = FindObjectOfType<PlayerInteractionManager>();
        
        // Store original basin water scale
        if (basinWaterObject) {
            originalBasinScale = basinWaterObject.transform.localScale;
        }
        
        // Set initial states
        if (waterFlowParticles) waterFlowParticles.gameObject.SetActive(false);
        if (basinWaterObject) basinWaterObject.SetActive(false);
        if (clueTextObject) clueTextObject.SetActive(false);
    }
    
    // Call this when player interacts with tap
    public void InteractWithTap()
    {
        Debug.Log("Tap interaction triggered");
        
        if (!tapOpened)
        {
            Debug.Log("Opening tap");
            OpenTap();

            // Play tap open sound
            if (InteractionSoundManager.Instance != null)
            {
                InteractionSoundManager.Instance.PlayTapToggle();
            }
        }
        else
        {
            Debug.Log("Closing tap");
            CloseTap();

            // Play tap close sound
            if (InteractionSoundManager.Instance != null)
            {
                InteractionSoundManager.Instance.PlayTapToggle();
            }
        }
    }
    
    // Call this when player interacts with valve
    public void InteractWithValve()
    {
        Debug.Log("Valve interaction triggered, current state: " + (valveOpened ? "Open" : "Closed"));

        // Play valve interaction sound
        if (InteractionSoundManager.Instance != null)
        {
            InteractionSoundManager.Instance.PlayValveInteraction();
        }
        
        if (!valveOpened)
        {
            Debug.Log("Opening valve");
            OpenValve();
            Debug.Log("Valve opened: " + valveOpened);
        }
        else
        {
            Debug.Log("Closing valve");
            CloseValve();
            Debug.Log("Valve closed: " + valveOpened);
        }
    }
    
    private void OpenTap()
    {
        // No animation, just change state
        tapOpened = true;
        
        // Check if water should flow
        CheckWaterFlow();
    }
    
    private void CloseTap()
    {
        tapOpened = false;
        
        // Stop water flow
        if (waterFlowParticles)
        {
            waterFlowParticles.Stop();

            // Stop water running sound
            if (InteractionSoundManager.Instance != null)
            {
                InteractionSoundManager.Instance.StopWaterRunning();
            }
        }
        
        // Drain water if it was filling
        if (waterFillCoroutine != null)
        {
            StopCoroutine(waterFillCoroutine);
            StartCoroutine(DrainWater());
        }
    }
    
    private void OpenValve()
    {
        StartCoroutine(AnimateValveRotation());
        valveOpened = true;
        
        // Check if water should flow
        CheckWaterFlow();
    }
    
    private void CloseValve()
    {
        StartCoroutine(AnimateValveRotation());
        valveOpened = false;
        
        // Stop water flow
        if (waterFlowParticles)
        {
            waterFlowParticles.Stop();

            // Stop water running sound
            if (InteractionSoundManager.Instance != null)
            {
                InteractionSoundManager.Instance.StopWaterRunning();
            }
        }
        
        // Drain water if it was filling
        if (waterFillCoroutine != null)
        {
            StopCoroutine(waterFillCoroutine);
            StartCoroutine(DrainWater());
        }
    }
    
    private void CheckWaterFlow()
    {
        Debug.Log("Checking water flow - Tap open: " + tapOpened + ", Valve open: " + valveOpened);
        if (tapOpened && valveOpened)
        {
            Debug.Log("Starting water flow");
            // Start water flow
            if (waterFlowParticles) 
            {
                waterFlowParticles.gameObject.SetActive(true);
                waterFlowParticles.Play();
                Debug.Log("Water flow effect activated");
                
                // Start water running sound with position
                if (InteractionSoundManager.Instance != null)
                {
                    // Pass the tap transform (or water particles transform) to position the sound
                    Transform soundPosition = waterFlowParticles.transform;
                    InteractionSoundManager.Instance.StartWaterRunning(soundPosition);
                }
            }
            else
            {
                Debug.LogError("Water flow particles is null");
            }
            
            // Start filling basin
            waterFillCoroutine = StartCoroutine(FillBasin());
        }
    }
    
    private IEnumerator AnimateValveRotation()
    {
        // Temporarily disable player interaction during animation
        if (interactionManager != null) interactionManager.SetInteractionEnabled(false);
        
        float targetRotation = valveRotationAmount;
        float time = 0;
        Vector3 startRotation = valveWheel.localEulerAngles;
        
        while (time < 1)
        {
            time += Time.deltaTime * valveRotationSpeed;
            float zRotation = startRotation.z + (targetRotation * time);
            valveWheel.localEulerAngles = new Vector3(startRotation.x, startRotation.y, zRotation);
            yield return null;
        }
        
        // Re-enable player interaction
        if (interactionManager != null) interactionManager.SetInteractionEnabled(true);
    }
    
    private IEnumerator FillBasin()
    {
        // Show basin water object at minimum scale
        if (basinWaterObject)
        {
            basinWaterObject.SetActive(true);
            // Use original X and Z scale, but minimal Y scale
            basinWaterObject.transform.localScale = new Vector3(
                originalBasinScale.x, 
                originalBasinScale.y * 0.01f, 
                originalBasinScale.z);
        }
        
        float time = 0;
        while (time < waterFillTime)
        {
            time += Time.deltaTime;
            if (basinWaterObject)
            {
                // Gradually increase water height while preserving X and Z scale
                float waterHeight = Mathf.Lerp(0.01f, 1f, time / waterFillTime);
                basinWaterObject.transform.localScale = new Vector3(
                    originalBasinScale.x, 
                    originalBasinScale.y * waterHeight, 
                    originalBasinScale.z);
            }
            yield return null;
        }
        
        // Reveal clue only if not already revealed
        if (!clueRevealed)
        {
            RevealClue();
        }
    }
    
    private IEnumerator DrainWater()
    {
        if (!basinWaterObject) yield break;
        
        float startHeight = basinWaterObject.transform.localScale.y / originalBasinScale.y;
        float time = 0;
        
        while (time < waterDrainTime)
        {
            time += Time.deltaTime;
            float waterHeight = Mathf.Lerp(startHeight, 0.01f, time / waterDrainTime);
            basinWaterObject.transform.localScale = new Vector3(
                originalBasinScale.x, 
                originalBasinScale.y * waterHeight, 
                originalBasinScale.z);
            yield return null;
        }
        
        basinWaterObject.SetActive(false);
    }
    
    private void RevealClue()
    {
        clueRevealed = true;
        
        // Show clue text
        if (clueTextObject) clueTextObject.SetActive(true);
        
        // Update progress UI
        if (clueProgressUI) clueProgressUI.SolveClue("water", waterClueCode);
        
        // Trigger narration or other events
        Debug.Log("Water clue revealed: " + waterClueCode);
    }
}