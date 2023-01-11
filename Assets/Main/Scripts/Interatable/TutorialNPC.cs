using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class TutorialNPC : MonoBehaviour
{
    [SerializeField] private Dialog[]   dialogs;
    [SerializeField] private GameObject dialogBox;
    [SerializeField] private Transform  dialogBoxPosition;
    [SerializeField] private Vector2    dialogBoxSize;
    [SerializeField] private float      delay;

    private int    m_index = 0;
    private bool   m_showing = false;
    private Player m_player;

    private void Update()
    {
        if (m_player == null)
        {
            GameObject go = GameObject.FindGameObjectWithTag("Player");
            if (go != null)
            {
                m_player = go.GetComponent<Player>();
            }
        }
        if (m_player != null && !m_showing && m_index < dialogs.Length && dialogs[m_index].CheckCondition(m_player))
        {
            p_Show();
        }
    }

    private void p_Show()
    {
        m_showing = true;
        Dialog dialog = dialogs[m_index];
        dialog.onStart?.Invoke();
        GameObject go = Instantiate(dialogBox, dialogBoxPosition.transform.position, Quaternion.identity, dialogBoxPosition);
        if (go.TryGetComponent(out DialogBox d))
        {
            d.Display(dialog.lines, dialogBoxSize, p_Callback);
        }
    }

    private void p_Callback()
    {
        StartCoroutine(p_CallbackAsync(delay));
    }

    private IEnumerator p_CallbackAsync(float delay)
    {
        yield return new WaitForSeconds(delay);
        dialogs[m_index].onEnd?.Invoke();
        ++m_index;
        m_showing = false;
    }

    [Serializable]
    public class Dialog
    {
        public Condition      condition;
        public string[]       lines;
        public DialogCallback onStart;
        public DialogCallback onEnd;

        private bool    m_init = false;
        private bool    m_playerRolled = false;
        private Vector2 m_playerPosition;
        private int     m_playerTotalKill;
        private float   m_walkDistance = 0.0f;

        public bool CheckCondition(Player player)
        {
            if (!m_init && player != null)
            {
                m_init = true;
                m_playerPosition  = player.transform.position;
                m_playerTotalKill = player.totalKillCount;

                player.GetComponent<Player>().onRollFinished.AddListener(p_OnPlayerRolled);
            }
            else
            {
                switch (condition)
                {
                    case Condition.None:
                        return true;
                    case Condition.PlayerMove:
                        m_walkDistance += Vector2.Distance(m_playerPosition, player.transform.position);
                        bool res1 = m_walkDistance > 1.0f;
                        m_playerPosition = player.transform.position;
                        return res1;
                    case Condition.PlayerKill:
                        bool res2 = m_playerTotalKill != player.totalKillCount;
                        m_playerTotalKill = player.totalKillCount;
                        return res2;
                    case Condition.PlayerFat:
                        return player.fatness >= 1.0f;
                    case Condition.PlayerRoll:
                        return m_playerRolled;
                    default:
                        break;
                }
            }
            return false;
        }

        private void p_OnPlayerRolled()
        {
            m_playerRolled = true;
        }
    }

    public enum Condition 
    {
        None,
        PlayerMove,
        PlayerKill,
        PlayerFat,
        PlayerRoll
    }

    [Serializable]
    public class DialogCallback : UnityEvent { };
}
