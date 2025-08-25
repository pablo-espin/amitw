using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    // Game states - added IntroCutscene state
    public enum GameState
    {
        HomeScreen,
        IntroCutscene,
        MemoryInput,
        Gameplay,
        EndScreen
    }
    
    public GameState CurrentState { get; private set; }
    public string PlayerMemory { get; private set; }
    
    // Events for state changes
    public event Action<GameState> OnGameStateChanged;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CurrentState = GameState.HomeScreen;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartGame()
    {
        // Lock cursor when game actually starts loading
        if (CursorManager.Instance != null)
        {
            CursorManager.Instance.ForceLockCursor();
        }
        
        // Change to cutscene state instead of directly to gameplay
        ChangeState(GameState.IntroCutscene);
        SceneManager.LoadScene("IntroCutscene");
    }
    
    // Called from the cutscene when it completes
    public void StartGameplay()
    {
        ChangeState(GameState.Gameplay);
        SceneManager.LoadScene("GameLevel");
    }

    // public void SubmitMemory(string memory)
    // {
    //     PlayerMemory = memory;
    //     ChangeState(GameState.Gameplay);
    //     SceneManager.LoadScene("GameLevel");
    // }

    public void EndGame(string outcome)
    {
        ChangeState(GameState.EndScreen);
        // Store outcome for end screen display
        PlayerPrefs.SetString("GameOutcome", outcome);
        SceneManager.LoadScene("EndScreen");
    }

    // Public method for restarting the game from within gameplay
    public void RestartGame()
    {
        // Set the state to gameplay (not HomeScreen) since we're reloading the game level
        ChangeState(GameState.Gameplay);
        
        // Force cursor to be locked for gameplay
        if (CursorManager.Instance != null)
        {
            CursorManager.Instance.ForceLockCursor();
        }
        
        // Make sure time scale is reset
        Time.timeScale = 1f;
        
        // Reload the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ChangeState(GameState newState)
    {
        CurrentState = newState;
        OnGameStateChanged?.Invoke(newState);
    }
}