using UnityEngine;

public class SweepWeapon : MeleeWeapon
{
    [Header("Sweep Attack")]
    [SerializeField] private GameObject sweepAttack;
    [SerializeField] private float      idleAngle   = 45.0f;
    [SerializeField] private float      attackAngle = 75.0f;
    [SerializeField] private float      sweepSize   = 1.0f;

    [Header("Weapon Stats")]
    [SerializeField] private float damage;
    [SerializeField] private float knockback;
    [SerializeField] private float stunTime;
    [SerializeField] private float attackTime;
    [SerializeField] private float activateTime;

    [Header("Tags")]
    [SerializeField] private string[] tags;

    private bool  m_reverse = false;
    private float m_time    = 0.0f;

    public override void OnStart()
    {
        SetAngle(Mathf.Max(0.01f, Mathf.Abs(idleAngle)));
    }

    public override void OnActivate()
    {
        RotateTo(Mathf.Sign(GetAngle()) * attackAngle, p_ActivateTime());
    }

    public override void OnCoolDown()
    {
        RotateTo(Mathf.Sign(GetAngle()) * idleAngle, p_ActivateTime());
        OffsetTo(0.0f, p_ActivateTime());
    }

    public override void OnUse()
    {
        if (sweepAttack != null)
        {
            GameObject go = Instantiate(sweepAttack, transform.position + new Vector3(direction.x, direction.y, 0.0f) * 0.5f * sweepSize, Quaternion.identity, GetUser().transform.parent);
            Melee m = go.GetComponent<Melee>();
            if (m != null)
            {
                m.Init(GetUser(), direction, p_AttackTime(), sweepSize, m_reverse, new string[] { "Hostile" }, p_OnMeleeHit);
            }
            m_reverse = !m_reverse;
            m_time = 0.0f;
        }
        RotateTo(-GetAngle(), p_AttackTime());
        OffsetTo(0.5f, p_AttackTime());
    }

    public override void Activating()
    {
        m_time += Time.deltaTime;
    }

    public override void CoolingDown()
    {
        m_time += Time.deltaTime;
    }

    public override void Using()
    {
        m_time += Time.deltaTime;
    }

    public override bool ActivateEnded()
    {
        if (m_time >= p_ActivateTime())
        {
            m_time = 0.0f;
            return true;
        }
        return false;
    }

    public override bool CoolDownEnded()
    {
        if (m_time >= p_ActivateTime())
        {
            m_time = 0.0f;
            return true;
        }
        return false;
    }

    public override bool UseEnded()
    {
        if (m_time >= p_AttackTime()) {
            m_time = 0.0f;
            return true;
        }
        return false;
    }

    private float p_AttackTime()
    {
        float userAttackSpeed = 0.0f;
        if (GetUser() != null)
        {
            userAttackSpeed = GetUser().statSet.GetValue("attackSpeed");
        }
        return attackTime * (1.0f - userAttackSpeed * 0.05f);
    }

    private float p_ActivateTime()
    {
        float userAttackSpeed = 0.0f;
        if (GetUser() != null)
        {
            userAttackSpeed = GetUser().statSet.GetValue("attackSpeed");
        }
        return activateTime * (1.0f - userAttackSpeed * 0.05f);
    }

    private float p_Damage()
    {
        float userAttackDamage = 0.0f;
        float a = 1.0f;
        float b = 0.0f;
        float c = 1.0f;
        if (GetUser() != null)
        {
            userAttackDamage = GetUser().statSet.GetValueAndCoefficients("attackDamage", out a, out b, out c);
        }
        return c * a * damage + userAttackDamage;
    }

    private void p_OnMeleeHit(LivingEntity entity, Melee melee)
    {
        LivingEntity.HandleAttack(melee.owner, entity, LivingEntity.AttackInfo.Create()
            .KnockbackDirection(melee.direction)
            .Knockback(knockback)
            .Damage(p_Damage())
            .InvulnerableTime(stunTime)
            .StunTime(stunTime)
            .Tags(tags)
        );

        if (Random.Range(0.0f, 1.0f) < 0.5f)
        {
            entity.AddStatusEffect(StatusEffectManager.BurnEffect(melee.owner, 5.0f, 0.1f, 0.5f));
        }
    }
}
