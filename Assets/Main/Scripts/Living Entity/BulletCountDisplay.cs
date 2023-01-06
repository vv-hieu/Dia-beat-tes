using UnityEngine;
using UnityEngine.UI;

public class BulletCountDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LivingEntity livingEntity;
    [SerializeField] private Material     material;

    private Renderer m_renderer;
    private Image    m_image;
    private Material m_material;
    private int      m_bulletCountId    = 0;
    private int      m_bulletCapacityId = 0;

    public void SetEntity(LivingEntity entity)
    {
        livingEntity = entity;
    }

    private void Awake()
    {
        m_renderer = GetComponent<Renderer>();
        m_image    = GetComponent<Image>();
        if (m_renderer != null)
        {
            m_renderer.material = Instantiate(material);
            m_material = m_renderer.material;
        }
        else if (m_image != null)
        {
            m_image.material = Instantiate(material);
            m_material = m_image.material;
        }
    }

    private void Start()
    {
        m_bulletCountId    = Shader.PropertyToID("_BulletCount");
        m_bulletCapacityId = Shader.PropertyToID("_BulletCapacity");
    }

    private void Update()
    {
        if (m_material != null)
        {
            m_material.SetFloat(m_bulletCountId   , p_BulletCount());
            m_material.SetFloat(m_bulletCapacityId, p_BulletCapacity());
        }
    }

    private float p_BulletCount()
    {
        if (livingEntity != null)
        {
            if (livingEntity.GetWeapon() != null)
            {
                return livingEntity.GetWeapon().BulletCount();
            }
        }
        return 0.0f;
    }

    private float p_BulletCapacity()
    {
        if (livingEntity != null)
        {
            if (livingEntity.GetWeapon() != null)
            {
                return livingEntity.GetWeapon().BulletCapacity();
            }
        }
        return 0.0f;
    }
}
