using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIButtonSoundHandler : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [SerializeField] private bool playHoverSound = true;
    [SerializeField] private bool playClickSound = true;
    
    // Called when pointer enters the UI element
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (playHoverSound && UISoundManager.Instance != null)
        {
            UISoundManager.Instance.PlayButtonHover();
        }
    }
    
    // Called when UI element is clicked
    public void OnPointerClick(PointerEventData eventData)
    {
        if (playClickSound && UISoundManager.Instance != null)
        {
            UISoundManager.Instance.PlayButtonClick();
        }
    }
}