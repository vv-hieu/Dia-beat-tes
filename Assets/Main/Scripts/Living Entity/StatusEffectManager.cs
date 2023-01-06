using System.Collections.Generic;
using UnityEngine;

public class StatusEffectManager : MonoBehaviour
{
    [Header("VFX")]
    [SerializeField] private GameObject burnVFX;
    [SerializeField] private GameObject frozenVFX;
    [SerializeField] private GameObject frenzyVFX;

    public static StatusEffectManager instance;

    public static LivingEntity.StatusEffectInflictor Get(string id)
    {
        if (id == "status_effect_burn")
        {
            return p_BurnEffect;
        }
        if (id == "status_effect_frozen")
        {
            return p_FrozenEffect;
        }
        if (id == "status_effect_frenzy")
        {
            return p_FrenzyEffect;
        }
        return null;
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

    private static LivingEntity.StatusEffect p_BurnEffect(LivingEntity inflictor, float time, int level)
    {
        string[] tags = new string[] { "StatusEffect", "Fire" };

        LivingEntity.AttackInfo burnDamageAttackInfo = LivingEntity.AttackInfo.Create()
            .Damage(0.25f * level)
            .Tags(tags);

        return GeneralStatusEffect.Create("status_effect_burn", inflictor, time)
            .AttachVFX(instance.burnVFX)
            .DamageOverTime(burnDamageAttackInfo, 0.5f, new string[] { "StatusEffect", "Fire" }, false);
    }

    private static LivingEntity.StatusEffect p_FrozenEffect(LivingEntity inflictor, float time, int level)
    {
        return GeneralStatusEffect.Create("status_effect_frozen", inflictor, time)
            .AttachVFX(instance.frozenVFX)
            .RemoveOnHurt()
            .RemoveTargetControl();
    }

    private static LivingEntity.StatusEffect p_FrenzyEffect(LivingEntity inflictor, float time, int level)
    {
        return GeneralStatusEffect.Create("status_effect_frenzy", inflictor, time)
            .AttachVFX(instance.frenzyVFX)
            .ModifyStat(FixedStatModifier.Create(GameManager.GetGameContext())
                .Multiplication("attackSpeed", (1.0f + level * 0.5f))
                .Multiplication("reloadSpeed", (1.0f + level * 0.5f)));
    }

    private class GeneralStatusEffect : LivingEntity.StatusEffect
    {
        private LivingEntity.StatModifier m_statModifier = new LivingEntity.StatModifier();

        private bool m_removeOnHurt = false;

        private GameObject m_vfx;
        private GameObject m_vfxInstance;
                                        
        private bool                    m_damageOverTimeEnabled    = false;
        private bool                    m_damageOverTimeCrit       = false;
        private LivingEntity.AttackInfo m_damageOverTimeAttackInfo = LivingEntity.AttackInfo.Create();
        private float                   m_damageOverTimeCycle      = 0.0f;
        private float                   m_damageOverTimeTime       = 0.0f;
                                        
        private bool m_removeTargetControl = false;

        private GeneralStatusEffect(string id, LivingEntity inflictor, float time) : base(id, inflictor, time)
        {
        }

        public override LivingEntity.StatModifier GetStatModifier()
        {
            return m_statModifier;
        }

        public override bool ShouldBeRemovedOnHurt()
        {
            return m_removeOnHurt;
        }

        public override void OnApply(LivingEntity target)
        {
            if (m_vfx != null)
            {
                m_vfxInstance = Instantiate(m_vfx, target.VFXPivot());
            }

            if (m_removeTargetControl)
            {
                target.SetInControl(false);
            }
        }

        public override void OnRemove(LivingEntity target)
        {
            if (m_vfxInstance != null)
            {
                ParticleSystem particle = m_vfxInstance.GetComponent<ParticleSystem>();
                if (particle != null)
                {
                    ParticleSystem.MainModule main = particle.main;
                    particle.Stop();
                    m_vfxInstance.transform.parent = target.transform.parent;
                    Destroy(m_vfxInstance, main.duration);
                }
                else
                {
                    Destroy(m_vfxInstance);
                }
            }

            if (m_removeTargetControl)
            {
                target.SetInControl(true);
            }
        }

        public override void OnUpdate(LivingEntity target)
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
                m_damageOverTimeTime += Time.deltaTime;
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

        public GeneralStatusEffect RemoveOnHurt()
        {
            m_removeOnHurt = true;
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
    
        public GeneralStatusEffect RemoveTargetControl()
        {
            m_removeTargetControl = true;
            return this;
        }
    }
}
