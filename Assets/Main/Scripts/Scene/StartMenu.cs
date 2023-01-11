using UnityEngine;

public class StartMenu : MonoBehaviour
{
    [SerializeField] private int lobbySceneId;
    [SerializeField] private int tutoialSceneId;

    public void Quit()
    {
        Application.Quit();
    }

    public void LoadNextScene(SceneLoader sceneLoader)
    {
        sceneLoader.LoadScene(Tutorial.TutorialDone() ? lobbySceneId : tutoialSceneId);
    }
}
