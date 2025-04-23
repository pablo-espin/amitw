using UnityEngine;
using UnityEngine.UI;

public class UIAutoSoundSetup : MonoBehaviour
{
    [SerializeField] private bool setupOnAwake = true;
    
    private void Awake()
    {
        if (setupOnAwake)
        {
            SetupAllUIElementSounds();
        }
    }
    
    public void SetupAllUIElementSounds()
    {
        // Setup all buttons in this UI hierarchy
        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            // Only add handler if it doesn't already have one
            if (button.gameObject.GetComponent<UIButtonSoundHandler>() == null)
            {
                button.gameObject.AddComponent<UIButtonSoundHandler>();
            }
        }
        
        // Setup all toggles
        Toggle[] toggles = GetComponentsInChildren<Toggle>(true);
        foreach (Toggle toggle in toggles)
        {
            // Add toggle sound handler if needed
            if (toggle.onValueChanged.GetPersistentEventCount() == 0)
            {
                toggle.onValueChanged.AddListener((value) => {
                    if (UISoundManager.Instance != null)
                        UISoundManager.Instance.PlayToggle();
                });
            }
        }
        
        // Setup all input fields for typing sounds
        TMPro.TMP_InputField[] inputFields = GetComponentsInChildren<TMPro.TMP_InputField>(true);
        foreach (TMPro.TMP_InputField inputField in inputFields)
        {
            // Add typing sound handler
            inputField.onValueChanged.AddListener((value) => {
                if (UISoundManager.Instance != null && value.Length > 0)
                    UISoundManager.Instance.PlayTyping();
            });
        }
    }
}