using UnityEngine;

// Add this script to the player object to automatically create the InputManager
public class InputManagerSetup : MonoBehaviour
{
    void Awake()
    {
        // Check if InputManager already exists
        if (InputManager.Instance == null)
        {
            // Create a new GameObject for the InputManager
            GameObject inputManagerObj = new GameObject("InputManager");
            inputManagerObj.AddComponent<InputManager>();
            Debug.Log("InputManager created");
        }
    }
}