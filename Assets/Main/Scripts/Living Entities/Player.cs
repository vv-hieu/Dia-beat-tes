using UnityEngine;

[ExecuteInEditMode]
public class Player : MonoBehaviour
{
    [SerializeField] private GameObject bullet;
    [SerializeField] private float      bulletSpeed    = 1.0f;
    [SerializeField] private float      bulletLifetime = 1.0f;

    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private Sprite playerNormal;
    [SerializeField] private Sprite playerPartialFat;
    [SerializeField] private Sprite playerFullyFat;

    [SerializeField] private GameObject sweatVFX;
    [SerializeField] private Transform  sweatVFXSpawnPosition;

    private LivingEntity m_livingEntity;
    private Rigidbody2D  m_rigidbody;
    private float        m_fatMeter        = 0.0f;
    private float        m_sweatSpawnTimer = 0.0f;

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

        m_livingEntity.attackDealtModifier    = p_ModifyAttackDealt;
        m_livingEntity.attackReceivedModifier = p_ModifyAttackReceived;
    }

    private void Update()
    {
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
            m_sweatSpawnTimer += Time.deltaTime;
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

    private float p_Speed()
    {
        if (m_livingEntity != null && m_livingEntity.statSet.TryGetValue("speed", out float speed))
        {
            // Speed is affected by weight
            return speed * (m_fatMeter < 0.5f ? 1.0f : m_fatMeter < 1.0f ? 0.75f : 0.5f);
        }
        return 0.0f;
    }

    private void p_ModifyAttackDealt(LivingEntity.AttackInfo attackInfo, LivingEntity receiver)
    {

    }

    private void p_ModifyAttackReceived(LivingEntity.AttackInfo attackInfo, LivingEntity attacker)
    {

    }

    private void p_OnBulletHit(LivingEntity entity, Projectile bullet)
    {
        LivingEntity.HandleAttack(bullet.owner, entity, LivingEntity.AttackInfo.NewAttackInfo()
            .SetKnockbackDirection(bullet.velocity)
            .SetKnockback(2.0f)
            .SetDamage(1.0f)
            .SetInvulnerableTime(0.4f)
            .SetStunTime(0.35f));
        Destroy(bullet.gameObject);
    }

    public class RelicContainer { 
        public struct Entry
        {
            public Relic.Property relicProperty;
            public int            level;
        }
    }
}
