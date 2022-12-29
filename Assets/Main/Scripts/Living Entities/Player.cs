using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Player : MonoBehaviour
{
    [SerializeField] private GameObject bullet;
    [SerializeField] private float      bulletSpeed    = 1.0f;
    [SerializeField] private float      bulletLifetime = 1.0f;

    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private Sprite         playerNormal;
    [SerializeField] private Sprite         playerPartialFat;
    [SerializeField] private Sprite         playerFullyFat;

    [SerializeField] private GameObject sweatVFX;
    [SerializeField] private Transform  sweatVFXSpawnPosition;

    // Temp, remove later
    [SerializeField] private Info info;
    [System.Serializable]
    public struct Info
    {
         public float health;
         public float shield;
         public float speed;
         public float luck;
         public float attackDamage;
         public float attackSpeed;
         public float critChance;
         public float critDamage;
         public float bulletCount;
         public float bulletPrecision;
         public float bulletSpeed;
         public float bulletLifeTime;
         public float bulletCapacity;
         public float reloadSpeed;
    }

    private LivingEntity                                    m_livingEntity;
    private Rigidbody2D                                     m_rigidbody;
    private float                                           m_fatMeter                     = 0.0f;
    private float                                           m_sweatSpawnTimer              = 0.0f;
    private Dictionary<string, RelicEntry>                  m_relics                       = new Dictionary<string, RelicEntry>();
    private Dictionary<string, LivingEntity.StatModifier>   m_relicStatModifiers           = new Dictionary<string, LivingEntity.StatModifier>();
    private Dictionary<string, LivingEntity.AttackModifier> m_relicAttackDealtModifiers    = new Dictionary<string, LivingEntity.AttackModifier>();
    private Dictionary<string, LivingEntity.AttackModifier> m_relicAttackReceivedModifiers = new Dictionary<string, LivingEntity.AttackModifier>();

    public int commonRelicCount { get; private set; } = 0;
    public int rareRelicCount   { get; private set; } = 0;
    public int cursedRelicCount { get; private set; } = 0;
    public int totalRelicCount  { get; private set; } = 0;

    public void SetFatness(float amount)
    {
        m_fatMeter = Mathf.Clamp01(amount);
        if (m_fatMeter < 0.5f)
        {
            sprite.sprite = playerNormal;
        }
        else if (m_fatMeter < 1.0f)
        {
            sprite.sprite = playerPartialFat;
        }
        else
        {
            sprite.sprite = playerFullyFat;
        }
    }

    public void AddFatness(float amount)
    {
        SetFatness(m_fatMeter + amount);
    }

    public void AddRelic(string id, Relic.Property relicProperty)
    {
        switch (relicProperty.type)
        {
            case Relic.Type.Common:
                {
                    ++commonRelicCount;
                    ++totalRelicCount;
                    break;
                }
            case Relic.Type.Rare:
                {
                    ++rareRelicCount;
                    ++totalRelicCount;
                    break;
                }
            case Relic.Type.Cursed:
                {
                    ++cursedRelicCount;
                    ++totalRelicCount;
                    break;
                }
        }

        int count = 0;
        if (m_relics.TryGetValue(id, out RelicEntry entry))
        {
            count = entry.count;
        }
        m_relics[id] = new RelicEntry(relicProperty, count + 1);

        string modifierId = id + "_" + count;

        m_relicStatModifiers[modifierId]           = relicProperty.statModifierGenerator(GameManager.instance.GetGameContext());
        m_relicAttackDealtModifiers[modifierId]    = relicProperty.attackDealtModifierGenerator(GameManager.instance.GetGameContext());
        m_relicAttackReceivedModifiers[modifierId] = relicProperty.attackReceivedModifierGenerator(GameManager.instance.GetGameContext());

        m_livingEntity.statSet.AddModifier(modifierId, m_relicStatModifiers[modifierId]);
    }

    public bool CanPickUp(Collectible.CollectibleType collectibleType)
    {
        if (collectibleType == Collectible.CollectibleType.CakePiece)
        {
            return m_fatMeter < 1.0f;
        }
        return true;
    }

    private void Start()
    {
        m_livingEntity = GetComponent<LivingEntity>();
        m_rigidbody    = GetComponent<Rigidbody2D>();

        SetFatness(0.0f);

        m_livingEntity.attackDealtModifier    = new PlayerAttackModifier(this, true);
        m_livingEntity.attackReceivedModifier = new PlayerAttackModifier(this, false);
    }

    private void Update()
    {
        p_UpdateInfo();

        float dt = Time.deltaTime;

        if (Input.GetMouseButtonDown(0) && bullet != null)
        {
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0.0f));

            Vector2 p0 = new Vector2(transform.position.x, transform.position.y);
            Vector2 p1 = new Vector2(worldPos.x, worldPos.y);

            GameObject go = Instantiate(bullet, transform.position, Quaternion.identity, transform.parent);
            Projectile projectile = go.GetComponent<Projectile>();
            projectile.Init(m_livingEntity, bulletLifetime, bulletSpeed, p1 - p0, new string[] { "Hostile" }, p_OnBulletHit);
        }

        if (m_fatMeter >= 1.0f && sweatVFX != null)
        {
            if (m_sweatSpawnTimer >= 1.0f)
            {
                m_sweatSpawnTimer = 0.0f;
            }
            if (m_sweatSpawnTimer <= 0.0f)
            {
                Vector3 pos = transform.position;
                if (sweatVFXSpawnPosition != null)
                {
                    pos = sweatVFXSpawnPosition.position;
                }
                Instantiate(sweatVFX, pos, Quaternion.identity, transform);
            }
            m_sweatSpawnTimer += dt;
        }
        else
        {
            m_sweatSpawnTimer = 0.0f;
        }
    }

    private void FixedUpdate()
    {
        Vector2 input    = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
        Vector2 position = new Vector2(transform.position.x, transform.position.y);
        m_rigidbody.MovePosition(position + input * p_Speed() * Time.fixedDeltaTime);
    }

    private void p_UpdateInfo()
    {
        info.health          = m_livingEntity.statSet.GetValue("health");
        info.shield          = m_livingEntity.statSet.GetValue("shield");
        info.speed           = m_livingEntity.statSet.GetValue("speed");
        info.luck            = m_livingEntity.statSet.GetValue("luck");
        info.attackDamage    = m_livingEntity.statSet.GetValue("attackDamage");
        info.attackSpeed     = m_livingEntity.statSet.GetValue("attackSpeed");
        info.critChance      = m_livingEntity.statSet.GetValue("critChance");
        info.critDamage      = m_livingEntity.statSet.GetValue("critDamage");
        info.bulletCount     = m_livingEntity.statSet.GetValue("bulletCount");
        info.bulletPrecision = m_livingEntity.statSet.GetValue("bulletPrecision");
        info.bulletSpeed     = m_livingEntity.statSet.GetValue("bulletSpeed");
        info.bulletLifeTime  = m_livingEntity.statSet.GetValue("bulletLifeTime");
        info.bulletCapacity  = m_livingEntity.statSet.GetValue("bulletCapacity");
        info.reloadSpeed     = m_livingEntity.statSet.GetValue("reloadSpeed");
}

    private float p_Speed()
    {
        if (m_livingEntity != null && m_livingEntity.statSet.TryGetValue("speed", out float speed))
        {
            // Speed is affected by weight
            return speed * (m_fatMeter < 0.5f ? 1.0f : m_fatMeter < 1.0f ? 0.75f : 0.5f);
        }
        return 0.0f;
    }

    private void p_OnBulletHit(LivingEntity entity, Projectile bullet)
    {
        LivingEntity.HandleAttack(bullet.owner, entity, LivingEntity.AttackInfo.NewAttackInfo()
            .SetKnockbackDirection(bullet.velocity)
            .SetKnockback(1.0f)
            .SetDamage(m_livingEntity.statSet.GetValue("attackDamage"))
            .SetInvulnerableTime(0.3f)
            .SetStunTime(0.2f));
        Destroy(bullet.gameObject);
    }

    private struct RelicEntry
    {
        public Relic.Property relicProperty;
        public int            count;

        public RelicEntry(Relic.Property relicProperty, int count)
        {
            this.relicProperty = relicProperty;
            this.count         = count;
        }
    }

    private class PlayerAttackModifier : LivingEntity.AttackModifier
    {
        private Player player;
        private bool   isAttacker;

        public PlayerAttackModifier(Player player, bool isAttacker)
        {
            this.player    = player;
            this.isAttacker = isAttacker;
        }

        public override List<LivingEntity.AttackModifyingOperation> Modify(LivingEntity.AttackInfo attackInfo, LivingEntity.AttackContext attackContext)
        {
            List<LivingEntity.AttackModifyingOperation> res = new List<LivingEntity.AttackModifyingOperation>();
            if (isAttacker)
            {
                foreach (var p in player.m_relicAttackDealtModifiers.Values)
                {
                    res.AddRange(p.Modify(attackInfo, attackContext));
                }
            }
            else
            {
                foreach (var p in player.m_relicAttackReceivedModifiers.Values)
                {
                    res.AddRange(p.Modify(attackInfo, attackContext));
                }
            }
            return res;
        }
    }
}
