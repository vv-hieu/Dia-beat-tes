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
    [SerializeField] private GameObject         staff;

    private NavMeshAgent     m_navMeshAgent;
    private Player           m_player;
    private Vector2          m_wanderTarget;
    private bool             m_init           = false;
    private bool             m_hasTarget      = false;
    private float            m_skullOffset    = 0.0f;
    private float            m_summonCooldown = 0.0f;
    private State            m_state          = State.Starting;
    private List<GameObject> m_summons        = new List<GameObject>();
    private Optional<float>  m_reach;

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

        livingEntity.SetWeapon(staff);

        Weapon w = livingEntity.GetWeapon();
        if (w != null)
        {
            m_reach = w.ReachDistance();
        }
    }

    protected override void OnUpdate()
    {
        m_player = GameManager.GetGameContext().player;

        int attempt = 0;
        switch (m_state)
        {
            case State.Starting:
                if (!m_init)
                {
                    m_init = true;
                    m_state = State.Summoning;
                }
                else
                {
                    attempt = 10;
                    if (m_player != null)
                    {
                        m_wanderTarget = new Vector2(Random.Range(-wanderRange, wanderRange) + m_player.transform.position.x, Random.Range(-wanderRange, wanderRange) + m_player.transform.position.y);
                        while (!GameManager.GetGameContext().map.IsWalkable(m_wanderTarget) && attempt > 0)
                        {
                            --attempt;
                            m_wanderTarget = new Vector2(Random.Range(-wanderRange, wanderRange) + m_player.transform.position.x, Random.Range(-wanderRange, wanderRange) + m_player.transform.position.y);
                        }
                    }
                    else
                    {
                        m_wanderTarget = new Vector2(Random.Range(-wanderRange, wanderRange) + transform.position.x, Random.Range(-wanderRange, wanderRange) + transform.position.y);
                        while (!GameManager.GetGameContext().map.IsWalkable(m_wanderTarget) && attempt > 0)
                        {
                            --attempt;
                            m_wanderTarget = new Vector2(Random.Range(-wanderRange, wanderRange) + transform.position.x, Random.Range(-wanderRange, wanderRange) + transform.position.y);
                        }
                    }
                    m_hasTarget = attempt > 0;
                    m_state = State.Wandering;
                }
                break;
            case State.Wandering:
                if (m_hasTarget)
                {
                    m_navMeshAgent.SetDestination(m_wanderTarget);
                    if (p_DistanceToTarget() < 0.5f)
                    {
                        m_state = m_summonCooldown > 0.0f ? State.Starting : State.Summoning;
                    }
                }
                else
                {
                    m_state = State.Starting;
                }
                break;
            case State.Summoning:
                p_Summon();
                attempt = 10;
                if (m_player != null)
                {
                    m_wanderTarget = new Vector2(Random.Range(-wanderRange, wanderRange) + m_player.transform.position.x, Random.Range(-wanderRange, wanderRange) + m_player.transform.position.y);
                    while (!GameManager.GetGameContext().map.IsWalkable(m_wanderTarget) && attempt > 0)
                    {
                        --attempt;
                        m_wanderTarget = new Vector2(Random.Range(-wanderRange, wanderRange) + m_player.transform.position.x, Random.Range(-wanderRange, wanderRange) + m_player.transform.position.y);
                    }
                }
                else
                {
                    m_wanderTarget = new Vector2(Random.Range(-wanderRange, wanderRange) + transform.position.x, Random.Range(-wanderRange, wanderRange) + transform.position.y);
                    while (!GameManager.GetGameContext().map.IsWalkable(m_wanderTarget) && attempt > 0)
                    {
                        --attempt;
                        m_wanderTarget = new Vector2(Random.Range(-wanderRange, wanderRange) + transform.position.x, Random.Range(-wanderRange, wanderRange) + transform.position.y);
                    }
                }
                m_hasTarget = attempt > 0;
                m_state = State.Starting;
                break;
            default:
                break;
        }


        p_SetSkullPositions();
        m_skullOffset += skullOrbitSpeed * Time.deltaTime;
        m_summonCooldown = Mathf.Max(0.0f, m_summonCooldown - Time.deltaTime);

        if (m_player != null)
        {
            Vector2 targetPos = new Vector2(m_player.transform.position.x, m_player.transform.position.y);
            livingEntity.AimWeaponAt(targetPos);
            if (!m_reach.enabled || (m_reach.enabled && p_DistanceToTarget() < m_reach.value))
            {
                livingEntity.UseWeapon(new string[] { "Friendly" });
            }
        }
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
        if (m_summonCooldown > 0.0f || !livingEntity.isInControl)
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
                    pos = new Vector2(transform.position.x, transform.position.y) + Random.insideUnitCircle * summonDistance;
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
