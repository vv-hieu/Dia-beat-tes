using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public void Pause()
    {
        GameStateManager.instance.SetState(GameState.Paused);
    }

    public void Unpause()
    {
        GameStateManager.instance.SetState(GameState.Gameplay);
    }
}
