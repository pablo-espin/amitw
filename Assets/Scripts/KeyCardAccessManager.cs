using UnityEngine;
using System;

public class KeyCardAccessManager : MonoBehaviour
{
    [SerializeField] private GameObject keyCardIndicator;
    
    private bool hasKeyCard = false;
    
    // Event for other scripts to subscribe to
    public event Action OnKeyCardAcquired;
    
    private void Start()
    {
        // Hide key card indicator initially
        if (keyCardIndicator != null)
        {
            keyCardIndicator.SetActive(false);
        }
    }
    
    public void AcquireKeyCard()
    {
        if (hasKeyCard)
            return;
            
        hasKeyCard = true;
        
        // Show key card indicator
        if (keyCardIndicator != null)
        {
            keyCardIndicator.SetActive(true);
            
            // If indicator has the pulse script, trigger it
            KeyCardIndicator indicator = keyCardIndicator.GetComponent<KeyCardIndicator>();
            if (indicator != null)
            {
                indicator.StartPulseHighlight();
            }
        }
        
        // Trigger event
        OnKeyCardAcquired?.Invoke();
        
        Debug.Log("Key card acquired!");
    }
    
    public bool HasKeyCard()
    {
        return hasKeyCard;
    }
}