using System.Collections.Generic;
using UnityEngine;

public class DefaultRelicEffect : RelicEffect
{
    [SerializeField] private RelicName relic;

    public override LivingEntity.StatModifier GenerateStatModifier(GameManager.GameContext context)
    {
        // Common relics
        if (relic == RelicName.Apple)
        {
            return FixedStatModifier.Create(context)
                .AdditionValue("health", 1.0f);
        }
        if (relic == RelicName.Milk)
        {
            return FixedStatModifier.Create(context)
                .AdditionValue("shield", 1.0f);
        }
        if (relic == RelicName.Candy)
        {
            return FixedStatModifier.Create(context)
                .AdditionValue("speed", 1.0f);
        }
        if (relic == RelicName.OrangeJuice)
        {
            return FixedStatModifier.Create(context)
                .AdditionValue("attackSpeed", 1.0f);
        }
        if (relic == RelicName.LuckyClover)
        {
            return FixedStatModifier.Create(context)
                .AdditionValue("luck", 1.0f);
        }
        if (relic == RelicName.BrassBullet)
        {
            return FixedStatModifier.Create(context)
                .AdditionValue("bulletCount", 1.0f);
        }
        if (relic == RelicName.Monocle)
        {
            return FixedStatModifier.Create(context)
                .AdditionValue("critChance", 1.0f)
                .AdditionValue("critDamage", 0.1f);
        }
        if (relic == RelicName.AmuletOfForce)
        {
            return FixedStatModifier.Create(context)
                .AdditionValue("attackDamage", 1.0f)
                .AdditionValue("meleeRange", 1.0f);
        }

        // Rare relics
        if (relic == RelicName.GoldenApple)
        {
            return FixedStatModifier.Create(context)
                .AdditionValue("health", 2.0f);
        }
        if (relic == RelicName.ChainmailArmor)
        {
            return FixedStatModifier.Create(context)
                .AdditionValue("shield", 2.0f);
        }
        if (relic == RelicName.HellsteelBlade)
        {
            return HellsteelStatModifier.Create(context)
                .AdditionValue("attackDamage", 0.2f);
        }
        if (relic == RelicName.HellsteelArmor)
        {
            return HellsteelStatModifier.Create(context)
                .AdditionValue("shield", 0.2f);
        }
        if (relic == RelicName.HellsteelDagger)
        {
            return HellsteelStatModifier.Create(context)
                .AdditionValue("critChance", 2.0f)
                .AdditionValue("critDamage", 0.25f);
        }
        if (relic == RelicName.TacticalScope)
        {
            return FixedStatModifier.Create(context)
                .AdditionValue("bulletPrecision", 1.0f);
        }
        if (relic == RelicName.ExtendedClip)
        {
            return FixedStatModifier.Create(context)
                .AdditionValue("bulletCapacity", 2.0f);
        }
        if (relic == RelicName.HelpingHand)
        {
            return FixedStatModifier.Create(context)
                .AdditionValue("reloadSpeed", 1.0f);
        }

        // Cursed relics
        if (relic == RelicName.DoubleTrouble)
        {
            return FixedStatModifier.Create(context)
                .Multiplication("attackDamage", 2.0f)
                .Multiplication("health", 0.5f);
        }
        if (relic == RelicName.DullNeedle)
        {
            return FixedStatModifier.Create(context)
                .AdditionValue("critChance", 20.0f)
                .Multiplication("critDamage", 0.5f);
        }
        if (relic == RelicName.CorruptedClover)
        {
            return FixedStatModifier.Create(context)
                .AdditionValue("luck", 2.0f)
                .AdditionValue("critChance", 20.0f);
        }
        if (relic == RelicName.SilverBullet)
        {
            // Silver bullet doesn't change any stat
        }
        if (relic == RelicName.LeadBullet)
        {
            return FixedStatModifier.Create(context)
                .AdditionValue("attackDamage", 2.0f)
                .Multiplication("bulletSpeed", 0.8f)
                .Multiplication("bulletLifeTime", 0.8f);
        }
        if (relic == RelicName.AluminiumBullet)
        {
            return FixedStatModifier.Create(context)
                .AdditionValue("attackDamage", -2.0f)
                .Multiplication("bulletSpeed", 1.2f)
                .Multiplication("bulletLifeTime", 1.2f);
        }

        // If unknown or not implemented, return default modifier
        return base.GenerateStatModifier(context);
    }

    public override LivingEntity.AttackModifier GenerateAttackDealtModifier(GameManager.GameContext context)
    {
        if (relic == RelicName.SilverBullet)
        {
            return ConditionalAttackModifier.Create(context)
                .If(
                    ConditionalAttackModifier.Condition.Target().
                        HasTag("Undead"), 
                    FixedAttackModifier.Create(context)
                        .DamageAdditionValue(2.0f)
                )
                .Else(
                    FixedAttackModifier.Create(context)
                        .DamageAdditionValue(-2.0f)
                );
        }
        if (relic == RelicName.EternalFlame)
        {
            return FixedAttackModifier.Create(context)
                .InflictStatusEffect("status_effect_burn", 2.0f, 1);
        }
        if (relic == RelicName.PermafrostIceCube)
        {
            return FixedAttackModifier.Create(context)
                .InflictStatusEffect("status_effect_frozen", 0.5f, 1);
        }

        return base.GenerateAttackDealtModifier(context);
    }

    public override LivingEntity.AttackModifier GenerateAttackReceivedModifier(GameManager.GameContext context)
    {
        if (relic == RelicName.GoldenApple)
        {
            return new GoldenAppleAttackReceivedModifier();
        }
        if (relic == RelicName.ChainmailArmor)
        {
            return FixedAttackModifier.Create(context)
                .CritNegation();
        }

        return base.GenerateAttackReceivedModifier(context);
    }

    public enum RelicName
    {
        Unknown = -1,

        Apple,
        Milk,
        Candy,
        OrangeJuice,
        LuckyClover,
        BrassBullet,
        Monocle,
        AmuletOfForce,

        GoldenApple,
        ChainmailArmor,
        HellsteelBlade,
        HellsteelArmor,
        HellsteelDagger,
        TacticalScope,
        ExtendedClip,
        HelpingHand,
        EternalFlame,
        PermafrostIceCube,

        DoubleTrouble,
        DullNeedle,
        CorruptedClover,
        SilverBullet,
        LeadBullet,
        AluminiumBullet
    }
}

public class FixedStatModifier : LivingEntity.StatModifier
{
    private List<LivingEntity.StatModifyingOperation> m_operations = new List<LivingEntity.StatModifyingOperation>();

    public override List<LivingEntity.StatModifyingOperation> Modify()
    {
        List<LivingEntity.StatModifyingOperation> res = new List<LivingEntity.StatModifyingOperation>();
        res.AddRange(m_operations);
        return res;
    }

    private FixedStatModifier(GameManager.GameContext context)
    {
    }

    public static FixedStatModifier Create(GameManager.GameContext context)
    {
        return new FixedStatModifier(context);
    }

    public FixedStatModifier AdditionPercent(string stat, float amount)
    {
        m_operations.Add(LivingEntity.StatModifyingOperation.AdditionPercent(stat, amount));
        return this;
    }

    public FixedStatModifier AdditionValue(string stat, float amount)
    {
        m_operations.Add(LivingEntity.StatModifyingOperation.AdditionValue(stat, amount));
        return this;
    }

    public FixedStatModifier Multiplication(string stat, float amount)
    {
        m_operations.Add(LivingEntity.StatModifyingOperation.Multiplication(stat, amount));
        return this;
    }
}

public class HellsteelStatModifier : Relic.PlayerAwaredStatModifier
{
    private List<LivingEntity.StatModifyingOperation> m_operations = new List<LivingEntity.StatModifyingOperation>();

    public override List<LivingEntity.StatModifyingOperation> Modify()
    {
        List<LivingEntity.StatModifyingOperation> res = new List<LivingEntity.StatModifyingOperation>();

        int curseRelicCount = p_CurseRelicCount();
        foreach (LivingEntity.StatModifyingOperation operation in m_operations)
        {
            LivingEntity.StatModifyingOperation o = operation;
            o.amount *= curseRelicCount;
            res.Add(o);
        }
        return res;
    }

    private HellsteelStatModifier(GameManager.GameContext context) : base(context.player)
    {
    }

    public static HellsteelStatModifier Create(GameManager.GameContext context)
    {
        return new HellsteelStatModifier(context);
    }

    public HellsteelStatModifier AdditionPercent(string stat, float amount)
    {
        m_operations.Add(LivingEntity.StatModifyingOperation.AdditionPercent(stat, amount));
        return this;
    }

    public HellsteelStatModifier AdditionValue(string stat, float amount)
    {
        m_operations.Add(LivingEntity.StatModifyingOperation.AdditionValue(stat, amount));
        return this;
    }

    public HellsteelStatModifier Multiplication(string stat, float amount)
    {
        m_operations.Add(LivingEntity.StatModifyingOperation.Multiplication(stat, amount));
        return this;
    }

    private int p_CurseRelicCount()
    {
        return player.cursedRelicCount;
    }
}

public class FixedAttackModifier : LivingEntity.AttackModifier
{
    private List<LivingEntity.AttackModifyingOperation> m_operations = new List<LivingEntity.AttackModifyingOperation>();

    public override List<LivingEntity.AttackModifyingOperation> Modify(LivingEntity.AttackInfo attackInfo, LivingEntity.AttackContext attackContext)
    {
        return m_operations;
    }

    private FixedAttackModifier(GameManager.GameContext context)
    {
    }

    public static FixedAttackModifier Create(GameManager.GameContext context)
    {
        return new FixedAttackModifier(context);
    }

    public FixedAttackModifier CritNegation()
    {
        m_operations.Add(new LivingEntity.AttackModifyingOperation(LivingEntity.AttackModifyingOperation.Operation.CritNegation));
        return this;
    }

    public FixedAttackModifier CarryOverDamage(bool enabled)
    {
        m_operations.Add(new LivingEntity.AttackModifyingOperation(LivingEntity.AttackModifyingOperation.Operation.CarryOverDamage, enabled ? 1.0f : 0.0f));
        return this;
    }

    public FixedAttackModifier DamageAdditionPercent(float amount)
    {
        m_operations.Add(new LivingEntity.AttackModifyingOperation(LivingEntity.AttackModifyingOperation.Operation.DamageAdditionPercent, amount));
        return this;
    }

    public FixedAttackModifier DamageAdditionValue(float amount)
    {
        m_operations.Add(new LivingEntity.AttackModifyingOperation(LivingEntity.AttackModifyingOperation.Operation.DamageAdditionValue, amount));
        return this;
    }

    public FixedAttackModifier DamageMultiplication(float amount)
    {
        m_operations.Add(new LivingEntity.AttackModifyingOperation(LivingEntity.AttackModifyingOperation.Operation.DamageMultiplication, amount));
        return this;
    }

    public FixedAttackModifier KnockbackAdditionPercent(float amount)
    {
        m_operations.Add(new LivingEntity.AttackModifyingOperation(LivingEntity.AttackModifyingOperation.Operation.KnockbackAdditionPercent, amount));
        return this;
    }

    public FixedAttackModifier KnockbackAdditionValue(float amount)
    {
        m_operations.Add(new LivingEntity.AttackModifyingOperation(LivingEntity.AttackModifyingOperation.Operation.KnockbackAdditionValue, amount));
        return this;
    }

    public FixedAttackModifier KnockbackMultiplication(float amount)
    {
        m_operations.Add(new LivingEntity.AttackModifyingOperation(LivingEntity.AttackModifyingOperation.Operation.KnockbackMultiplication, amount));
        return this;
    }

    public FixedAttackModifier StunTimeAdditionPercent(float amount)
    {
        m_operations.Add(new LivingEntity.AttackModifyingOperation(LivingEntity.AttackModifyingOperation.Operation.StunTimeAdditionPercent, amount));
        return this;
    }

    public FixedAttackModifier StunTimeAdditionValue(float amount)
    {
        m_operations.Add(new LivingEntity.AttackModifyingOperation(LivingEntity.AttackModifyingOperation.Operation.StunTimeAdditionValue, amount));
        return this;
    }

    public FixedAttackModifier StunTimeMultiplication(float amount)
    {
        m_operations.Add(new LivingEntity.AttackModifyingOperation(LivingEntity.AttackModifyingOperation.Operation.StunTimeMultiplication, amount));
        return this;
    }

    public FixedAttackModifier InflictStatusEffect(string effectId, float time, int level)
    {
        m_operations.Add(new LivingEntity.AttackModifyingOperation(LivingEntity.AttackModifyingOperation.Operation.InflictStatusEffect, effectId, time, level));
        return this;
    }
}

public class ConditionalAttackModifier : LivingEntity.AttackModifier
{
    private List<KeyValuePair<Condition, FixedAttackModifier>> m_ifModifiers = new List<KeyValuePair<Condition, FixedAttackModifier>>();
    private FixedAttackModifier m_elseModifier;

    public override List<LivingEntity.AttackModifyingOperation> Modify(LivingEntity.AttackInfo attackInfo, LivingEntity.AttackContext attackContext)
    {
        foreach (var p in m_ifModifiers)
        {
            if (p.Key.Check(attackContext))
            {
                return p.Value.Modify(attackInfo, attackContext);
            }
        }
        if (m_elseModifier != null)
        {
            return m_elseModifier.Modify(attackInfo, attackContext);
        }
        return base.Modify(attackInfo, attackContext);
    }

    private ConditionalAttackModifier(GameManager.GameContext context)
    {
    }

    public static ConditionalAttackModifier Create(GameManager.GameContext context)
    {
        return new ConditionalAttackModifier(context);
    }

    public ConditionalAttackModifier If(Condition condition, FixedAttackModifier modifier)
    {
        m_ifModifiers.Add(new KeyValuePair<Condition, FixedAttackModifier>(condition, modifier));
        return this;
    }

    public ConditionalAttackModifier Else(FixedAttackModifier modifier)
    {
        m_elseModifier = modifier;
        return this;
    }

    public struct Condition
    {
        public Type type;
        public bool isAttacker;
        public string[] tags;

        private Condition(bool isAttacker)
        {
            this.type = Type.Unknown;
            this.isAttacker = isAttacker;
            this.tags = new string[] { };
        }

        public static Condition Attacker()
        {
            return new Condition(true);
        }

        public static Condition Target()
        {
            return new Condition(false);
        }

        public Condition HasTag(string tag)
        {
            this.type = Type.Has;
            this.tags = new string[] { tag };
            return this;
        }

        public Condition HasAllTags(string[] tags)
        {
            this.type = Type.HasAll;
            this.tags = tags;
            return this;
        }

        public Condition HasAnyTags(string[] tags)
        {
            this.type = Type.HasAny;
            this.tags = tags;
            return this;
        }

        public bool Check(LivingEntity.AttackContext context)
        {
            LivingEntity targetedEntity = isAttacker ? context.attacker : context.target;
            if (targetedEntity == null)
            {
                return false;
            }
            switch (type)
            {
                case Type.Has:
                    return targetedEntity.HasTag(tags[0]);
                case Type.HasAll:
                    return targetedEntity.HasTagsAll(tags);
                case Type.HasAny:
                    return targetedEntity.HasTagsAny(tags);
                default:
                    return false;
            }
        }

        public enum Type
        {
            Unknown = -1,
            Has,
            HasAll,
            HasAny
        }
    }
}

public class GoldenAppleAttackReceivedModifier : LivingEntity.AttackModifier
{
    public override List<LivingEntity.AttackModifyingOperation> Modify(LivingEntity.AttackInfo attackInfo, LivingEntity.AttackContext attackContext)
    {

        if (attackContext.target != null)
        {
            List<LivingEntity.AttackModifyingOperation> res = new List<LivingEntity.AttackModifyingOperation>();
            float health = attackContext.target.statSet.GetValue("health");
            if (attackInfo.damage > 0.1f * health)
            {
                float p = (0.1f * health) / attackInfo.damage;
                res.Add(new LivingEntity.AttackModifyingOperation(LivingEntity.AttackModifyingOperation.Operation.DamageMultiplication, p));
                return res;
            }
        }
        
        return base.Modify(attackInfo, attackContext);
    }
}
