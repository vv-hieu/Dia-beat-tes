using UnityEngine;

public class Weapon : MonoBehaviour
{
    [Header("Weapon Properties")]
    [SerializeField] private string weaponName;
    [SerializeField] private Sprite weaponSprite;

    [Header("References")]
    [SerializeField] private SpriteRenderer sprite;

    private LivingEntity m_user;
    private Vector2      m_target;
    private bool         m_flipped             = false;
    private bool         m_rotating            = false;
    private bool         m_offseting           = false;
    private float        m_angle               = 0.0f;
    private float        m_sinAngle            = 0.0f;
    private float        m_cosAngle            = 1.0f;
    private float        m_rotateFrom          = 0.0f;
    private float        m_rotateTo            = 0.0f;
    private float        m_rotateTime          = 0.0f;
    private float        m_rotateTotalTime     = 0.0f;
    private float        m_offset              = 0.0f;
    private float        m_offsetFrom          = 0.0f;
    private float        m_offsetTo            = 0.0f;
    private float        m_offsetTime          = 0.0f;
    private float        m_offsetTotalTime     = 0.0f;
    private State        m_state               = State.Ready;
    private string[]     m_affectedTags;

    protected Vector2 direction { get; private set; }

    public string Name()
    {
        return weaponName;
    }

    public Sprite Sprite()
    {
        return weaponSprite;
    }

    public virtual Type WeaponType()
    {
        return Type.Unknown;
    }

    public void SetUser(LivingEntity user)
    {
        m_user = user;
    }

    public LivingEntity GetUser()
    {
        return m_user;
    }

    public void AimAt(Vector2 target)
    {
        m_target = target;
    }

    protected void SetAngle(float angle)
    {
        m_angle = angle;
        m_sinAngle = Mathf.Sin(angle * Mathf.Deg2Rad);
        m_cosAngle = Mathf.Cos(angle * Mathf.Deg2Rad);
    }

    protected float GetAngle()
    {
        return m_angle;
    }

    protected void RotateTo(float angle, float time)
    {
        if (time > 0.0f)
        {
            m_rotating        = true;
            m_rotateFrom      = m_angle;
            m_rotateTo        = angle;
            m_rotateTime      = 0.0f;
            m_rotateTotalTime = time;
        }
        else
        {
            SetAngle(angle);
        }
    }

    protected void SetOffset(float offset)
    {
        m_offset = offset;
    }

    protected float GetOffset()
    {
        return m_offset;
    }

    protected void OffsetTo(float offset, float time)
    {
        if (time > 0.0f)
        {
            m_offseting       = true;
            m_offsetFrom      = m_offset;
            m_offsetTo        = offset;
            m_offsetTime      = 0.0f;
            m_offsetTotalTime = time;
        }
        else
        {
            SetOffset(offset);
        }
    }

    protected void SetFlipped(bool flipped)
    {
        m_flipped = flipped;
    }

    protected bool GetFlipped()
    {
        return m_flipped;
    }

    protected string[] GetAffectedTags()
    {
        return m_affectedTags;
    }

    public bool Use(string[] affectedTags)
    {
        if (m_state != State.Ready || m_user == null)
        {
            return false;
        }

        m_affectedTags = affectedTags;
        OnActivate();

        m_state = State.Activating;
        return true;
    }

    public virtual void OnStart()
    {
    }

    public virtual void OnActivate()
    {
    }

    public virtual void OnCoolDown()
    {
    }

    public virtual void OnUse()
    {
    }

    public virtual void Activating()
    {
    }

    public virtual void CoolingDown()
    {
    }

    public virtual void Using()
    {
    }

    public virtual bool ActivateEnded()
    {
        return true;
    }

    public virtual bool CoolDownEnded()
    {
        return true;
    }

    public virtual bool UseEnded()
    {
        return true;
    }

    public virtual Optional<float> ReachDistance()
    {
        return new Optional<float>();
    }

    private void Start()
    {
        OnStart();
    }

    private void Update()
    {
        p_RotateToDir();
    
        if (m_state == State.Activating)
        {
            Activating();
            if (ActivateEnded())
            {
                OnUse();
                m_state = State.Using;
            }
        }
        else if (m_state == State.Using)
        {
            Using();
            if (UseEnded())
            {
                OnCoolDown();
                m_state = State.CoolingDown;
            }
        }
        else if (m_state == State.CoolingDown)
        {
            CoolingDown();
            if (CoolDownEnded())
            {
                m_state = State.Ready;
            }
        }

        if (m_rotating)
        {
            SetAngle(Mathf.Lerp(m_rotateFrom, m_rotateTo, m_rotateTime / m_rotateTotalTime));
            if (m_rotateTime >= m_rotateTotalTime)
            {
                m_rotating = false;
            }
            m_rotateTime += Time.deltaTime;
        }

        if (m_offseting)
        {
            SetOffset(Mathf.Lerp(m_offsetFrom, m_offsetTo, m_offsetTime / m_offsetTotalTime));
            if (m_offsetTime >= m_offsetTotalTime)
            {
                m_offseting = false;
            }
            m_offsetTime += Time.deltaTime;
        }
    }

    private void p_RotateToDir()
    {
        Vector2 pos = new Vector2(transform.position.x, transform.position.y);

        Vector3 dir = m_target - pos;
        if (dir.sqrMagnitude <= 0.1f)
        {
            return;
        }
        dir = dir.normalized;

        direction = dir;
        if (sprite != null)
        {
            if (dir.x < -0.01f)
            {
                sprite.flipX = !m_flipped;
            }
            else if (dir.x > 0.01f)
            {
                sprite.flipX = m_flipped;
            }
        }
        dir = new Vector3(m_cosAngle * dir.x - m_sinAngle * dir.y, m_sinAngle * dir.x + m_cosAngle * dir.y, 0.0f);
        transform.rotation = Quaternion.LookRotation(Vector3.forward, dir);

        transform.localPosition = dir * m_offset;
    }

    public enum Type
    { 
        Unknown = -1,
        Melee,      // Swords, spears, ...
        Ranged,     // Guns, bows, ...
        Activated   // Wands
    }

    private enum State
    {
        Unknown = -1,
        Ready,
        Activating,
        Using,
        CoolingDown
    }
}

public class MeleeWeapon : Weapon
{
    public override Type WeaponType()
    {
        return Type.Melee;
    }
}

public class RangedWeapon : Weapon
{
    public override Type WeaponType()
    {
        return Type.Ranged;
    }
}

public class ActivatedWeapon : Weapon
{
    public override Type WeaponType()
    {
        return Type.Activated;
    }
}
