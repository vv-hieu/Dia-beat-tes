using UnityEngine;

public class GameStateManager
{
    private static GameStateManager s_instance;

    private GameStateManager()
    {
    }

    public static GameStateManager instance
    {
        get
        {
            if (s_instance == null)
            {
                s_instance = new GameStateManager();
            }

            return s_instance;
        }
    }

    public delegate void GameStateChangedHandler(GameState newState);

    public event GameStateChangedHandler onGameStateChanged;

    public GameState currentState { get; private set; }

    public void SetState(GameState newState)
    {
        if (currentState != newState)
        {
            currentState = newState;
            onGameStateChanged?.Invoke(newState);
        }
    }
}

public enum GameState
{
    Gameplay,
    Paused
}