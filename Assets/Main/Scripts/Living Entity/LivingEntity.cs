using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.VisualScripting;

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
    [SerializeField] private Optional<float> bulletLifetime;
    [SerializeField] private Optional<float> bulletCapacity;
    [SerializeField] private Optional<float> reloadSpeed;
    [SerializeField] private Optional<float> meleeRange;

    [Header("Entity Tags")]
    [SerializeField] private List<string> entityTags = new List<string>();

    [Header("Loot")]
    [SerializeField] private LootTable lootTable;

    [Header("Weapon")]
    [SerializeField] private Transform weaponPivot;

    [Header("Visual")]
    [SerializeField] private Transform vfxPivot;
    [SerializeField] private Material  defaultMaterial;
    [SerializeField] private Material  hitMaterial;

    [Header("Audio")]
    [SerializeField] private AudioClip hurtSound;

    [Header("Component References")]
    [SerializeField] private Collider2D[]                                 colliders;
    [SerializeField] private SerializableDictionary<SpriteRenderer, bool> sprites = new SerializableDictionary<SpriteRenderer, bool>();

    public AttackModifier attackDealtModifier;
    public AttackModifier attackReceivedModifier;
    public DeathCallback  onDeath;
    public KillCallback   onKill;

    public float   currentHealth { get; private set; } = 0.0f;
    public float   currentShield { get; private set; } = 0.0f;
    public StatSet statSet       { get; private set; }
    public bool    isMoving      { get; private set; } = false;
    public bool    isHurt        { get; private set; } = false;
    public bool    isFacingRight { get; private set; } = true;
    public bool    isInControl   { get; private set; } = true;

    private NavMeshAgent                       m_navMeshAgent;
    private Rigidbody2D                        m_rigidbody;
    private Weapon                             m_weapon;
    private float                              m_shieldTime                          = 0.0f;
    private float                              m_invulnerableTime                    = 0.0f;
    private float                              m_stunTime                            = 0.0f;
    private float                              m_knockbackTime                       = 0.0f;
    private float                              m_hurtTime                            = 0.0f;
    private bool                               m_isKnockedback                       = false;
    private bool                               m_died                                = false;
    private Vector2                            m_knockbackDir                        = Vector2.zero;
    private Vector2                            m_knockbackOrigin                     = Vector2.zero;
    private Vector2                            m_previousPosition                    = Vector2.zero;
    private Dictionary<string, StatusEffect>   m_statusEffects                       = new Dictionary<string, StatusEffect>();
    private Dictionary<string, AttackModifier> m_statusEffectAttackDealtModifiers    = new Dictionary<string, AttackModifier>();
    private Dictionary<string, AttackModifier> m_statusEffectAttackReceivedModifiers = new Dictionary<string, AttackModifier>();

    private static float                                SHIELD_REGENERATE_TIME = 2.0f;
    private static float                                HURT_TIME              = 0.1f;
    private static System.Random                        RANDOM                 = new System.Random();
    private static Dictionary<Collider2D, LivingEntity> ENTITY_FROM_COLLIDER   = new Dictionary<Collider2D, LivingEntity>();

    public static LivingEntity FromCollider(Collider2D collider)
    {
        if (ENTITY_FROM_COLLIDER.TryGetValue(collider, out LivingEntity entity)) 
        {
            return entity;
        }
        return null;
    }

    public static void HandleAttack(LivingEntity attacker, LivingEntity target, AttackInfo attackInfo, bool enableCrit = true)
    {
        if (target != null)
        {
            if (target.m_invulnerableTime > 0.0f)
            {
                return;
            }

            float critChance = 0.0f;
            if (attacker != null)
            {
                critChance = attacker.statSet.GetValue("critChance");
            }
            attackInfo.critDamage = 0.0f;
            if ((float)RANDOM.NextDouble() * 100.0f <= critChance && enableCrit)
            {
                attackInfo.critDamage = attacker.statSet.GetValue("critDamage");
                attackInfo.damage += attackInfo.critDamage;
            }

            AttackContext context = new AttackContext(attacker, target);
            List<AttackModifyingOperation> operations = new List<AttackModifyingOperation>();

            if (attacker != null)
            {
                if (attacker.attackDealtModifier != null)
                {
                    operations.AddRange(attacker.attackDealtModifier.Modify(attackInfo, context));
                }
                foreach (AttackModifier attackModifier in attacker.m_statusEffectAttackDealtModifiers.Values)
                {
                    operations.AddRange(attackModifier.Modify(attackInfo, context));
                }
            }
            if (target.attackReceivedModifier != null)
            {
                operations.AddRange(target.attackReceivedModifier.Modify(attackInfo, context));
            }
            foreach (AttackModifier attackModifier in target.m_statusEffectAttackReceivedModifiers.Values)
            {
                operations.AddRange(attackModifier.Modify(attackInfo, context));
            }

            bool negateCrit = false;
            bool carryOver = attackInfo.carryOverDamage;

            float a0 = 1.0f;
            float b0 = 0.0f;
            float c0 = 1.0f;

            float a1 = 1.0f;
            float b1 = 0.0f;
            float c1 = 1.0f;

            float a2 = 1.0f;
            float b2 = 0.0f;
            float c2 = 1.0f;

            foreach (AttackModifyingOperation operation in operations)
            {
                switch (operation.operation)
                {
                    case AttackModifyingOperation.Operation.CritNegation:
                        negateCrit = true;
                        break;
                    case AttackModifyingOperation.Operation.CarryOverDamage:
                        carryOver = (operation.amount > 0.5f);
                        break;
                    case AttackModifyingOperation.Operation.DamageAdditionPercent:
                        a0 += operation.amount;
                        break;
                    case AttackModifyingOperation.Operation.DamageAdditionValue:
                        b0 += operation.amount;
                        break;
                    case AttackModifyingOperation.Operation.DamageMultiplication:
                        c0 *= operation.amount;
                        break;
                    case AttackModifyingOperation.Operation.KnockbackAdditionPercent:
                        a1 += operation.amount;
                        break;
                    case AttackModifyingOperation.Operation.KnockbackAdditionValue:
                        b1 += operation.amount;
                        break;
                    case AttackModifyingOperation.Operation.KnockbackMultiplication:
                        c1 *= operation.amount;
                        break;
                    case AttackModifyingOperation.Operation.StunTimeAdditionPercent:
                        a2 += operation.amount;
                        break;
                    case AttackModifyingOperation.Operation.StunTimeAdditionValue:
                        b2 += operation.amount;
                        break;
                    case AttackModifyingOperation.Operation.StunTimeMultiplication:
                        c2 *= operation.amount;
                        break;
                    case AttackModifyingOperation.Operation.InflictStatusEffect:
                        if (!attackInfo.tags.Contains("StatusEffect"))
                        {
                            attackInfo.ApplyEffect(operation.effectId, operation.amount, operation.level);
                        }
                        break;
                    default:
                        break;
                }
            }

            if (negateCrit)
            {
                attackInfo.damage -= attackInfo.critDamage;
                attackInfo.critDamage = 0.0f;
            }
            attackInfo.carryOverDamage = carryOver;

            attackInfo.damage    = Mathf.Max(0.0f, c0 * (a0 * attackInfo.damage + b0));
            attackInfo.knockback = Mathf.Max(0.0f, c1 * (a1 * attackInfo.knockback + b1));
            attackInfo.stunTime  = Mathf.Max(0.0f, c2 * (a2 * attackInfo.stunTime + b2));

            target.p_ReceiveAttack(attackInfo, context);
        }
    }

    public void SetInvulnerableTime(float amount)
    {
        m_invulnerableTime = amount;
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

        List<GameObject> itemList = new List<GameObject>();
        if (lootTable != null)
        {
            itemList = lootTable.Get(random, luck);
        }
        ListUtility.Shuffle(itemList, random);

        float index = 0.5f;
        foreach (GameObject item in itemList)
        {
            float r     = Mathf.Sqrt(index / itemList.Count) * 1.5f;
            float theta = 10.1665f * index;
            if (item != null)
            {
                Instantiate(item, transform.position + new Vector3(Mathf.Cos(theta), Mathf.Sin(theta), 0.0f) * r, Quaternion.identity, transform.parent);
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

    public void AddStatusEffect(StatusEffect effect)
    {
        StatusEffect existingEffect = null;
        if (m_statusEffects.TryGetValue(effect.id, out StatusEffect e))
        {
            existingEffect = e;
        }
        if (effect.CanOverride(existingEffect))
        {
            existingEffect?.ForceRemove(this);
            m_statusEffects[effect.id]                       = effect;
            m_statusEffectAttackDealtModifiers[effect.id]    = effect.GetAttackDealtModifier();
            m_statusEffectAttackReceivedModifiers[effect.id] = effect.GetAttackReceivedModifier();
        }
    }

    public void RemoveStatusEffect(string effectId)
    {
        StatusEffect existingEffect = null;
        if (m_statusEffects.TryGetValue(effectId, out StatusEffect e))
        {
            existingEffect = e;
        }
        existingEffect?.ForceRemove(this);
        m_statusEffects.Remove(effectId);
        m_statusEffectAttackDealtModifiers.Remove(effectId);
        m_statusEffectAttackReceivedModifiers.Remove(effectId);
    }

    public Transform VFXPivot()
    {
        if (vfxPivot != null)
        {
            return vfxPivot;
        }
        return transform;
    }

    public Transform WeaponPivot()
    {
        if (weaponPivot != null)
        {
            return weaponPivot;
        }
        return transform;
    }

    public void SetInControl(bool isInControl)
    {
        this.isInControl = isInControl;
    }

    public void SetWeapon(GameObject weaponObjectPrefab)
    {
        if (weaponObjectPrefab != null)
        {
            if (weaponObjectPrefab.TryGetComponent(out Weapon w))
            {
                GameObject weaponObject = Instantiate(weaponObjectPrefab, WeaponPivot().position, Quaternion.identity, WeaponPivot());
                Weapon weapon = weaponObject.GetComponent<Weapon>();
                if (weapon != null)
                {
                    weapon.SetUser(this);
                }
                if (m_weapon != null)
                {
                    Destroy(m_weapon.gameObject);
                }
                m_weapon = weapon;
            }
        }
        else
        {
            if (m_weapon != null)
            {
                Destroy(m_weapon.gameObject);
            }
            m_weapon = null;
        }
    }

    public void RemoveWeapon()
    {
        if (m_weapon != null)
        {
            Destroy(m_weapon.gameObject);
            m_weapon = null;
        }
    }

    public Weapon GetWeapon()
    {
        return m_weapon;
    }

    public bool UseWeapon(string[] affectedTags)
    {
        if (m_weapon != null && isInControl)
        {
            return m_weapon.Use(affectedTags);
        }
        return false;
    }

    public void AimWeaponAt(Vector2 target)
    {
        if (m_weapon != null)
        {
            m_weapon.AimAt(target);
        }
    }

    private void Awake()
    {
        m_navMeshAgent = GetComponent<NavMeshAgent>();
        m_rigidbody    = GetComponent<Rigidbody2D>();

        GameStateManager.instance.onGameStateChanged += p_OnGameStateChanged;

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

    private void OnDestroy()
    {
        foreach (Collider2D collider in colliders)
        {
            ENTITY_FROM_COLLIDER.Remove(collider);
        }
        GameStateManager.instance.onGameStateChanged -= p_OnGameStateChanged;
    }

    private void Start()
    {
        m_previousPosition = new Vector2(transform.position.x, transform.position.y);

        foreach (Collider2D collider in colliders)
        {
            ENTITY_FROM_COLLIDER[collider] = this;
        }
        if (TryGetComponent(out Collider2D selfCollider))
        {
            ENTITY_FROM_COLLIDER[selfCollider] = this;
        }
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
        currentHealth = Mathf.Clamp(currentHealth, 0.0f, statSet.GetValue("health"));

        if (m_navMeshAgent != null && statSet != null && statSet.TryGetValue("speed", out float speed))
        {
            m_navMeshAgent.speed = speed;
        }

        float shield = 0.0f;
        statSet.TryGetValue("shield", out shield);

        if (currentShield < shield)
        {
            m_shieldTime += GameStateManager.instance.currentState == GameState.Paused ? 0.0f : Time.deltaTime;
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

        m_invulnerableTime = Mathf.Max(0.0f, m_invulnerableTime - Time.deltaTime);
        m_stunTime         = Mathf.Max(0.0f, m_stunTime - Time.deltaTime);
        if (m_navMeshAgent != null)
        {
            m_navMeshAgent.enabled = (m_stunTime <= 0.0f && currentHealth > 0.0f && isInControl && (GameStateManager.instance.currentState != GameState.Paused));
        }

        isHurt = (m_hurtTime > 0.0f);
        m_hurtTime -= Time.deltaTime;

        foreach (var sprite in sprites)
        {
            sprite.Key.material = ((isHurt && currentHealth > 0.0f) ? hitMaterial : defaultMaterial);
        }

        foreach (StatusEffect effect in m_statusEffects.Values)
        {
            effect.Update(this);
        }
        List<string> finishedEffectIds = new List<string>();
        foreach (var p in m_statusEffects)
        {
            if (p.Value.ShouldBeRemoved())
            {
                finishedEffectIds.Add(p.Key);
            }
        }
        foreach (string effectId in finishedEffectIds)
        {
            m_statusEffects.Remove(effectId);
        }

        if (currentHealth <= 0.01f && !m_died)
        {
            RemoveWeapon();
            m_died = true;
            isInControl = false;
            OnDeath();
            if (onDeath != null)
            {
                onDeath();
            }
        }
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        if (m_isKnockedback)
        {
            if (m_rigidbody != null)
            {
                m_rigidbody.MovePosition(m_knockbackOrigin + m_knockbackDir * Mathf.Pow(Mathf.Clamp01(m_knockbackTime * 10.0f), 0.5f));
            }
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
        foreach (var sprite in sprites)
        {
            if (sprite.Value)
            {
                sprite.Key.flipX = isFacingRight;
            }
        }
        float d = v.magnitude / dt;
        isMoving = (d >= 0.001f);
        m_previousPosition = new Vector2(transform.position.x, transform.position.y);
    }
    
    private StatSet p_BuildStatSet()
    {
        return StatSet.NewStatSet()
            .AddStat("health"         , new Stat( 1.0f,  50.0f, health))
            .AddStat("shield"         , new Stat( 0.0f,  10.0f, shield))
            .AddStat("speed"          , new Stat( 0.0f,  10.0f, speed))
            .AddStat("luck"           , new Stat(-2.0f,   2.0f, luck))
            .AddStat("attackDamage"   , new Stat( 0.0f,  50.0f, attackDamage))
            .AddStat("attackSpeed"    , new Stat( 0.0f,  10.0f, attackSpeed))
            .AddStat("critChance"     , new Stat( 0.0f, 100.0f, critChance))
            .AddStat("critDamage"     , new Stat( 0.0f,  50.0f, critDamage))
            .AddStat("bulletCount"    , new Stat( 0.0f,  50.0f, bulletCount))
            .AddStat("bulletPrecision", new Stat( 0.0f,  10.0f, bulletPrecision))
            .AddStat("bulletSpeed"    , new Stat( 0.0f,  50.0f, bulletSpeed))
            .AddStat("bulletLifetime" , new Stat( 0.0f,  10.0f, bulletLifetime))
            .AddStat("bulletCapacity" , new Stat( 0.0f,  50.0f, bulletCapacity))
            .AddStat("reloadSpeed"    , new Stat( 0.0f,  10.0f, reloadSpeed))
            .AddStat("meleeRange"     , new Stat( 0.0f,  10.0f, meleeRange));
    }

    private void p_ReceiveAttack(AttackInfo attackInfo, AttackContext context)
    {
        if (m_invulnerableTime > 0.0f)
        {
            return;
        }
        m_invulnerableTime = attackInfo.invulnerableTime;

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

        m_stunTime = Mathf.Max(m_stunTime, attackInfo.stunTime);

        m_isKnockedback   = true;
        m_knockbackDir    = attackInfo.knockbackDirection.normalized * attackInfo.knockback;
        m_knockbackOrigin = new Vector2(transform.position.x, transform.position.y);

        m_hurtTime = HURT_TIME;
        List<string> removedEffectIds = new List<string>();
        foreach (var p in m_statusEffects)
        {
            if (p.Value.ShouldBeRemovedOnHurt())
            {
                removedEffectIds.Add(p.Key);
            }
        }
        foreach (string effectId in removedEffectIds)
        {
            m_statusEffects[effectId].ForceRemove(this);
            m_statusEffects.Remove(effectId);
        }

        m_shieldTime = 0.0f;

        foreach (string effectId in attackInfo.statusEffectsInflictors.Keys)
        {
            AddStatusEffect(attackInfo.statusEffectsInflictors[effectId](context.attacker, attackInfo.statusEffectsTime[effectId], attackInfo.statusEffectsLevel[effectId]));
        }
        SoundManager.PlaySound(hurtSound);

        if (currentHealth <= 0.01f)
        {
            context.attacker.onKill?.Invoke();
        }
    }

    private void p_OnGameStateChanged(GameState newState)
    {
        switch (newState)
        {
            case GameState.Gameplay:
                isInControl = true;
                break;
            case GameState.Paused:
                isInControl = false;
                break;
            default:
                break;
        }
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
            this.defaultValue = defaultValue.enabled ? defaultValue.value : minValue;
        }

        public float Clamp(float value)
        {
            return Mathf.Clamp(value, minValue, maxValue);
        }
    }

    public struct StatModifyingOperation
    {
        public string    statId;
        public Operation operation;
        public float     amount;

        public static StatModifyingOperation AdditionPercent(string statId, float amount)
        {
            StatModifyingOperation res = new StatModifyingOperation();
            res.statId    = statId;
            res.operation = Operation.AdditionPercent;
            res.amount    = amount;
            return res;
        }

        public static StatModifyingOperation AdditionValue(string statId, float amount)
        {
            StatModifyingOperation res = new StatModifyingOperation();
            res.statId    = statId;
            res.operation = Operation.AdditionValue;
            res.amount    = amount;
            return res;
        }

        public static StatModifyingOperation Multiplication(string statId, float amount)
        {
            StatModifyingOperation res = new StatModifyingOperation();
            res.statId    = statId;
            res.operation = Operation.Multiplication;
            res.amount    = amount;
            return res;
        }

        public enum Operation
        {
            AdditionPercent,
            AdditionValue,
            Multiplication
        }
    }

    public class StatModifier
    {
        public virtual List<StatModifyingOperation> Modify()
        {
            return new List<StatModifyingOperation>();
        }
    }

    public class StatInstance
    {
        public Stat stat { get; private set; }

        public float baseValue { get; private set; }

        public StatInstance(Stat stat)
        {
            this.stat      = stat;
            this.baseValue = stat.defaultValue;
        }

        public StatInstance(Stat stat, float baseValue)
        {
            this.stat      = stat;
            this.baseValue = stat.Clamp(baseValue);
        }
    }

    public class StatSet
    {
        private Dictionary<string, StatInstance> m_statInstances = new Dictionary<string, StatInstance>();
        private Dictionary<string, StatModifier> m_statModifiers = new Dictionary<string, StatModifier>();

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

        public void AddModifier(string modifierId, StatModifier modifier)
        {
            m_statModifiers[modifierId] = modifier;
        }

        public void RemoveModifier(string modifierId)
        {
            m_statModifiers.Remove(modifierId);
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
                value = statInstance.baseValue;

                float a = 1.0f;
                float b = 0.0f;
                float c = 1.0f;

                List<StatModifyingOperation> operations = p_GetOperations();
                foreach (StatModifyingOperation operation in operations)
                {
                    if (operation.statId == statId)
                    {
                        switch (operation.operation)
                        {
                            case StatModifyingOperation.Operation.AdditionPercent:
                                a += operation.amount;
                                break;
                            case StatModifyingOperation.Operation.AdditionValue:
                                b += operation.amount;
                                break;
                            case StatModifyingOperation.Operation.Multiplication:
                                c *= operation.amount;
                                break;
                            default:
                                break;
                        }
                    }
                }

                value = statInstance.stat.Clamp(c * (a * value + b));
                return true;
            }
            value = 0.0f;
            return false;
        }

        public bool TryGetValueAndCoefficients(string statId, out float value, out float a, out float b, out float c)
        {
            if (m_statInstances.TryGetValue(statId, out StatInstance statInstance))
            {
                value = statInstance.baseValue;

                a = 1.0f;
                b = 0.0f;
                c = 1.0f;

                List<StatModifyingOperation> operations = p_GetOperations();
                foreach (StatModifyingOperation operation in operations)
                {
                    if (operation.statId == statId)
                    {
                        switch (operation.operation)
                        {
                            case StatModifyingOperation.Operation.AdditionPercent:
                                a += operation.amount;
                                break;
                            case StatModifyingOperation.Operation.AdditionValue:
                                b += operation.amount;
                                break;
                            case StatModifyingOperation.Operation.Multiplication:
                                c *= operation.amount;
                                break;
                            default:
                                break;
                        }
                    }
                }

                value = statInstance.stat.Clamp(c * (a * value + b));
                return true;
            }
            value = 0.0f;
            a = 1.0f;
            b = 0.0f;
            c = 1.0f;
            return false;
        }

        public float GetBaseValue(string statId)
        {
            float res = float.NaN;
            if (TryGetBaseValue(statId, out float r))
            {
                res = r;
            }
            return res;
        }

        public float GetValue(string statId)
        {
            float res = float.NaN;
            if (TryGetValue(statId, out float r))
            {
                res = r;
            }
            return res;
        }

        public float GetValueAndCoefficients(string statId, out float a, out float b, out float c)
        {
            float res = float.NaN;
            if (TryGetValueAndCoefficients(statId, out float r, out a, out b, out c))
            {
                res = r;
            }
            return res;
        }

        private List<StatModifyingOperation> p_GetOperations()
        {
            List<StatModifyingOperation> res = new List<StatModifyingOperation>();
            foreach (StatModifier modifier in m_statModifiers.Values)
            {
                res.AddRange(modifier.Modify());
            }
            return res;
        }
    }

    public class AttackInfo {
        public float                                     damage                  = 0.0f;
        public float                                     knockback               = 0.0f;
        public float                                     stunTime                = 0.0f;
        public float                                     invulnerableTime        = 0.0f;
        public float                                     critDamage              = 0.0f;
        public bool                                      carryOverDamage         = true;
        public bool                                      isBlacklist             = false;
        public Vector2                                   knockbackDirection      = Vector2.zero;
        public HashSet<string>                           tags                    = new HashSet<string>();
        public Dictionary<string, StatusEffectInflictor> statusEffectsInflictors = new Dictionary<string, StatusEffectInflictor>();
        public Dictionary<string, float>                 statusEffectsTime       = new Dictionary<string, float>();
        public Dictionary<string, int>                   statusEffectsLevel      = new Dictionary<string, int>();

        public static AttackInfo Create()
        {
            return new AttackInfo();
        }

        public AttackInfo Damage(float value)
        {
            damage = value;
            return this;
        }

        public AttackInfo StunTime(float value)
        {
            stunTime = value;
            return this;
        }

        public AttackInfo InvulnerableTime(float value)
        {
            invulnerableTime = value;
            return this;
        }

        public AttackInfo Knockback(float value)
        {
            knockback = value;
            return this;
        }

        public AttackInfo KnockbackDirection(Vector2 value)
        {
            knockbackDirection = value;
            return this;
        }

        public AttackInfo CarryOverDamage(bool value)
        {
            carryOverDamage = value;
            return this;
        }

        public AttackInfo Tags(IEnumerable<string> tags, bool overwrite = false)
        {
            if (overwrite)
            {
                this.tags.Clear();
            }
            this.tags.AddRange(tags);
            return this;
        }
    
        public AttackInfo ApplyEffect(string effectId, float time, int level)
        {
            statusEffectsInflictors.Add(effectId, StatusEffectManager.Get(effectId));
            statusEffectsTime.Add(effectId, time);
            statusEffectsLevel.Add(effectId, level);

            return this;
        }
    }

    public struct AttackModifyingOperation
    {
        public Operation operation;
        public string    effectId;
        public float     amount;
        public int       level;

        public AttackModifyingOperation(Operation operation)
        {
            this.operation = operation;
            this.effectId  = "";
            this.amount    = 0.0f;
            this.level     = 0;
        }

        public AttackModifyingOperation(Operation operation, float amount)
        {
            this.operation = operation;
            this.effectId  = "";
            this.amount    = amount;
            this.level     = 0;
        }

        public AttackModifyingOperation(Operation operation, string effectId, float amount, int level)
        {
            this.operation = operation;
            this.effectId  = effectId;
            this.amount    = amount;
            this.level     = level;
        }

        public AttackModifyingOperation(Operation operation, string effectId)
        {
            this.operation = operation;
            this.effectId  = effectId;
            this.amount    = 0.0f;
            this.level     = 0;
        }

        public enum Operation
        {
            Unknown = -1,

            CritNegation,
            CarryOverDamage,

            DamageAdditionPercent,
            DamageAdditionValue,
            DamageMultiplication,

            KnockbackAdditionPercent,
            KnockbackAdditionValue,
            KnockbackMultiplication,

            StunTimeAdditionPercent,
            StunTimeAdditionValue,
            StunTimeMultiplication,

            InflictStatusEffect
        }
    }

    public struct AttackContext
    {
        public LivingEntity attacker;
        public LivingEntity target;

        public AttackContext(LivingEntity attacker, LivingEntity target)
        {
            this.attacker = attacker;
            this.target   = target;
        }
    }

    public class AttackModifier
    {
        public virtual List<AttackModifyingOperation> Modify(AttackInfo attackInfo, AttackContext attackContext)
        {
            return new List<AttackModifyingOperation>();
        }
    }

    public class StatusEffect
    {
        public string       id        { get; private set; }
        public LivingEntity inflictor { get; private set; }

        private float m_timeLeft;
        private bool  m_initialized = false;
        private bool  m_finished    = false;

        protected StatusEffect(string id, LivingEntity inflictor, float time)
        {
            this.id        = id;
            this.inflictor = inflictor;

            this.m_timeLeft = Mathf.Max(time, 0.0f);
        }

        public bool CanOverride(StatusEffect existingEffect)
        {
            if (existingEffect == null)
            {
                return true;
            }
            return m_timeLeft > existingEffect.m_timeLeft;
        }

        public bool ShouldBeRemoved()
        {
            return m_finished;
        }

        public void Update(LivingEntity target)
        {
            if (m_timeLeft > 0.0f)
            {
                if (!m_initialized)
                {
                    m_initialized = true;
                    if (target != null)
                    {
                        OnApply(target);
                        target.statSet.AddModifier(id, GetStatModifier());
                    }
                }

                if (target != null)
                {
                    OnUpdate(target);
                }
                m_timeLeft = Mathf.Max(0.0f, m_timeLeft - Time.deltaTime);
            }
            else
            {
                if (!m_finished)
                {
                    m_finished = true;
                    if (target != null)
                    {
                        target.statSet.RemoveModifier(id);
                        OnRemove(target);
                    }
                }
            }
        }

        public void ForceRemove(LivingEntity target)
        {
            m_timeLeft = 0.0f;
            m_finished = true;
            if (target != null)
            {
                target.statSet.RemoveModifier(id);
                OnRemove(target);
            }
        }

        public virtual bool ShouldBeRemovedOnHurt()
        {
            return false;
        }

        public virtual bool ShouldBeRemovedOnAttack()
        {
            return false;
        }

        public virtual void OnApply(LivingEntity target)
        {
        }

        public virtual void OnRemove(LivingEntity target)
        {
        }

        public virtual void OnUpdate(LivingEntity target)
        {
        }

        public virtual StatModifier GetStatModifier()
        {
            return new StatModifier();
        }

        public virtual AttackModifier GetAttackDealtModifier()
        {
            return new AttackModifier();
        }

        public virtual AttackModifier GetAttackReceivedModifier()
        {
            return new AttackModifier();
        }
    }

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

    public delegate void         DeathCallback();
    public delegate void         KillCallback();
    public delegate StatusEffect StatusEffectInflictor(LivingEntity owner, float time, int level);
}
