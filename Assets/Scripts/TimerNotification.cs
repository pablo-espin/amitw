using UnityEngine;
using TMPro;
using System.Collections;

public class TimerNotification : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private float displayDuration = 3f;
    [SerializeField] private float fadeInTime = 0.5f;
    [SerializeField] private float fadeOutTime = 0.5f;
    [SerializeField] private CanvasGroup canvasGroup;
    
    private Coroutine currentNotification;
    
    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        // Hide initially
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
    
    public void ShowNotification(string message, Color color)
    {
        // If already showing a notification, stop it
        if (currentNotification != null)
            StopCoroutine(currentNotification);
        
        // Start new notification
        currentNotification = StartCoroutine(ShowNotificationCoroutine(message, color));
    }
    
    private IEnumerator ShowNotificationCoroutine(string message, Color color)
    {
        // Set text and color
        notificationText.text = message;
        notificationText.color = color;
        
        // Fade in
        float timer = 0f;
        while (timer < fadeInTime)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeInTime);
            yield return null;
        }
        canvasGroup.alpha = 1f;
        
        // Display for duration
        yield return new WaitForSeconds(displayDuration);
        
        // Fade out
        timer = 0f;
        while (timer < fadeOutTime)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeOutTime);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        
        currentNotification = null;
    }
}