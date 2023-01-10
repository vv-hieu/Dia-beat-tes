using System.Collections;
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

    public void PauseForSeconds(float seconds)
    {
        Pause();
        p_UnpaseAsync(seconds);
    }

    private IEnumerator p_UnpaseAsync(float delay)
    {
        yield return new WaitForSeconds(delay);
        Unpause();
    }
}
