using UnityEngine;

[ExecuteInEditMode]
public class HealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LivingEntity m_livingEntity;

    private Material m_material;
    private int      m_healthId        = 0;
    private int      m_shieldId        = 0;
    private int      m_currentHealthId = 0;
    private int      m_currentShieldId = 0;

    private void Start()
    {
        m_material = GetComponent<Renderer>().material;
        m_material.renderQueue = 5000;

        m_healthId        = Shader.PropertyToID("_Health");
        m_shieldId        = Shader.PropertyToID("_Shield");
        m_currentHealthId = Shader.PropertyToID("_CurrentHealth");
        m_currentShieldId = Shader.PropertyToID("_CurrentShield");
    }

    private void Update()
    {
        m_material.SetFloat(m_healthId       , p_Health());
        m_material.SetFloat(m_shieldId       , p_Shield());
        m_material.SetFloat(m_currentHealthId, p_CurrentHealth());
        m_material.SetFloat(m_currentShieldId, p_CurrentShield());
    }

    private float p_CurrentHealth()
    {
        if (m_livingEntity != null)
        {
            return m_livingEntity.currentHealth;
        }
        return 0.0f;
    }

    private float p_CurrentShield()
    {
        if (m_livingEntity != null)
        {
            return m_livingEntity.currentShield;
        }
        return 0.0f;
    }

    private float p_Health()
    {
        if (m_livingEntity != null && m_livingEntity.statSet.TryGetValue("health", out float health))
        {
            return health;
        }
        return 0.0f;
    }

    private float p_Shield()
    {
        if (m_livingEntity != null && m_livingEntity.statSet.TryGetValue("shield", out float shield))
        {
            return shield;
        }
        return 0.0f;
    }
}
