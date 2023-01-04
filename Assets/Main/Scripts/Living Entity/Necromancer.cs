using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Necromancer : Boss
{
    [SerializeField] private Transform[]        skulls;
    [SerializeField] private float              skullOrbitSpeed;
    [SerializeField] private float              skullOrbitRadius;
    [SerializeField] private RadialDamageZone[] skullDamageZones;
    [SerializeField] private float              wanderRange;
    [SerializeField] private GameObject[]       summons;
    [SerializeField] private int                minSummons;
    [SerializeField] private int                maxSummons;
    [SerializeField] private float              summonDistance;
    [SerializeField] private float              summonCooldown;

    [SerializeField] private State currentState;

    private NavMeshAgent     m_navMeshAgent;
    private Player           m_player;
    private Vector2          m_wanderTarget;
    private bool             m_hasTarget      = false;
    private float            m_skullOffset    = 0.0f;
    private float            m_summonCooldown = 0.0f;
    private State            m_state          = State.Starting;
    private List<GameObject> m_summons        = new List<GameObject>();

    private void OnDrawGizmos()
    {
        if (m_hasTarget)
        {
            Color restoreColor = Gizmos.color;

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(new Vector3(m_wanderTarget.x, m_wanderTarget.y, 0.0f), 0.25f);

            Gizmos.color = restoreColor;
        }
    }

    protected override void OnAwake()
    {
        m_navMeshAgent = GetComponent<NavMeshAgent>();
    }

    protected override void OnStart()
    {
        m_navMeshAgent.updateUpAxis = false;
        m_navMeshAgent.updateRotation = false;

        foreach (RadialDamageZone damageZone in skullDamageZones)
        {
            damageZone.Init(livingEntity, 1.0f, 0.1f, 0.1f, 0.15f, new string[] { }, new string[] { "Friendly" }, false, false);
        }
    }

    protected override void OnUpdate()
    {
        m_player = GameManager.GetGameContext().player;

        switch (m_state)
        {
            case State.Starting:
                m_state = State.Summoning;
                break;
            case State.Wandering:
                if (m_hasTarget)
                {
                    m_navMeshAgent.SetDestination(m_wanderTarget);
                    if (p_DistanceToTarget() < 0.2f)
                    {
                        m_state = State.Summoning;
                    }
                }
                else
                {
                    m_state = State.Starting;
                }
                break;
            case State.Summoning:
                p_Summon();
                int attempt2 = 10;
                if (m_player != null)
                {
                    m_wanderTarget = new Vector2(Random.Range(-wanderRange, wanderRange) + m_player.transform.position.x, Random.Range(-wanderRange, wanderRange) + m_player.transform.position.y);
                    while (!GameManager.GetGameContext().map.IsWalkable(m_wanderTarget) && attempt2 > 0)
                    {
                        --attempt2;
                        m_wanderTarget = new Vector2(Random.Range(-wanderRange, wanderRange) + m_player.transform.position.x, Random.Range(-wanderRange, wanderRange) + m_player.transform.position.y);
                    }
                }
                else
                {
                    m_wanderTarget = new Vector2(Random.Range(-wanderRange, wanderRange) + transform.position.x, Random.Range(-wanderRange, wanderRange) + transform.position.y);
                    while (!GameManager.GetGameContext().map.IsWalkable(m_wanderTarget) && attempt2 > 0)
                    {
                        --attempt2;
                        m_wanderTarget = new Vector2(Random.Range(-wanderRange, wanderRange) + transform.position.x, Random.Range(-wanderRange, wanderRange) + transform.position.y);
                    }
                }
                m_hasTarget = attempt2 > 0;
                m_state = State.Wandering;
                break;
            default:
                break;
        }

        p_SetSkullPositions();
        m_skullOffset += skullOrbitSpeed * Time.deltaTime;
        m_summonCooldown = Mathf.Max(0.0f, m_summonCooldown - Time.deltaTime);

        currentState = m_state;
    }

    private void p_SetSkullPositions()
    {
        if (skulls.Length > 0)
        {
            float da = 2.0f * Mathf.PI / skulls.Length;
            float ca = m_skullOffset;

            foreach (Transform skull in skulls)
            {
                skull.position = new Vector3(transform.position.x, transform.position.y, 0.0f) + new Vector3(Mathf.Cos(ca), Mathf.Sin(ca), 0.0f) * skullOrbitRadius;
                ca += da;
            }
        }
    }

    private void p_Summon()
    {
        if (m_summonCooldown > 0.0f)
        {
            return;
        }

        for (int i = m_summons.Count - 1; i >= 0; --i)
        {
            if (m_summons[i] == null)
            {
                m_summons.RemoveAt(i);
            }
        }

        if (summons.Length > 0 && m_summons.Count < minSummons)
        {
            int min   = minSummons - m_summons.Count;
            int max   = maxSummons - m_summons.Count;
            int count = Random.Range(min, max + 1);
            while (count-- > 0)
            {
                int attempt = 10;
                Vector2 pos = new Vector2(transform.position.x, transform.position.y) + Random.insideUnitCircle * summonDistance;
                while (!GameManager.GetGameContext().map.IsWalkable(pos) && attempt > 0)
                {
                    --attempt;
                    pos = Random.insideUnitCircle * summonDistance;
                }
                if (attempt > 0)
                {
                    GameObject go = Instantiate(summons[Random.Range(0, summons.Length)], new Vector3(pos.x, pos.y, 0.0f), Quaternion.identity, transform.parent);
                    m_summons.Add(go);
                }
            }

            m_summonCooldown = summonCooldown;
        }
    }

    private float p_DistanceToTarget()
    {
        return Vector2.Distance(new Vector2(transform.position.x, transform.position.y), m_wanderTarget);
    }

    private enum State
    {
        Starting,
        Wandering,
        Summoning,
    }
}
