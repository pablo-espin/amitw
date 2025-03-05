using UnityEngine;

public class MemorySphere : MonoBehaviour
{
    [Header("Sphere States")]
    private bool isDecrypted = false;
    private bool isCorrupted = false;
    
    [Header("Visual Properties")]
    [SerializeField] private Material normalMaterial;
    [SerializeField] private Material decryptedMaterial;
    [SerializeField] private Material corruptedMaterial;
    private MeshRenderer sphereRenderer;

    [Header("Floating Animation")]
    [SerializeField] private float floatHeight = 0.1f;
    [SerializeField] private float floatSpeed = 1f;
    private Vector3 startPosition;

    void Start()
    {
        sphereRenderer = GetComponent<MeshRenderer>();
        startPosition = transform.position;
    }

    void Update()
    {
        if (!isDecrypted && !isCorrupted)
        {
            // Create floating motion using sine wave
            float newY = startPosition.y + (Mathf.Sin(Time.time * floatSpeed) * floatHeight);
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }

    public void OnInteract()
    {
        if (!isDecrypted && !isCorrupted)
        {
            // Show decryption panel through HUD
            FindObjectOfType<GameHUDManager>().ShowDecryptionPanel();
        }
    }

    public void Decrypt()
    {
        isDecrypted = true;
        isCorrupted = false;
        sphereRenderer.material = decryptedMaterial;
        
        // Optional: Add particle effects or other visual feedback
        Debug.Log("Memory successfully decrypted");
    }
    
    public void Corrupt()
    {
        isCorrupted = true;
        isDecrypted = false;
        sphereRenderer.material = corruptedMaterial;
        
        // Optional: Add corruption visual effects
        Debug.Log("Memory corrupted");
    }

    public bool IsDecrypted()
    {
        return isDecrypted;
    }
    
    public bool IsCorrupted()
    {
        return isCorrupted;
    }
}