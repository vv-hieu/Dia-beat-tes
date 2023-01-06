using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private GameObject loadingScreen;

    public void LoadScene(int sceneId)
    {
        StartCoroutine(p_LoadSceneAsync(sceneId));
    }

    private IEnumerator p_LoadSceneAsync(int sceneId)
    {
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
    }
}
