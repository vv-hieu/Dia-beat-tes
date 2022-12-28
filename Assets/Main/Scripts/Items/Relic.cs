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

    private Vector3 m_originalSpritePos;
    private Vector3 m_originalShadowScale;
    private Vector3 m_originalScale;
    private float   m_time = 0.0f;

    private void Start()
    {
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
        m_time += Time.deltaTime;
        float d = Mathf.Cos(2.0f * m_time);
        sprite.localPosition = m_originalSpritePos + Vector3.up * d * 0.02f;
        shadow.localScale    = m_originalShadowScale * (1.0f + d * 0.05f);
        transform.localScale = m_originalScale * Mathf.Clamp01(m_time * 5.0f);
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
    }

    public enum Type
    {
        Common,
        Rare,
        Cursed
    }
}

