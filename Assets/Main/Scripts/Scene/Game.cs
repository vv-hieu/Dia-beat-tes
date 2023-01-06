using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Game : MonoBehaviour
{
    [SerializeField] private GameObject     pauseMenu;
    [SerializeField] private RelicSelection relicSelection;
    [SerializeField] private RelicSelection finalRelicSelection;
    [SerializeField] private int            endSceneId;

    private GameObject m_currentMenu;

    public void CompleteLevel()
    {
        GameObject go = GameObject.FindGameObjectWithTag("Player");
        if (go != null)
        {
            if (go.TryGetComponent(out Player player))
            {
                player.ResetPosition();
            }
        }
        GameManager.CompleteLevel();
    }

    public void QueryRelics(int phaseIdx)
    {
        relicSelection.Clear();
        relicSelection.Query(RelicSelection.QueryInfo.Create()
            .Common(Mathf.Max(1.0f, 30.0f - phaseIdx * 6.0f))
            .Rare(5.0f)
            .Cursed(1.0f));
    }

    public void QueryFinalRelics()
    {
        finalRelicSelection.Clear();
        finalRelicSelection.Query(RelicSelection.QueryInfo.Create()
            .Common(5.0f)
            .Rare(5.0f)
            .Cursed(1.0f));
    }

    public void ResetCurrentMenu()
    {
        m_currentMenu = null;
    }

    public void SetCurrentMenu(GameObject menuObj)
    {
        m_currentMenu = menuObj;
    }

    private void Start()
    {
        GameObject go = GameObject.FindGameObjectWithTag("Player");
        if (go != null)
        {
            if (go.TryGetComponent(out Player player))
            {
                player.onPlayerDie.AddListener(p_OnPlayerDie);
                player.transform.position = Vector3.zero;
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown("escape"))
        {
            if (m_currentMenu != null)
            {
                m_currentMenu.SetActive(false);
                m_currentMenu = null;
                GameStateManager.instance.SetState(GameState.Gameplay);
            }
            else
            {
                if (pauseMenu != null)
                {
                    m_currentMenu = pauseMenu;
                    m_currentMenu.SetActive(true);
                    GameStateManager.instance.SetState(GameState.Paused);
                }
            }
        }
    }

    private void p_OnPlayerDie()
    {
        StartCoroutine(p_OnPlayerDieAsync());
    }

    private IEnumerator p_OnPlayerDieAsync()
    {
        yield return new WaitForSeconds(2.0f);
        SceneManager.LoadScene(endSceneId);
    }
}
