using UnityEngine;

public class MemorySphere : MonoBehaviour
{
    [Header("Sphere States")]
    private bool isDecrypted = false;
    private bool isCorrupted = false;
    private bool isDeleted = false; // Flag for deleted state
    
    [Header("Visual Properties")]
    [SerializeField] private Material normalMaterial;
    [SerializeField] private Material decryptedMaterial;
    [SerializeField] private Material corruptedMaterial;
    [SerializeField] private Material deletedMaterial; // Material for deleted state
    private MeshRenderer sphereRenderer;

    [Header("Floating Animation")]
    [SerializeField] private float floatHeight = 0.1f;
    [SerializeField] private float floatSpeed = 1f;
    [SerializeField] private float deletedFloatSpeed = 0.3f; // Slower float for deleted state
    [SerializeField] private float deletedFloatHeight = 0.05f; // Lower float height for deleted state
    
    [Header("Deleted State Effects")]
    [SerializeField] private float pulseFrequency = 1.5f; // How fast the sphere pulses when deleted
    [SerializeField] private float minScale = 0.95f; // Minimum scale during pulse
    [SerializeField] private float maxScale = 1.05f; // Maximum scale during pulse

    [Header("Audio")]
    [SerializeField] private AudioSource ambientSoundSource;
    
    private Vector3 startPosition;
    private Vector3 originalScale;

    void Start()
    {
        sphereRenderer = GetComponent<MeshRenderer>();
        startPosition = transform.position;
        originalScale = transform.localScale;
        
        // Initialize with normal material
        if (sphereRenderer && normalMaterial)
        {
            sphereRenderer.material = normalMaterial;
        }
    }

    void Update()
    {
        if (isDeleted)
        {
            // Create floating motion using sine wave - slower for deleted state
            float newY = startPosition.y + (Mathf.Sin(Time.time * deletedFloatSpeed) * deletedFloatHeight);
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            
            // Add a pulsing effect when in deleted state
            float pulseScale = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(Time.time * pulseFrequency) + 1) * 0.5f);
            transform.localScale = originalScale * pulseScale;
        }
        else if (!isDecrypted && !isCorrupted)
        {
            // Normal floating motion using sine wave
            float newY = startPosition.y + (Mathf.Sin(Time.time * floatSpeed) * floatHeight);
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            
            // Ensure scale is normal
            transform.localScale = originalScale;
        }
    }

    public void OnInteract()
    {
        if (!isDecrypted && !isCorrupted)
        {
            // Check if this is the first interaction with any memory sphere
            if (GameInteractionDialogueManager.Instance != null)
            {
                GameInteractionDialogueManager.Instance.OnMemorySphereFirstInteraction();
            }

            // Play interaction sound
            if (InteractionSoundManager.Instance != null)
            {
                InteractionSoundManager.Instance.PlayMemorySphereInteraction();
            }

            // Show decryption panel through HUD regardless of state
            // (the HUD manager will handle showing the deleted message if needed)
            FindObjectOfType<GameHUDManager>().ShowDecryptionPanel();
        }
    }

    public void Decrypt()
    {
        // Only decrypt if not already in deleted state
        if (!isDeleted)
        {
            isDecrypted = true;
            isCorrupted = false;
            
            if (sphereRenderer && decryptedMaterial)
            {
                sphereRenderer.material = decryptedMaterial;
            }
            
            // Reset scale to original
            transform.localScale = originalScale;
            
            // Optional: Add particle effects or other visual feedback
            Debug.Log("Memory successfully decrypted");
        }
        else
        {
            Debug.Log("Cannot decrypt - memory is permanently deleted");
        }
    }
    
    public void Corrupt()
    {
        // Only corrupt if not already in deleted state
        if (!isDeleted)
        {
            isCorrupted = true;
            isDecrypted = false;
            
            if (sphereRenderer && corruptedMaterial)
            {
                sphereRenderer.material = corruptedMaterial;
            }
            
            // Reset scale to original
            transform.localScale = originalScale;
            
            // Optional: Add corruption visual effects
            Debug.Log("Memory corrupted");
        }
        else
        {
            Debug.Log("Cannot corrupt - memory is permanently deleted");
        }
    }
    
    public void Delete()
    {
        isDeleted = true;
        isDecrypted = false;
        isCorrupted = false;
        
        // Try to get MeshRenderer again in case it wasn't found in Start()
        if (sphereRenderer == null)
        {
            sphereRenderer = GetComponent<MeshRenderer>();
            Debug.Log("Getting MeshRenderer again: " + (sphereRenderer != null ? "Found" : "Not Found"));
        }
        
        // Check if we have both the renderer and material
        if (sphereRenderer != null && deletedMaterial != null)
        {
            // Direct material assignment
            sphereRenderer.material = deletedMaterial;
            Debug.Log("Deleted material applied successfully");
        }
        else
        {
            Debug.LogError("Cannot apply deleted material - MeshRenderer: " + 
                        (sphereRenderer != null ? "Found" : "Not Found") + 
                        ", Material: " + 
                        (deletedMaterial != null ? "Found" : "Not Found"));
        }

        // Stop the ambient sound when sphere is deleted
        if (ambientSoundSource != null)
        {            
            // Fade out gracefully
            StartCoroutine(FadeOutAudio(1.0f)); // Fade out over 1 second
        }        

        Debug.Log("Memory permanently deleted");
    }

    // Fade out memory sphere ambient audio
    private System.Collections.IEnumerator FadeOutAudio(float fadeDuration)
    {
        if (ambientSoundSource == null) yield break;
        
        float startVolume = ambientSoundSource.volume;
        float timer = 0;
        
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            ambientSoundSource.volume = Mathf.Lerp(startVolume, 0, timer / fadeDuration);
            yield return null;
        }
        
        // Ensure volume is zero and stop the sound
        ambientSoundSource.volume = 0;
        ambientSoundSource.Stop();
    }

    public bool IsDecrypted()
    {
        return isDecrypted;
    }
    
    public bool IsCorrupted()
    {
        return isCorrupted;
    }
    
    public bool IsDeleted()
    {
        return isDeleted;
    }
}