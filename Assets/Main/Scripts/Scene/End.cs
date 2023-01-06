using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class End : MonoBehaviour
{
    [SerializeField] private TextMeshPro coinCount;
    [SerializeField] private float       incrementTime = 5.0f;
    [SerializeField] private float       delayTime     = 1.0f;
    [SerializeField] private int         startSceneId;

    private void Start()
    {
        StartCoroutine(p_IncrementAsync());
    }

    private IEnumerator p_IncrementAsync()
    {
        int coins = GameManager.GetGameContext().completedLevels * 50 + Random.Range(0, 26);

        float time = 0.0f;
        while (time < incrementTime)
        {
            float d = Mathf.Clamp01(time / incrementTime);
            int displayAmount = (int)(d * d * d * (coins + 0.9f));
            if (coinCount != null)
            {
                coinCount.text = "" + displayAmount;
            }
            time += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        GameManager.SetTotalCoinCount(GameManager.GetGameContext().totalCoinCount + coins);
        yield return new WaitForSeconds(delayTime);

        GameObject go = GameObject.FindGameObjectWithTag("Player");
        Destroy(go);
        SceneManager.LoadScene(startSceneId);
    }
}
