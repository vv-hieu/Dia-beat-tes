using UnityEngine;

[ExecuteInEditMode]
public class HealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LivingEntity livingEntity;
    [SerializeField] private Material     material;

    private Renderer m_renderer;
    private Material m_material;
    private int      m_healthId        = 0;
    private int      m_shieldId        = 0;
    private int      m_currentHealthId = 0;
    private int      m_currentShieldId = 0;

    private void Awake()
    {
        m_renderer          = GetComponent<Renderer>();
        m_renderer.material = Instantiate(material);
    }

    private void Start()
    {
        m_material = m_renderer.material;

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
        if (livingEntity != null)
        {
            return livingEntity.currentHealth;
        }
        return 0.0f;
    }

    private float p_CurrentShield()
    {
        if (livingEntity != null)
        {
            return livingEntity.currentShield;
        }
        return 0.0f;
    }

    private float p_Health()
    {
        if (livingEntity != null && livingEntity.statSet.TryGetValue("health", out float health))
        {
            return health;
        }
        return 0.0f;
    }

    private float p_Shield()
    {
        if (livingEntity != null && livingEntity.statSet.TryGetValue("shield", out float shield))
        {
            return shield;
        }
        return 0.0f;
    }
}
