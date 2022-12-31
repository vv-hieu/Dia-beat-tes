using System.Collections.Generic;
using UnityEngine;

public class StatusEffectManager : MonoBehaviour
{
    [Header("VFX")]
    [SerializeField] private GameObject burnVFX;
    [SerializeField] private GameObject frenzyVFX;

    public static StatusEffectManager instance;

    public static LivingEntity.StatusEffect BurnEffect(LivingEntity inflictor, float time, float burnDamage, float burnCycle)
    {
        string[] tags = new string[] { "StatusEffect", "Fire" };

        LivingEntity.AttackInfo burnDamageAttackInfo = LivingEntity.AttackInfo.Create()
            .Damage(burnDamage)
            .Tags(tags);

        return GeneralStatusEffect.Create("status_effect_burn", inflictor, time)
            .AttachVFX(instance.burnVFX)
            .DamageOverTime(burnDamageAttackInfo, burnCycle, new string[] { "StatusEffect", "Fire" }, false);
    }

    public static LivingEntity.StatusEffect FrenzyEffect(LivingEntity inflictor, float time)
    {
        return GeneralStatusEffect.Create("status_effect_frenzy", inflictor, time)
            .AttachVFX(instance.frenzyVFX);
    }

    private void Start()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Debug.LogError("There can only be one instance of StatusEffectManager.");
        }
    }

    private class GeneralStatusEffect : LivingEntity.StatusEffect
    {
        private LivingEntity.StatModifier m_statModifier             = new LivingEntity.StatModifier();
        private GameObject                m_vfx;
        private GameObject                m_vfxInstance;
        private bool                      m_damageOverTimeEnabled    = false;
        private bool                      m_damageOverTimeCrit       = false;
        private LivingEntity.AttackInfo   m_damageOverTimeAttackInfo = LivingEntity.AttackInfo.Create();
        private float                     m_damageOverTimeCycle      = 0.0f;
        private float                     m_damageOverTimeTime       = 0.0f;

        private GeneralStatusEffect(string id, LivingEntity inflictor, float time) : base(id, inflictor, time)
        {
        }

        public override LivingEntity.StatModifier GetStatModifier()
        {
            return m_statModifier;
        }

        public override void OnApply(LivingEntity target)
        {
            if (m_vfx != null)
            {
                m_vfxInstance = Instantiate(m_vfx, target.VFXPivot());
            }
        }

        public override void OnRemove(LivingEntity target)
        {
            if (m_vfxInstance != null)
            {
                Destroy(m_vfxInstance);
            }
        }

        public override void OnUpdate(LivingEntity target, float dt)
        {
            if (m_damageOverTimeEnabled)
            {
                if (m_damageOverTimeTime >= m_damageOverTimeCycle)
                {
                    m_damageOverTimeTime = 0.0f;
                }
                if (m_damageOverTimeTime <= 0.0f)
                {
                    LivingEntity.HandleAttack(inflictor, target, m_damageOverTimeAttackInfo, m_damageOverTimeCrit);
                }
                m_damageOverTimeTime += dt;
            }
        }

        public static GeneralStatusEffect Create(string id, LivingEntity inflictor, float time)
        {
            return new GeneralStatusEffect(id, inflictor, time);
        }
    
        public GeneralStatusEffect AttachVFX(GameObject vfx)
        {
            m_vfx = vfx;
            return this;
        }

        public GeneralStatusEffect ModifyStat(LivingEntity.StatModifier modifier)
        {
            m_statModifier = modifier;
            return this;
        }

        public GeneralStatusEffect DamageOverTime(LivingEntity.AttackInfo damage, float cycle, IEnumerable<string> tags, bool crit)
        {
            m_damageOverTimeEnabled    = true;
            m_damageOverTimeCrit       = crit;
            m_damageOverTimeCycle      = cycle;
            m_damageOverTimeAttackInfo = damage;
            return this;
        }
    }
}
