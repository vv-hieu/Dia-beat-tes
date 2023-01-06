using UnityEngine;

public class GameTimerUpdator : MonoBehaviour
{
    private void Update()
    {
        float dt = GameStateManager.instance.currentState == GameState.Paused ? 0.0f : Time.deltaTime;
        GameManager.AddTimer(dt);
    }
}
