using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MobSpawner : MonoBehaviour
{
    [SerializeField] private Map                map;
    [SerializeField] private Phase[]            phases;
    [SerializeField] private GameObject         boss;
    [SerializeField] private int                mobCap;
    [SerializeField] private BossDefeatHandler  onBossDefeat;
    [SerializeField] private PhaseChangeHandler onPhaseChange;

    private Phase            m_currentPhase;
    private int              m_phaseIdx        = -1;
    private float            m_phaseTimestamp  = 0.0f;
    private float            m_timer           = 0.0f;
    private bool             m_init            = false;
    private System.Random    m_rng             = new System.Random();
    private List<GameObject> m_mobs            = new List<GameObject>();
    private bool             m_normalPhases    = true;
    private bool             m_bossSummoned    = false;
    private GameObject       m_boss;

    private void Start()
    {
        p_Begin(5.0f);
    }

    private void Update()
    {
        if (m_init)
        {
            GameManager.GameContext context = GameManager.GetGameContext();

            if (m_normalPhases && context.timeSinceStart >= m_phaseTimestamp)
            {
                if (m_phaseIdx >= 0)
                {
                    p_OnPhaseEnded(m_phaseIdx);
                }
                ++m_phaseIdx;
                if (m_phaseIdx < phases.Length)
                {
                    m_phaseTimestamp += phases[m_phaseIdx].duration;
                    p_OnPhaseEnter(m_phaseIdx);
                    m_currentPhase = phases[m_phaseIdx];
                }
                else
                {
                    m_normalPhases = false;
                    p_SpawnBoss();
                }
            }

            if (m_timer >= 1.0f / m_currentPhase.spawnRate)
            {
                p_AttemptSpawn();
                m_timer = 0.0f;
            }
            if (m_bossSummoned && m_boss == null)
            {
                onBossDefeat?.Invoke();
                m_bossSummoned = false;
            }

            m_timer += GameStateManager.instance.currentState == GameState.Gameplay ? Time.deltaTime : 0.0f;
        }
    }

    private void p_OnPhaseEnter(int index)
    {
    }

    private void p_OnPhaseEnded(int index)
    {
        onPhaseChange?.Invoke(index);
    }

    private void p_SpawnBoss()
    {
        if (boss != null && !m_bossSummoned)
        {
            Vector2 spawnPos = map.RandomWalkable(m_rng);
            m_boss = Instantiate(boss, new Vector3(spawnPos.x, spawnPos.y, 0.0f), Quaternion.identity, transform);
            m_bossSummoned = (m_boss != null);
        }
    }

    private void p_AttemptSpawn()
    {
        for (int i = m_mobs.Count - 1; i >= 0; --i)
        {
            if (m_mobs[i] == null)
            {
                m_mobs.RemoveAt(i);
            }
        }

        if (m_mobs.Count >= mobCap)
        {
            return;
        }

        GameObject mob = m_currentPhase.mobPool.Get(m_rng);
        if (mob != null)
        {
            Vector2 spawnPos = map.RandomWalkable(m_rng);
            m_mobs.Add(Instantiate(mob, new Vector3(spawnPos.x, spawnPos.y, 0.0f), Quaternion.identity, transform));
        }
    }

    private void p_Begin(float delay)
    {
        StartCoroutine(p_BeginAsync(delay));
    }

    private IEnumerator p_BeginAsync(float delay)
    {
        yield return new WaitForSeconds(delay);
        m_init = true;
    }

    [Serializable]
    public struct Phase
    {
        public float   duration;
        public float   spawnRate;
        public MobPool mobPool;
    }

    [Serializable]
    public class BossDefeatHandler : UnityEvent { }

    [Serializable]
    public class PhaseChangeHandler : UnityEvent<int> { }
}
