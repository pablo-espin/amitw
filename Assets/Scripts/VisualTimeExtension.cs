using UnityEngine;
using TMPro;
using System.Collections;

public class VisualTimeExtension : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private TextMeshProUGUI extensionText;
    
    [Header("Animation Settings")]
    [SerializeField] private Color extensionColor = Color.green;
    
    private Vector3 startPosition;
    
    private void Awake()
    {
        if (extensionText != null)
        {
            startPosition = extensionText.transform.position;
            extensionText.gameObject.SetActive(false);
        }
    }
    
    public void ShowTimeExtension(int minutes)
    {
        string message = $"+{minutes} minutes added";
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(SimpleTimeExtensionAnimation(message));
        }
        else
        {
            Debug.LogWarning("Cannot start time extension animation - GameObject is inactive");
        }
    }
    
    public void ShowTimeExtension(string message)
    {
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(SimpleTimeExtensionAnimation(message));
        }
        else
        {
            Debug.LogWarning("Cannot start time extension animation - GameObject is inactive");
        }
    }
    
    private IEnumerator SimpleTimeExtensionAnimation(string message)
    {
        if (extensionText == null)
        {
            Debug.LogError("ExtensionText is null!");
            yield break;
        }
        
        Debug.Log($"Showing time extension message: {message}");
        
        // Setup text
        extensionText.gameObject.SetActive(true);
        extensionText.text = message;
        extensionText.color = extensionColor;
        extensionText.transform.position = startPosition;
        
        // Show text for 2 seconds
        float displayTime = 0f;
        while (displayTime < 2f)
        {
            displayTime += Time.deltaTime;
            
            // Keep text at start position and full alpha
            extensionText.transform.position = startPosition;
            extensionText.color = extensionColor;
            
            yield return null;
        }
        
        // Quick fade out
        float fadeElapsed = 0f;
        float fadeDuration = 0.3f;
        
        while (fadeElapsed < fadeDuration)
        {
            fadeElapsed += Time.deltaTime;
            float fadeT = fadeElapsed / fadeDuration;
            
            Color color = extensionColor;
            color.a = Mathf.Lerp(1f, 0f, fadeT);
            extensionText.color = color;
            
            yield return null;
        }
        
        // Hide and reset
        extensionText.gameObject.SetActive(false);
        extensionText.transform.position = startPosition;
        extensionText.color = extensionColor;
        
        Debug.Log("Time extension animation complete");
    }
}