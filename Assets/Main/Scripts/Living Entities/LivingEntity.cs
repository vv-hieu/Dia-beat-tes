using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.VisualScripting;

[ExecuteInEditMode]
[IncludeInSettings(true)]
public class LivingEntity : MonoBehaviour
{
    [Header("Initial Stats")]
    [SerializeField] private Optional<float> health;
    [SerializeField] private Optional<float> shield;
    [SerializeField] private Optional<float> speed;
    [SerializeField] private Optional<float> luck;
    [SerializeField] private Optional<float> attackDamage;
    [SerializeField] private Optional<float> attackSpeed;
    [SerializeField] private Optional<float> critChance;
    [SerializeField] private Optional<float> critDamage;
    [SerializeField] private Optional<float> bulletCount;
    [SerializeField] private Optional<float> bulletPrecision;
    [SerializeField] private Optional<float> bulletSpeed;
    [SerializeField] private Optional<float> bulletLifeTime;
    [SerializeField] private Optional<float> bulletCapacity;
    [SerializeField] private Optional<float> reloadSpeed;

    [Header("Entity Tags")]
    [SerializeField] private SerializableHashSet<string> entityTags = new SerializableHashSet<string>();

    [Header("Loot")]
    [SerializeField] private List<LootPool> lootTable = new List<LootPool>();

    [Header("Component References")]
    [SerializeField] private SpriteRenderer m_sprite;

    [Header("Visual")]
    [SerializeField] private Material defaultMaterial;
    [SerializeField] private Material hitMaterial;

    public CritModifier       critDealtModifier          = null;
    public CritModifier       critReceivedModifier       = null;
    public AttackInfoModifier attackDealtModifier        = null;
    public AttackInfoModifier attackReceivedModifier     = null;

    public float   currentHealth { get; private set; } = 0.0f;
    public float   currentShield { get; private set; } = 0.0f;
    public StatSet statSet       { get; private set; }
    public bool    isMoving      { get; private set; } = false;
    public bool    isHurt        { get; private set; } = false;
    public bool    isFacingRight { get; private set; } = true;

    private NavMeshAgent m_navMeshAgent;
    private Rigidbody2D  m_rigidbody;
    private float        m_shieldTime       = 0.0f;
    private float        m_invulnerableTime = 0.0f;
    private float        m_stunTime         = 0.0f;
    private float        m_knockbackTime    = 0.0f;
    private float        m_hurtTime         = 0.0f;
    private float        m_frenzyTime       = 0.0f;
    private bool         m_isKnockedback    = false;
    private Vector2      m_knockbackDir     = Vector2.zero;
    private Vector2      m_knockbackOrigin  = Vector2.zero;
    private Vector2      m_previousPosition = Vector2.zero;

    private static float         SHIELD_REGENERATE_TIME = 2.0f;
    private static float         HURT_TIME              = 0.1f;
    private static System.Random RANDOM                 = new System.Random();

    public static void HandleAttack(LivingEntity attacker, LivingEntity target, float critChance, float critDamage, AttackInfo attackInfo)
    {
        if (target != null)
        {
            if (attacker != null && attacker.critDealtModifier != null)
            {
                attacker.critDealtModifier(ref critChance, ref critDamage, target);
            }
            if (target.critReceivedModifier != null)
            {
                target.critReceivedModifier(ref critChance, ref critDamage, target);
            }
            critChance = Mathf.Clamp01(critChance * 0.01f);
            attackInfo.critDamage = (float)RANDOM.NextDouble() <= critChance ? critDamage : 0.0f;
            attackInfo.damage += attackInfo.critDamage;

            if (attacker != null && attacker.attackDealtModifier != null)
            {
                attacker.attackDealtModifier(attackInfo, target);
            }
            if (target.attackReceivedModifier != null)
            {
                target.attackReceivedModifier(attackInfo, attacker);
            }
            attackInfo.damage = Mathf.Max(0.0f, attackInfo.damage);

            target.p_ReceiveAttack(attackInfo);
        }
    }

    public bool HasTag(string tag)
    {
        return entityTags.Contains(tag);
    }

    public bool HasTagsAll(string[] tags)
    {
        foreach (string tag in tags)
        {
            if (!entityTags.Contains(tag))
            {
                return false;
            }
        }
        return true;
    }

    public bool HasTagsAny(string[] tags)
    {
        foreach (string tag in tags)
        {
            if (entityTags.Contains(tag))
            {
                return true;
            }
        }
        return false;
    }

    public void OnDeath()
    {
        System.Random random = new System.Random();
        float luck = 0.0f;
        if (GameObject.FindGameObjectWithTag("Player").GetComponent<LivingEntity>().statSet.TryGetValue("luck", out float l))
        {
            luck = l;
        }

        List<string> itemList = new List<string>();
        foreach (LootPool pool in lootTable)
        {
            pool.AddItems(itemList, random, luck);
        }
        ListUtility.Shuffle(itemList, random);

        float index = 0.5f;
        foreach (string itemName in itemList)
        {
            float r     = Mathf.Sqrt(index / itemList.Count) * 1.5f;
            float theta = 10.1665f * index;
            GameObject collectible = ItemManager.instance.GetCollectible(itemName);
            if (collectible != null)
            {
                Instantiate(collectible, transform.position + new Vector3(Mathf.Cos(theta), Mathf.Sin(theta), 0.0f) * r, Quaternion.identity, transform.parent);
            }
            index += 1.0f;
        }
    }

    public void Heal(float amount)
    {
        float maxHealth = 0.0f;
        if (statSet.TryGetValue("health", out float h))
        {
            maxHealth = h;
        }
        currentHealth = Mathf.Clamp(currentHealth + amount, 0.0f, maxHealth);
    }

    private void Start()
    {
        // Get components
        m_navMeshAgent = GetComponent<NavMeshAgent>();
        m_rigidbody    = GetComponent<Rigidbody2D>();

        // Build stat set
        statSet = p_BuildStatSet();

        if (statSet.TryGetValue("health", out float h))
        {
            currentHealth = h;
        }
        if (statSet.TryGetValue("shield", out float s))
        {
            currentShield = s;
        }

        m_previousPosition = new Vector2(transform.position.x, transform.position.y);
    }

    private void OnValidate()
    {
        statSet = p_BuildStatSet();

        if (statSet.TryGetValue("health", out float h))
        {
            currentHealth = h;
        }
        if (statSet.TryGetValue("shield", out float s))
        {
            currentShield = s;
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        if (m_navMeshAgent != null && statSet != null && statSet.TryGetValue("speed", out float speed))
        {
            m_navMeshAgent.speed = speed;
        }

        float shield = 0.0f;
        statSet.TryGetValue("shield", out shield);

        if (currentShield < shield)
        {
            m_shieldTime += dt;
            while (m_shieldTime >= SHIELD_REGENERATE_TIME)
            {
                m_shieldTime -= SHIELD_REGENERATE_TIME;
                float s = 0.0f;
                statSet.TryGetValue("shield", out s);
                currentShield = Mathf.Min(currentShield + 1.0f, s);
            }
        }
        else
        {
            m_shieldTime = 0.0f;
        }

        m_invulnerableTime = Mathf.Max(0.0f, m_invulnerableTime - dt);
        m_stunTime         = Mathf.Max(0.0f, m_stunTime - dt);
        if (m_navMeshAgent != null)
        {
            m_navMeshAgent.enabled = (m_stunTime <= 0.0f);
        }

        isHurt = (m_hurtTime > 0.0f);
        m_hurtTime -= dt;

        m_sprite.material = (isHurt ? hitMaterial : defaultMaterial);

        if (m_frenzyTime > 0.0f)
        {

        }

        m_frenzyTime = Mathf.Max(0.0f, m_frenzyTime - dt);
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        if (m_isKnockedback)
        {
            m_rigidbody.MovePosition(m_knockbackOrigin + m_knockbackDir * Mathf.Pow(Mathf.Clamp01(m_knockbackTime * 10.0f), 0.5f));
            m_knockbackTime += dt;

            if (m_knockbackTime >= 0.1f)
            {
                m_isKnockedback = false;
                m_knockbackTime = 0.0f;
            }
        }

        Vector2 v = m_previousPosition - new Vector2(transform.position.x, transform.position.y);
        if (v.x < -0.01f)
        {
            isFacingRight = false;
        }
        else if (v.x > 0.01f)
        {
            isFacingRight = true;
        }
        m_sprite.flipX = isFacingRight;
        float d = v.magnitude / dt;
        isMoving = (d >= 0.001f);
        m_previousPosition = new Vector2(transform.position.x, transform.position.y);
    }

    private StatSet p_BuildStatSet()
    {
        return StatSet.NewStatSet()
            .AddStat("health"         , new Stat( 1.0f,  10.0f, health))
            .AddStat("shield"         , new Stat( 0.0f,   5.0f, shield))
            .AddStat("speed"          , new Stat( 0.1f,   5.0f, speed))
            .AddStat("luck"           , new Stat(-2.0f,   2.0f, luck))
            .AddStat("attackDamage"   , new Stat( 0.1f,  50.0f, attackDamage))
            .AddStat("attackSpeed"    , new Stat( 0.1f,   5.0f, attackSpeed))
            .AddStat("critChance"     , new Stat( 0.0f, 100.0f, critChance))
            .AddStat("critDamage"     , new Stat( 0.1f,  50.0f, critDamage))
            .AddStat("bulletCount"    , new Stat( 1.0f,  20.0f, bulletCount))
            .AddStat("bulletPrecision", new Stat( 0.0f, 100.0f, bulletPrecision))
            .AddStat("bulletSpeed"    , new Stat( 1.0f,  50.0f, bulletSpeed))
            .AddStat("bulletLifeTime" , new Stat( 1.0f,  10.0f, bulletLifeTime))
            .AddStat("bulletCapacity" , new Stat( 1.0f,  10.0f, bulletCapacity))
            .AddStat("reloadSpeed"    , new Stat( 0.1f,   5.0f, reloadSpeed));
    }

    private void p_ReceiveAttack(AttackInfo attackInfo)
    {
        // Skip if target is invulnerabe
        if (m_invulnerableTime > 0.0f)
        {
            return;
        }
        m_invulnerableTime = attackInfo.invulnerableTime;

        // Damage
        float damage = attackInfo.damage;
        if (currentShield > 0.0f)
        {
            float damage2 = damage - currentShield;
            currentShield = Mathf.Max(0.0f, -damage2);
            if (!attackInfo.carryOverDamage && damage2 >= 0.0f)
            {
                damage2 = 0.0f;
            }
            damage = damage2;
        }
        if (damage > 0.0f)
        {
            currentHealth -= damage;
        }

        // Stun
        m_stunTime = Mathf.Max(m_stunTime, attackInfo.stunTime);

        // Knockback
        m_isKnockedback   = true;
        m_knockbackDir    = attackInfo.knockbackDirection.normalized * attackInfo.knockback;
        m_knockbackOrigin = new Vector2(transform.position.x, transform.position.y);

        // Hurt
        m_hurtTime = HURT_TIME;
    }

    public struct Stat {
        public float minValue { get; private set; }
        public float maxValue { get; private set; }

        public float defaultValue { get; private set; }

        public Stat(float minValue, float maxValue)
        {
            this.minValue     = minValue;
            this.maxValue     = maxValue;
            this.defaultValue = minValue;
        }

        public Stat(float minValue, float maxValue, float defaultValue)
        {
            this.minValue     = minValue;
            this.maxValue     = maxValue;
            this.defaultValue = defaultValue;
        }

        public Stat(float minValue, float maxValue, Optional<float> defaultValue)
        {
            this.minValue     = minValue;
            this.maxValue     = maxValue;
            this.defaultValue = defaultValue.Enabled ? defaultValue.Value : minValue;
        }

        public float Clamp(float value)
        {
            return Mathf.Clamp(value, minValue, maxValue);
        }
    }

    [Serializable]
    public struct StatModifier
    {
        public Operation operation;
        public float     amount;

        public StatModifier(Operation operation, float amount)
        {
            this.operation = operation;
            this.amount    = amount;
        }

        public enum Operation
        {
            AdditionPercent,
            AdditionValue,
            Multiplication
        }
    }

    public class StatInstance
    {
        public Stat stat { get; private set; }

        public float baseValue { get; private set; }

        public float value { get; private set; }

        private Dictionary<string, StatModifier> m_modifiers = new Dictionary<string, StatModifier>();

        public StatInstance(Stat stat)
        {
            this.stat      = stat;
            this.baseValue = stat.defaultValue;
            p_Validate();
        }

        public StatInstance(Stat stat, float baseValue)
        {
            this.stat      = stat;
            this.baseValue = stat.Clamp(baseValue);
            p_Validate();
        }

        public void AddModifier(string id, StatModifier modifier)
        {
            m_modifiers.Add(id, modifier);
            p_Validate();
        }

        public void RemoveModifier(string id)
        {
            m_modifiers.Remove(id);
            p_Validate();
        }

        private void p_Validate()
        {
            float a = 1.0f;
            float b = 0.0f;
            float c = 1.0f;

            foreach (StatModifier modifier in m_modifiers.Values)
            {
                if (modifier.operation == StatModifier.Operation.AdditionPercent)
                {
                    a += modifier.amount;
                }
                else if (modifier.operation == StatModifier.Operation.AdditionValue)
                {
                    b += modifier.amount;
                }
                else if (modifier.operation == StatModifier.Operation.Multiplication)
                {
                    c *= modifier.amount;
                }
            }

            value = stat.Clamp(c * (a * baseValue + b));
        }
    }

    public class StatSet
    {
        private Dictionary<string, StatInstance> m_statInstances = new Dictionary<string, StatInstance>();

        public static StatSet NewStatSet()
        {
            return new StatSet();
        }

        public StatSet AddStat(string statId, Stat stat)
        {
            m_statInstances.Add(statId, new StatInstance(stat));
            return this;
        }

        public StatSet AddStat(string statId, Stat stat, float baseValue)
        {
            m_statInstances.Add(statId, new StatInstance(stat, baseValue));
            return this;
        }

        public void AddModifier(string statId, string modifierId, StatModifier modifier)
        {
            if (m_statInstances.ContainsKey(statId))
            {
                m_statInstances[statId].AddModifier(modifierId, modifier);
            }
        }

        public void RemoveModifier(string statId, string modifierId)
        {
            if (m_statInstances.ContainsKey(statId))
            {
                m_statInstances[statId].RemoveModifier(modifierId);
            }
        }

        public bool TryGetBaseValue(string statId, out float baseValue)
        {
            if (m_statInstances.TryGetValue(statId, out StatInstance statInstance))
            {
                baseValue = statInstance.baseValue;
                return true;
            }
            baseValue = 0.0f;
            return false;
        }

        public bool TryGetValue(string statId, out float value)
        {
            if (m_statInstances.TryGetValue(statId, out StatInstance statInstance))
            {
                value = statInstance.value;
                return true;
            }
            value = 0.0f;
            return false;
        }
    }

    public class AttackInfo {
        public float           damage             = 0.0f;
        public float           critDamage         = 0.0f;
        public float           stunTime           = 0.0f;
        public float           invulnerableTime   = 0.0f;
        public float           knockback          = 0.0f;
        public Vector2         knockbackDirection = Vector2.zero;
        public bool            carryOverDamage    = false;
        public HashSet<string> tags               = new HashSet<string>();

        public static AttackInfo NewAttackInfo()
        {
            return new AttackInfo();
        }

        public AttackInfo SetDamage(float value)
        {
            damage = value;
            return this;
        }

        public AttackInfo SetStunTime(float value)
        {
            stunTime = value;
            return this;
        }

        public AttackInfo SetInvulnerableTime(float value)
        {
            invulnerableTime = value;
            return this;
        }

        public AttackInfo SetKnockback(float value)
        {
            knockback = value;
            return this;
        }

        public AttackInfo SetKnockbackDirection(Vector2 value)
        {
            knockbackDirection = value;
            return this;
        }

        public AttackInfo SetCarryOverDamage(bool value)
        {
            carryOverDamage = value;
            return this;
        }

        public AttackInfo AddTag(string tag)
        {
            tags.Add(tag);
            return this;
        }

        public AttackInfo RemoveTag(string tag)
        {
            tags.Remove(tag);
            return this;
        }
    }

    public delegate void AttackInfoModifier(AttackInfo attackInfo, LivingEntity livingEntity);
    public delegate void CritModifier(ref float critChance, ref float critDamage, LivingEntity livingEntity);

    [Serializable]
    public struct LootPool 
    {
        public float               chance;
        public int                 rolls;
        public List<LootPoolEntry> entries;

        public void AddItems(List<string> itemList, System.Random random, float luck)
        {
            float totalWeight = p_TotalWeight(luck);

            for (int i = 0; i < rolls; ++i)
            {
                float rand = (float)random.NextDouble();
                if (rand <= chance)
                {
                    float cumulativeWeight = 0.0f;
                    float rand2 = (float)random.NextDouble() * totalWeight;

                    foreach (LootPoolEntry entry in entries)
                    {
                        cumulativeWeight += entry.GetWeight(luck);
                        if (cumulativeWeight >= rand2)
                        {
                            int count = entry.GetRandomCount(random, luck);
                            while (count-- > 0)
                            {
                                itemList.Add(entry.item);
                            }
                            break;
                        }
                    }
                }
            }
        }

        private float p_TotalWeight(float luck)
        {
            float res = 0.0f;
            foreach (LootPoolEntry entry in entries)
            {
                res += entry.GetWeight(luck);
            }
            return res;
        }
    }

    [Serializable]
    public struct LootPoolEntry
    {
        public string     item;
        public float      weight;
        public Vector2Int count;
        public bool       weightAffectedByLuck;
        public bool       countAffectedByLuck;

        public float GetWeight(float luck)
        {
            if (!weightAffectedByLuck)
            {
                luck = 0.0f;
            }
            return weight * (1.0f + 0.519f * (float)Math.Tanh((double)luck));
        }

        public int GetRandomCount(System.Random random, float luck)
        {
            if (!countAffectedByLuck)
            {
                luck = 0.0f;
            }
            float rand01     = (float)random.NextDouble();
            float finalCount = Mathf.Pow(rand01, Mathf.Pow(2.0f, luck)) * (count.y - count.x + 0.99f) + count.x;
            return (int)finalCount;
        }
    }
}
