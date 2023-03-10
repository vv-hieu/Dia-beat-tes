using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PierceWeapon : MeleeWeapon
{
    [Header("Pierce Attack")]
    [SerializeField] private GameObject pierceAttack;
    [SerializeField] private float      retract        = 0.2f;
    [SerializeField] private float      extend         = 0.5f;
    [SerializeField] private float      pierceSize     = 1.0f;
    [SerializeField] private float      pierceDistance = 1.0f;

    [Header("Weapon Stats")]
    [SerializeField] private float damage;
    [SerializeField] private float knockback;
    [SerializeField] private float stunTime;
    [SerializeField] private float attackTime;
    [SerializeField] private float activateTime;

    [Header("Tags")]
    [SerializeField] private string[] tags;

    private float m_time = 0.0f;

    public override void OnActivate()
    {
        OffsetTo(-retract, p_ActivateTime());
    }

    public override void OnCoolDown()
    {
        OffsetTo(0.0f, p_ActivateTime());
    }

    public override void OnUse()
    {
        if (pierceAttack != null)
        {
            GameObject go = Instantiate(pierceAttack, transform.position + new Vector3(direction.x, direction.y, 0.0f) * pierceDistance, Quaternion.identity, GetUser().transform.parent);
            Melee m = go.GetComponent<Melee>();
            if (m != null)
            {
                m.Init(GetUser(), direction, p_AttackTime(), p_MeleeRange(), false, GetAffectedTags(), p_OnMeleeHit);
            }
            m_time = 0.0f;
        }
        OffsetTo(extend, p_AttackTime());
    }

    public override void Activating()
    {
        m_time += GameStateManager.instance.currentState == GameState.Paused ? 0.0f : Time.deltaTime;
    }

    public override void CoolingDown()
    {
        m_time += GameStateManager.instance.currentState == GameState.Paused ? 0.0f : Time.deltaTime;
    }

    public override void Using()
    {
        m_time += GameStateManager.instance.currentState == GameState.Paused ? 0.0f : Time.deltaTime;
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
        if (m_time >= p_AttackTime())
        {
            m_time = 0.0f;
            return true;
        }
        return false;
    }

    public override Optional<float> ReachDistance()
    {
        return new Optional<float>(pierceSize + pierceDistance);
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

    private float p_MeleeRange()
    {
        float userMeleeRange = 0.0f;
        if (GetUser() != null)
        {
            userMeleeRange = GetUser().statSet.GetValue("meleeRange");
        }
        return pierceSize * (1.0f + userMeleeRange * 0.2f);
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
    }
}
