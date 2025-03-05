using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    // Game states
    public enum GameState
    {
        HomeScreen,
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

    private void ChangeState(GameState newState)
    {
        CurrentState = newState;
        OnGameStateChanged?.Invoke(newState);
    }
}