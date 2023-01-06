using UnityEngine;

public class Lobby : MonoBehaviour
{
    [SerializeField] private RelicSelection cursedRelicSelection;
    [SerializeField] private RelicSelection nonCursedRelicSelection;
    [SerializeField] private GameObject     pauseMenu;

    private GameObject m_currentMenu;

    public void ResetLevelCounter()
    {
        GameManager.ResetLevel();
    }

    public void ResetCurrentMenu()
    {
        m_currentMenu = null;
    }

    public void SetCurrentMenu(GameObject menuObj)
    {
        m_currentMenu = menuObj;
    }

    public void RerollShop() {
        if (p_UseMoney(100))
        {
            nonCursedRelicSelection.Clear();
            nonCursedRelicSelection.Query(RelicSelection.QueryInfo.Create()
            .Common(20.0f)
            .Rare(1.0f));
        }
    }

    private void Start()
    {
        cursedRelicSelection.Query(RelicSelection.QueryInfo.Create()
            .Cursed(1.0f));

        nonCursedRelicSelection.Query(RelicSelection.QueryInfo.Create()
            .Common(20.0f)
            .Rare(1.0f));
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

    private bool p_UseMoney(int amount)
    {
        return GameManager.SpendCoin(amount);
    }
}
