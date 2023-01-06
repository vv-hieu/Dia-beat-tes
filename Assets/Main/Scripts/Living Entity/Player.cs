using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Weapon")]
    [SerializeField] private GameObject weapon;

    [Header("Appearance")]
    [SerializeField] private Sprite playerNormal;
    [SerializeField] private Sprite playerPartialFat;
    [SerializeField] private Sprite playerFullyFat;
    [SerializeField] private float  rollSpeed;

    [Header("VFX")]
    [SerializeField] private GameObject sweatVFX;

    [Header("Interacting")]
    [SerializeField] private float interactRadius;

    [Header("References")]
    [SerializeField] private SpriteRenderer   sprite;
    [SerializeField] private GameObject       defaultState;
    [SerializeField] private GameObject       rollingState;
    [SerializeField] private RadialDamageZone rollingDamageZone;
    [SerializeField] private SpriteRenderer   rollingSprite;

    private LivingEntity                                    m_livingEntity;
    private Rigidbody2D                                     m_rigidbody;
    private float                                           m_fatMeter                     = 0.0f;
    private float                                           m_fatSpeed                     = 1.0f;
    private GameObject                                      m_sweatVFX;
    private Dictionary<string, RelicEntry>                  m_relics                       = new Dictionary<string, RelicEntry>();
    private Dictionary<string, LivingEntity.StatModifier>   m_relicStatModifiers           = new Dictionary<string, LivingEntity.StatModifier>();
    private Dictionary<string, LivingEntity.AttackModifier> m_relicAttackDealtModifiers    = new Dictionary<string, LivingEntity.AttackModifier>();
    private Dictionary<string, LivingEntity.AttackModifier> m_relicAttackReceivedModifiers = new Dictionary<string, LivingEntity.AttackModifier>();
    private bool                                            m_rolling                      = false;
    private float                                           m_rollTime                     = 0.0f;
    private float                                           m_rollAngle                    = 0.0f;
    private Vector2                                         m_rollDirection                = Vector2.right;
    private HashSet<Interactable>                           m_interactables                = new HashSet<Interactable>();
    private bool                                            m_inputEnabled                 = true;

    public int commonRelicCount { get; private set; } = 0;
    public int rareRelicCount   { get; private set; } = 0;
    public int cursedRelicCount { get; private set; } = 0;
    public int totalRelicCount  { get; private set; } = 0;

    public void EnableInput()
    {
        m_inputEnabled = true;
    }

    public void DisableInput()
    {
        m_inputEnabled = false;
    }

    public void SetFatness(float amount)
    {
        m_fatMeter = Mathf.Clamp01(amount);
        if (m_fatMeter < 0.5f)
        {
            sprite.sprite = playerNormal;
            if (m_sweatVFX != null)
            {
                Destroy(m_sweatVFX);
                m_sweatVFX = null;
            }
        }
        else if (m_fatMeter < 1.0f)
        {
            sprite.sprite = playerPartialFat;
            if (m_sweatVFX != null)
            {
                Destroy(m_sweatVFX);
                m_sweatVFX = null;
            }
        }
        else
        {
            sprite.sprite = playerFullyFat;
            m_sweatVFX = Instantiate(sweatVFX, m_livingEntity.VFXPivot());
        }
        m_fatSpeed = 1.0f - Mathf.Floor(m_fatMeter * 2.0f) * 0.25f;
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

        m_relicStatModifiers[modifierId]           = relicProperty.statModifierGenerator(GameManager.GetGameContext());
        m_relicAttackDealtModifiers[modifierId]    = relicProperty.attackDealtModifierGenerator(GameManager.GetGameContext());
        m_relicAttackReceivedModifiers[modifierId] = relicProperty.attackReceivedModifierGenerator(GameManager.GetGameContext());

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

    private void Awake()
    {
        m_livingEntity = GetComponent<LivingEntity>();
        m_rigidbody    = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        SetFatness(0.0f);

        m_livingEntity.attackDealtModifier    = new PlayerAttackModifier(this, true);
        m_livingEntity.attackReceivedModifier = new PlayerAttackModifier(this, false);

        m_livingEntity.SetWeapon(weapon);

        if (rollingDamageZone != null)
        {
            rollingDamageZone.Init(m_livingEntity, 1.0f, 0.3f, 0.4f, 0.5f, new string[] { "Roll" }, new string[] { "Hostile" }, true, true);
        }
    }

    private void Update()
    {
        if (m_livingEntity.isInControl && m_inputEnabled)
        {
            if (m_rollTime <= 0.0f)
            {
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0.0f));

                m_livingEntity.AimWeaponAt(new Vector2(worldPos.x, worldPos.y));
                if (Input.GetMouseButton(0))
                {
                    m_livingEntity.UseWeapon(new string[] { "Hostile" });
                }
            }

            if (Input.GetKeyDown("space") && m_rollTime <= 0.0f)
            {
                SetFatness(0.0f);
                m_rolling = true;
                m_rollTime = 2.0f;
                m_rollAngle = 0.0f;
                defaultState.SetActive(false);
                rollingState.SetActive(true);
            }
        }

        if (m_rollTime <= 0.0f && m_rolling)
        {
            m_rolling = false;
            defaultState.SetActive(true);
            rollingState.SetActive(false);
        }
        else if (m_rollTime > 0.0f)
        {
            rollingSprite.transform.localEulerAngles = new Vector3(0.0f, 0.0f, (m_livingEntity.isFacingRight ? m_rollAngle : -m_rollAngle));
            m_rollAngle += rollSpeed * Time.deltaTime;
        }
        m_rollTime = Mathf.Max(0.0f, m_rollTime - Time.deltaTime);

        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, interactRadius);
        HashSet<Interactable> set = new HashSet<Interactable>();
        foreach (Collider2D collider in colliders)
        {
            if (collider.TryGetComponent(out Interactable interactable))
            {
                if (m_interactables.Add(interactable))
                {
                    interactable.OnApproach(this);
                }
                set.Add(interactable);
            }
        }
        foreach (Interactable interactable in m_interactables)
        {
            if (!set.Contains(interactable))
            {
                interactable.OnLeave(this);
            }
        }
        m_interactables = set;
        if (m_livingEntity.isInControl && m_inputEnabled && m_rollTime <= 0.0f && Input.GetKeyDown("f"))
        {
            foreach (Interactable interactable in m_interactables)
            {
                interactable.OnInteract(this);
            }
        }
    }

    private void FixedUpdate()
    {
        if (m_livingEntity.isInControl && m_inputEnabled)
        {
            Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
            if (input != Vector2.zero)
            {
                m_rollDirection = input;
            }
            Vector2 position = new Vector2(transform.position.x, transform.position.y);
            m_rigidbody.MovePosition(position + (m_rollTime > 0.0f ? m_rollDirection : input) * m_livingEntity.statSet.GetValue("speed") * (m_rollTime > 0.0f ? 1.0f : m_fatSpeed) * Time.fixedDeltaTime);
        }
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
