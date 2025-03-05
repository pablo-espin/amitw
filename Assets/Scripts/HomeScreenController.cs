using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HomeScreenController : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private TextMeshProUGUI controlsText;

    void Start()
    {
        // Set up button listener
        startButton.onClick.AddListener(OnStartButtonClicked);
        
        // Set up information text
        infoText.text = "Welcome to the data centre\n\n" +
                       "Your task is to decrypt a memory before it gets permanently deleted. " +
                       "I will be guiding you, but you can choose to follow your own path. Things here are fragile and can overheat easily, so be careful. Good luck!";
        
        // Set up controls text
        controlsText.text = "Controls:\n" +
                          "WASD - Move\n" +
                          "Mouse - Look around\n" +
                          "E - Interact\n" +
                          "ESC - Unlock mouse";
    }

    public void OnStartButtonClicked()
    {
        GameManager.Instance.StartGame();
    }
}