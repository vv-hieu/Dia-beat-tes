using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private GameObject loadingScreen;
    
    private List<GameObject> m_keep = new List<GameObject>();
    private float m_delay = 2.0f;

    public void AddObjectToKeep(GameObject obj)
    {
        m_keep.Add(obj);
    }

    public void LoadScene(int sceneId)
    {
        StartCoroutine(p_LoadSceneAsync(sceneId));
    }

    public void LoadSceneWithDelay(int sceneId)
    {
        StartCoroutine(p_LoadSceneAsync(sceneId, m_delay));
    }

    public void SetDelay(float delay)
    {
        m_delay = delay;
    }

    private IEnumerator p_LoadSceneAsync(int sceneId)
    {
        foreach (GameObject go in m_keep)
        {
            DontDestroyOnLoad(go);
        }

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneId);

        if (loadingScreen != null)
        {
            loadingScreen.SetActive(true);
        }

        while (!op.isDone)
        {
            float progress = Mathf.Clamp01(op.progress / 0.9f);
            yield return null;
        }

        GameStateManager.instance.SetState(GameState.Gameplay);
    }

    private IEnumerator p_LoadSceneAsync(int sceneId, float seconds)
    {
        yield return new WaitForSeconds(seconds);

        foreach (GameObject go in m_keep)
        {
            DontDestroyOnLoad(go);
        }

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneId);

        if (loadingScreen != null)
        {
            loadingScreen.SetActive(true);
        }

        while (!op.isDone)
        {
            float progress = Mathf.Clamp01(op.progress / 0.9f);
            yield return null;
        }

        GameStateManager.instance.SetState(GameState.Gameplay);
    }
}
