using System;
using UnityEngine;
using UnityEngine.Events;

public class Relic : MonoBehaviour
{
    [Header("Property")]
    [SerializeField] private Property relicProperty;

    [Header("References")]
    [SerializeField] private Transform      sprite;
    [SerializeField] private Transform      shadow;
    [SerializeField] private SpriteRenderer aura;

    [HideInInspector] public string id;

    private RelicSpecialEffect m_relicSpecialEffect;
    private Vector3            m_originalSpritePos;
    private Vector3            m_originalShadowScale;
    private Vector3            m_originalScale;
    private float              m_time      = 0.0f;
    private float              m_hoverTime = 0.0f;
    private bool               m_hover     = false;

    private void Start()
    {
        m_relicSpecialEffect = GetComponent<RelicSpecialEffect>();
        if (m_relicSpecialEffect != null)
        {
            relicProperty.critDealtModifier      = m_relicSpecialEffect.ModifyCritDealt;
            relicProperty.critReceivedModifier   = m_relicSpecialEffect.ModifyCritReceived;
            relicProperty.attackDealtModifier    = m_relicSpecialEffect.ModifyAttackDealt;
            relicProperty.attackReceivedModifier = m_relicSpecialEffect.ModifyAttackReceived;
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
        transform.localScale = m_originalScale * Mathf.Clamp01(m_time * 5.0f) * (1.0f + m_hoverTime * 0.2f);
    }

    private void OnMouseEnter()
    {
        m_hover = true;
    }

    private void OnMouseExit()
    {
        m_hover = false;
    }

    private void OnMouseUpAsButton()
    {
        GameObject.FindGameObjectWithTag("Player").GetComponent<Player>().AddRelic(id, relicProperty);
        Destroy(gameObject);
    }

    private Color p_AuraColor()
    {
        if (relicProperty.type == Type.Cursed)
        {
            return new Color(0.2f, 0.0f, 0.0f, 0.8f);
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
        public bool   upgradable;
        public SerializableDictionary<string, LivingEntity.StatModifier> statModifiers;

        public LivingEntity.CritModifier       critDealtModifier;
        public LivingEntity.CritModifier       critReceivedModifier;
        public LivingEntity.AttackInfoModifier attackDealtModifier;
        public LivingEntity.AttackInfoModifier attackReceivedModifier;
    }

    public enum Type
    {
        Unknown = -1,

        Common,
        Rare,
        Cursed
    }
}

