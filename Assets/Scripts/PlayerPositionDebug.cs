using UnityEngine;

public class PlayerPositionDebug : MonoBehaviour
{
    void Start()
    {
        Debug.Log($"Player position at start: {transform.position}");
    }
    
    void Update()
    {
        if (Time.frameCount % 300 == 0) // Log every 300 frames
        {
            Debug.Log($"Player position: {transform.position}");
        }
    }
}