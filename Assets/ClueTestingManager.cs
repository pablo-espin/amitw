using UnityEngine;

public class ClueTestingScript : MonoBehaviour
{
    [SerializeField] private ClueProgressUI clueProgressUI;
    
    [Header("Test Clue Codes")]
    [SerializeField] private string waterClueCode = "H2O-981";
    [SerializeField] private string electricityClueCode = "KWH-365";
    [SerializeField] private string locationClueCode = "NYC-527";
    [SerializeField] private string falseClueCode = "ERR-404";
    
    void Update()
    {
        // Press 1 to discover Water clue
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            clueProgressUI.SolveClue("water", waterClueCode);
            Debug.Log("Water clue discovered: " + waterClueCode);
        }
        
        // Press 2 to discover Electricity clue
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            clueProgressUI.SolveClue("electricity", electricityClueCode);
            Debug.Log("Electricity clue discovered: " + electricityClueCode);
        }
        
        // Press 3 to discover Location clue
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            clueProgressUI.SolveClue("location", locationClueCode);
            Debug.Log("Location clue discovered: " + locationClueCode);
        }
        
        // Press 4 to discover False clue
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            clueProgressUI.SolveClue("false", falseClueCode);
            Debug.Log("False clue discovered: " + falseClueCode);
        }
        
        // Press C to check if all real clues are solved
        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("All clues solved: " + clueProgressUI.AreAllCluesSolved());
        }
    }
}