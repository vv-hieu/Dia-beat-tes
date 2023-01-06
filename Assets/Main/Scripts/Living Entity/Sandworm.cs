using UnityEngine;

public class Sandworm : Boss
{
    [SerializeField] private Transform          head;
    [SerializeField] private float              wanderRange;
    [SerializeField] private float              attackRadius;
    [SerializeField] private float              rotateSpeed;
    [SerializeField] private float              explodeTime;
    [SerializeField] private RadialDamageZone[] damageZones;
    [SerializeField] private GameObject         explodeVFX;
    [SerializeField] private AudioClip          explodeSound;

    private Player      m_player;
    private State       m_state = State.Starting;
    private Vector2     m_target;
    private float       m_time  = 0.0f;
    private Transform[] m_segments;
    private int         m_segmentIndex = 0;

    private void OnDrawGizmos()
    {
        Color restoreColor = Gizmos.color;

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(m_target, 0.25f);

        Gizmos.color = restoreColor;
    }

    protected override void OnStart()
    {
        foreach (RadialDamageZone damageZone in damageZones)
        {
            damageZone.Init(livingEntity, 1.0f, 0.5f, 0.4f, 0.5f, new string[] { }, new string[] { "Friendly" }, true, true);
        }
    }

    protected override void OnUpdate()
    {
        m_player = GameManager.GetGameContext().player;

        switch (m_state)
        {
            case State.Starting:
                p_OnWander();
                m_state = State.Wandering;
                break;
            case State.Wandering:
                if (p_DistanceToTarget() <= 0.1f)
                {
                    p_OnApproach();
                    m_state = State.Approaching;
                }
                break;
            case State.Approaching:
                if (m_player == null)
                {
                    m_state = State.Wandering;
                }
                else if (p_DistanceToTarget() <= 0.1f)
                {
                    p_OnCharge();
                    m_state = State.Charging;
                }
                break;
            case State.Charging:
                if (m_player == null)
                {
                    m_state = State.Wandering;
                }
                else if (p_DistanceToTarget() <= 0.1f)
                {
                    p_OnPostCharge();
                    m_state = State.PostCharging;
                }
                break;
            case State.PostCharging:
                if (m_player == null)
                {
                    m_state = State.Wandering;
                }
                else if (p_DistanceToTarget() <= 0.1f)
                {
                    p_OnWander();
                    m_state = State.Wandering;
                }
                break;
            case State.Dying:
                if (m_time >= explodeTime)
                {
                    m_time = 0.0f;
                }
                if (m_time <= 0.0f)
                {
                    Instantiate(explodeVFX, m_segments[m_segmentIndex].position, Quaternion.identity, transform.parent);
                    SoundManager.PlaySound(explodeSound);
                    Destroy(m_segments[m_segmentIndex].gameObject);
                    ++m_segmentIndex;
                    if (m_segmentIndex >= m_segments.Length)
                    {
                        Destroy(gameObject);
                    }
                }
                m_time += Time.deltaTime;
                break;
            default:
                break;
        }

        if (m_state != State.Dying && GameStateManager.instance.currentState != GameState.Paused)
        {
            float distToTarget = Vector2.Distance(new Vector2(head.position.x, head.position.y), m_target);
            if (distToTarget > 0.05f)
            {
                Vector2 direction = (m_target - new Vector2(head.position.x, head.position.y)).normalized;
                head.rotation = Quaternion.RotateTowards(head.rotation, Quaternion.LookRotation(Vector3.forward, direction), (1.0f / distToTarget + 1.0f) * rotateSpeed * Time.deltaTime);
                float d = 0.5f * (1.0f + Vector2.Dot(direction.normalized, new Vector2(head.up.x, head.up.y)));
                transform.Translate(head.up * p_Speed() * d * Time.deltaTime);
            }
        }
    }

    protected override void OnDeath()
    {
        m_state = State.Dying;
        m_segments = head.GetComponent<ConnectedSegments>().GetSegments(true);
    }

    private void p_OnWander()
    {
        do
        {
            m_target = new Vector2(Random.Range(-wanderRange, wanderRange) + m_player.transform.position.x, Random.Range(-wanderRange, wanderRange) + m_player.transform.position.y);
        } while (Vector2.Distance(m_target, new Vector2(transform.position.x, transform.position.y)) < 0.5f * wanderRange);
    }

    private void p_OnApproach()
    {
        do
        {
            float angle = Random.Range(0.0f, 360.0f);
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            m_target = new Vector2(m_player.transform.position.x, m_player.transform.position.y) + direction * attackRadius;
        } while (Vector2.Distance(m_target, new Vector2(transform.position.x, transform.position.y)) < attackRadius);
    }

    private void p_OnCharge()
    {
        m_target = new Vector2(m_player.transform.position.x, m_player.transform.position.y);
    }

    private void p_OnPostCharge()
    {
        Vector2 direction = new Vector2(head.up.x, head.up.y);
        m_target += direction.normalized * attackRadius;
    }

    private float p_Speed()
    {
        return livingEntity.statSet.GetValue("speed");
    }

    private float p_DistanceToTarget()
    {
        return Vector2.Distance(new Vector2(head.position.x, head.position.y), m_target);
    }

    private enum State
    {
        Starting,
        Wandering,
        Approaching,
        Charging,
        PostCharging,
        Dying
    }
}
