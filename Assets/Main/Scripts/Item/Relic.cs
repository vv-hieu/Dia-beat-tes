using System;
using UnityEngine;

public class Relic : MonoBehaviour
{
    [Header("Property")]
    [SerializeField] private Property relicProperty;

    [Header("References")]
    [SerializeField] private Transform      sprite;
    [SerializeField] private Transform      shadow;
    [SerializeField] private SpriteRenderer aura;

    [HideInInspector] public string id;

    private RelicEffect m_relicEffect;
    private Vector3     m_originalSpritePos;
    private Vector3     m_originalShadowScale;
    private Vector3     m_originalScale;
    private float       m_time      = 0.0f;
    private float       m_hoverTime = 0.0f;
    private bool        m_hover     = false;

    public Property property
    {
        get
        {
            return relicProperty;
        }
    }

    public void AddToPlayer(Player player)
    {
        player.AddRelic(id, gameObject);
    }

    private void Awake()
    {
        m_relicEffect = GetComponent<RelicEffect>();
    }

    private void Start()
    {
        if (m_relicEffect != null)
        {
            relicProperty.statModifierGenerator           = m_relicEffect.GenerateStatModifier;
            relicProperty.attackDealtModifierGenerator    = m_relicEffect.GenerateAttackDealtModifier;
            relicProperty.attackReceivedModifierGenerator = m_relicEffect.GenerateAttackReceivedModifier;
        }

        m_originalSpritePos   = sprite.localPosition;
        m_originalShadowScale = shadow.localScale;
        m_originalScale       = transform.localScale;

        aura.color = p_AuraColor();
    }

    private void OnValidate()
    {
        aura.color = p_AuraColor();
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        m_time += dt;
        m_hoverTime = Mathf.Clamp01(m_hoverTime + (m_hover ? dt : -dt) * 10.0f);

        float d = Mathf.Cos(2.0f * m_time);
        sprite.localPosition = m_originalSpritePos + Vector3.up * d * 0.02f;
        shadow.localScale    = m_originalShadowScale * (1.0f + d * 0.05f);
    }

    private void OnMouseEnter()
    {
        m_hover = true;
    }

    private void OnMouseExit()
    {
        m_hover = false;
    }

    private Color p_AuraColor()
    {
        if (relicProperty.type == Type.Cursed)
        {
            return new Color(0.2f, 0.0f, 0.0f, 0.3f);
        }
        if (relicProperty.type == Type.Common)
        {
            return new Color(0.2f, 0.8f, 1.0f, 0.3f);
        }
        if (relicProperty.type == Type.Rare)
        {
            return new Color(0.2f, 1.0f, 0.4f, 0.3f);
        }
        return new Color(0.0f, 0.0f, 0.0f, 0.0f);
    }

    [Serializable]
    public struct Property
    {
        public string name;
        public string description;
        public Type   type;
        public int    price;
  
        public StatModifierGenerator   statModifierGenerator;
        public AttackModifierGenerator attackDealtModifierGenerator;
        public AttackModifierGenerator attackReceivedModifierGenerator;
    }

    public enum Type
    {
        Unknown = -1,
        Common,
        Rare,
        Cursed
    }

    public delegate LivingEntity.StatModifier   StatModifierGenerator(GameManager.GameContext context);
    public delegate LivingEntity.AttackModifier AttackModifierGenerator(GameManager.GameContext context);

    public class LivingEntityAwaredStatModifier : LivingEntity.StatModifier {
        protected LivingEntity livingEntity;

        public LivingEntityAwaredStatModifier(LivingEntity livingEntity)
        {
            this.livingEntity = livingEntity;
        }
    }

    public class PlayerAwaredStatModifier : LivingEntityAwaredStatModifier
    {
        protected Player player;

        public PlayerAwaredStatModifier(Player player) : base(player.GetComponent<LivingEntity>())
        {
            this.player = player;
        }
    }
}

