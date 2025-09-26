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
    
    [Header("Water Level Animation")]
    [SerializeField] private float waterMinHeight = -0.5f; // Start position (below basin)
    [SerializeField] private float waterMaxHeight = 0.1f;  // End position (filled basin)
    [SerializeField] private AnimationCurve waterFillCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve waterDrainCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Clue Settings")]
    [SerializeField] private string waterClueCode = "H2O-781";
    [SerializeField] private GameObject clueTextObject;
    [SerializeField] private ClueProgressUI clueProgressUI;
    
    // References for interaction
    private PlayerInteractionManager interactionManager;
    private Coroutine waterAnimationCoroutine;
    
    // Store original basin water position and scale
    private Vector3 originalBasinPosition;
    private Vector3 originalBasinScale;
    
    // Store valve wheel's original rotation
    private Vector3 valveOriginalRotation;
    
    void Start()
    {
        // Find the interaction manager
        interactionManager = FindObjectOfType<PlayerInteractionManager>();
        
        // Store original basin water position and scale
        if (basinWaterObject != null) 
        {
            originalBasinPosition = basinWaterObject.transform.localPosition;
            originalBasinScale = basinWaterObject.transform.localScale;
            
            // Set initial position to minimum height (empty basin)
            Vector3 startPosition = originalBasinPosition;
            startPosition.y = waterMinHeight;
            basinWaterObject.transform.localPosition = startPosition;
        }
        
        // Store valve wheel's original rotation
        if (valveWheel != null)
        {
            valveOriginalRotation = valveWheel.localEulerAngles;
            Debug.Log($"Valve original rotation stored: {valveOriginalRotation}");
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
            // Check if valve is closed for narrator dialogue
            if (!valveOpened && GameInteractionDialogueManager.Instance != null)
            {
                GameInteractionDialogueManager.Instance.OnWaterTapWithValveClosed();
            }
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

            // Trigger narrator dialogue when valve is opened
            if (GameInteractionDialogueManager.Instance != null)
            {
                GameInteractionDialogueManager.Instance.OnValveOpened();
            }
        }
        else
        {
            Debug.Log("Closing valve");
            CloseValve();
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

        // Notify stats system that water tap stopped running
        if (StatsSystem.Instance != null)
        {
            StatsSystem.Instance.OnWaterTapStateChanged(false);
        }

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
        if (waterAnimationCoroutine != null)
        {
            StopCoroutine(waterAnimationCoroutine);
            waterAnimationCoroutine = StartCoroutine(DrainWater());
        }
    }
    
    private void OpenValve()
    {
        StartCoroutine(AnimateValveToPosition(true));
        valveOpened = true;
        
        // Check if water should flow
        CheckWaterFlow();
    }
    
    private void CloseValve()
    {
        StartCoroutine(AnimateValveToPosition(false));
        valveOpened = false;

        // Notify stats system that water tap stopped running (valve closed = no flow)
        if (StatsSystem.Instance != null)
        {
            StatsSystem.Instance.OnWaterTapStateChanged(false);
        }

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
        if (waterAnimationCoroutine != null)
        {
            StopCoroutine(waterAnimationCoroutine);
            waterAnimationCoroutine = StartCoroutine(DrainWater());
        }
    }
    
    private void CheckWaterFlow()
    {
        Debug.Log("Checking water flow - Tap open: " + tapOpened + ", Valve open: " + valveOpened);
        if (tapOpened && valveOpened)
        {
            Debug.Log("Starting water flow");
            
            // Notify stats system that water tap started running
            if (StatsSystem.Instance != null)
            {
                StatsSystem.Instance.OnWaterTapStateChanged(true);
            }

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
            waterAnimationCoroutine = StartCoroutine(FillBasin());
        }
    }
    
    private IEnumerator AnimateValveToPosition(bool opening)
    {
        // Temporarily disable player interaction during animation
        if (interactionManager != null) interactionManager.SetInteractionEnabled(false);
        
        // Determine start and end rotations
        Vector3 startRotation = valveWheel.localEulerAngles;
        Vector3 targetRotation;
        
        if (opening)
        {
            // Opening: rotate from original position to original + valveRotationAmount
            targetRotation = valveOriginalRotation;
            targetRotation.z = valveOriginalRotation.z + valveRotationAmount;
        }
        else
        {
            // Closing: rotate back to original position
            targetRotation = valveOriginalRotation;
        }
        
        Debug.Log($"Valve animation - Opening: {opening}, Start: {startRotation}, Target: {targetRotation}");
        
        float animationTime = 1f / valveRotationSpeed;
        float elapsed = 0f;
        
        while (elapsed < animationTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationTime;
            
            // Smoothly interpolate between start and target rotation
            Vector3 currentRotation = Vector3.Lerp(startRotation, targetRotation, t);
            valveWheel.localEulerAngles = currentRotation;
            
            yield return null;
        }
        
        // Ensure final rotation is exactly the target
        valveWheel.localEulerAngles = targetRotation;
        
        Debug.Log($"Valve animation complete. Final rotation: {valveWheel.localEulerAngles}");
        
        // Re-enable player interaction
        if (interactionManager != null) interactionManager.SetInteractionEnabled(true);
    }
    
    private IEnumerator FillBasin()
    {
        // Show basin water object at minimum height
        if (basinWaterObject != null)
        {
            basinWaterObject.SetActive(true);
            
            // Set starting position at minimum height
            Vector3 startPosition = originalBasinPosition;
            startPosition.y = waterMinHeight;
            basinWaterObject.transform.localPosition = startPosition;
            
            // Ensure scale is correct
            basinWaterObject.transform.localScale = originalBasinScale;
        }
        
        float elapsed = 0f;
        while (elapsed < waterFillTime)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / waterFillTime;
            
            if (basinWaterObject != null)
            {
                // Use animation curve for smooth filling
                float curveValue = waterFillCurve.Evaluate(normalizedTime);
                
                // Interpolate between min and max height
                float currentHeight = Mathf.Lerp(waterMinHeight, waterMaxHeight, curveValue);
                
                // Update position
                Vector3 currentPosition = originalBasinPosition;
                currentPosition.y = currentHeight;
                basinWaterObject.transform.localPosition = currentPosition;
            }
            yield return null;
        }
        
        // Ensure final position is exactly at max height
        if (basinWaterObject != null)
        {
            Vector3 finalPosition = originalBasinPosition;
            finalPosition.y = waterMaxHeight;
            basinWaterObject.transform.localPosition = finalPosition;
        }
        
        // NOTE: Clue is no longer revealed here - it will be revealed when water drains
        Debug.Log("Basin filling complete - clue will be revealed when water drains");
    }
    
    private IEnumerator DrainWater()
    {
        if (basinWaterObject == null || !basinWaterObject.activeSelf) 
            yield break;
        
        Debug.Log("Starting water drain");

        // Play drain sound
        if (InteractionSoundManager.Instance != null && basinWaterObject != null)
        {
            InteractionSoundManager.Instance.PlayWaterDrain(basinWaterObject.transform);
            Debug.Log("Water drain sound played");
        }
        
        // Get current water height
        float startHeight = basinWaterObject.transform.localPosition.y;
        
        float elapsed = 0f;
        while (elapsed < waterDrainTime)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / waterDrainTime;
            
            // Use animation curve for smooth draining
            float curveValue = waterDrainCurve.Evaluate(normalizedTime);
            
            // Interpolate from current height to min height
            float currentHeight = Mathf.Lerp(startHeight, waterMinHeight, curveValue);
            
            // Update position
            Vector3 currentPosition = originalBasinPosition;
            currentPosition.y = currentHeight;
            basinWaterObject.transform.localPosition = currentPosition;
            
            yield return null;
        }
        
        // Hide the water object when fully drained
        basinWaterObject.SetActive(false);
        Debug.Log("Water drain complete, water object deactivated");
        
        // Reveal clue after water has completely drained
        if (!clueRevealed)
        {
            Debug.Log("Water has completely drained - revealing clue now!");
            RevealClue();
        }
    }
    
    private void RevealClue()
    {
        clueRevealed = true;

        // Show code found text
        if (ItemFoundFeedbackManager.Instance != null)
        {
            ItemFoundFeedbackManager.Instance.ShowCodeFoundSequence();
        }
        
        // Show clue text and ensure it stays visible permanently
        if (clueTextObject)
        {
            clueTextObject.SetActive(true);
            Debug.Log("Water clue text revealed and activated: " + clueTextObject.name);
        }
        else
        {
            Debug.LogError("ClueTextObject is null! Make sure it's assigned in the inspector.");
        }
        
        // Update progress UI (this updates the HUD)
        if (clueProgressUI) 
        {
            clueProgressUI.SolveClue("water", waterClueCode);
            Debug.Log("Water clue registered in HUD UI with code: " + waterClueCode);
        }
        else
        {
            Debug.LogError("ClueProgressUI is null! Make sure it's assigned in the inspector.");
        }
        
        // Trigger narration or other events
        Debug.Log("Water clue fully revealed: " + waterClueCode);
    }
}