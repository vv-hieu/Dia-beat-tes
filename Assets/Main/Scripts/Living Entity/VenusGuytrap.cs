using UnityEngine;

public class VenusGuytrap : Boss
{
    [SerializeField] private Transform  body;
    [SerializeField] private Transform  leftLeaf;
    [SerializeField] private Transform  rightLeaf;
    [SerializeField] private float      leafOpenAngle  = 15.0f;
    [SerializeField] private float      leafCloseAngle = 75.0f;
    [SerializeField] private float      appearTime     = 0.2f;
    [SerializeField] private float      openTime       = 0.2f;
    [SerializeField] private float      attackTime     = 2.0f;
    [SerializeField] private float      hideTime       = 2.0f;
    [SerializeField] private float      attackRadius   = 5.0f;
    [SerializeField] private GameObject weapon;
    [SerializeField] private GameObject explodeVFX;

    private Player m_player;
    private State  m_state     = State.Starting;
    private float  m_bodyScale = 0.0f;
    private float  m_leafAngle = 0.0f;
    private float  m_leafScale = 0.0f;
    private float  m_time      = 0.0f;
    private bool   m_attacked  = false;
    private bool   m_appeared  = false;

    protected override void OnStart()
    {
        m_bodyScale = 0.0f;
        m_leafAngle = leafCloseAngle;
        m_leafScale = 0.0f;

        livingEntity.SetWeapon(weapon);
    }

    protected override void OnUpdate()
    {
        m_player = GameManager.GetGameContext().player;

        bool changedState = false;
        switch (m_state)
        {
            case State.Starting:
                m_time = 0.0f;
                m_state = State.Hiding;
                break;
            case State.Appearing:
                if (!m_appeared)
                {
                    m_appeared = true;
                    p_SelectPosition();
                }
                m_leafScale = Mathf.Clamp01(m_time / appearTime);
                if (m_time >= appearTime)
                {
                    m_time = 0.0f;
                    changedState = true;
                    m_state = State.OpeningLeaves;
                }
                break;
            case State.OpeningLeaves:
                m_bodyScale = Mathf.Clamp01(m_time / openTime);
                m_leafAngle = Mathf.Lerp(leafCloseAngle, leafOpenAngle, m_bodyScale);
                if (m_time >= openTime)
                {
                    m_time = 0.0f;
                    changedState = true;
                    m_attacked = false;
                    m_state = m_player == null ? State.Idle : State.Attacking;
                }
                break;
            case State.Attacking:
                if (m_player == null)
                {
                    m_state = State.Idle;
                }
                else
                {
                    m_bodyScale = Mathf.Sin(m_time * 5.0f) * 0.02f + 1.0f;
                    m_leafScale = m_bodyScale;
                    if (m_time >= attackTime * 0.5f && !m_attacked)
                    {
                        m_attacked = true;
                        p_Attack();
                    }
                    if (m_time >= attackTime)
                    {
                        m_time = 0.0f;
                        m_bodyScale = 1.0f;
                        m_leafScale = 1.0f;
                        changedState = true;
                        m_state = State.ClosingLeaves;
                    }
                }
                break;
            case State.ClosingLeaves:
                if (m_player == null)
                {
                    m_state = State.Idle;
                }
                else
                {
                    m_bodyScale = 1.0f - Mathf.Clamp01(m_time / openTime);
                    m_leafAngle = Mathf.Lerp(leafCloseAngle, leafOpenAngle, m_bodyScale);
                    if (m_time >= openTime)
                    {
                        m_time = 0.0f;
                        changedState = true;
                        m_state = State.Disappearing;
                    }
                }
                break;
            case State.Disappearing:
                if (m_player == null)
                {
                    m_state = State.Idle;
                }
                else
                {
                    m_leafScale = 1.0f - Mathf.Clamp01(m_time / appearTime);
                    if (m_time >= appearTime)
                    {
                        m_time = 0.0f;
                        changedState = true;
                        m_state = State.Hiding;
                    }
                }
                break;
            case State.Hiding:
                if (m_time >= 2.0f)
                {
                    m_time = 0.0f;
                    changedState = true;
                    m_appeared = false;
                    m_state = State.Appearing;
                }
                break;
            case State.Idle:
                m_bodyScale = Mathf.Sin(m_time * 5.0f) * 0.02f + 1.0f;
                m_leafScale = m_bodyScale;
                if (m_player != null)
                {
                    m_time = 0.0f;
                    m_bodyScale = 1.0f;
                    m_leafScale = 1.0f;
                    changedState = true;
                    m_state = State.Hiding;
                }
                break;
            default:
                break;
        }


        body.localScale = Vector3.one * m_bodyScale;

        leftLeaf.localScale  = Vector3.one * m_leafScale;
        rightLeaf.localScale = Vector3.one * m_leafScale;

        leftLeaf.localEulerAngles  = Vector3.forward * -Mathf.Abs(m_leafAngle);
        rightLeaf.localEulerAngles = Vector3.forward * Mathf.Abs(m_leafAngle);

        if (!changedState)
        {
            m_time += Time.deltaTime;
        }
    }

    protected override void OnDeath()
    {
        if (explodeVFX != null)
        {
            Instantiate(explodeVFX, transform.position, Quaternion.identity, transform.parent);
        }
        Destroy(gameObject);
    }

    private void p_SelectPosition()
    {
        if (m_player != null)
        {
            float random = Random.Range(0.0f, 360.0f);
            transform.position = new Vector3(m_player.transform.position.x + Mathf.Cos(random) * attackRadius, m_player.transform.position.y + Mathf.Sin(random) * attackRadius, 0.0f);
        }
    }

    private void p_Attack()
    {
        if (m_player != null)
        {
            livingEntity.AimWeaponAt(new Vector2(m_player.transform.position.x, m_player.transform.position.y));
            livingEntity.UseWeapon(new string[] { "Friendly" });
        }
    }

    private enum State
    {
        Starting,
        Appearing,
        OpeningLeaves,
        Attacking,
        ClosingLeaves,
        Disappearing,
        Hiding,
        Idle
    }
}
