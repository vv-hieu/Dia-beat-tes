using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Melee : MonoBehaviour
{
    [Header("Appearance")]
    [SerializeField] private Color color;

    public LivingEntity owner     { get; private set; }
    public Vector2      direction { get; private set; }

    private SpriteRenderer          m_sprite;
    private SpriteFlipbookAnimation m_animation;
    private string[]                m_affectTags;
    private OnHit                   m_onHit;
    private bool                    m_init        = false;
    private bool                    m_firstFrame  = true;
    private float                   m_time        = 0.0f;
    private float                   m_lifetime    = 0.0f;

    public void Init(LivingEntity owner, Vector2 direction, float lifetime, float size, bool flipped, string[] affectedTags, OnHit onHit)
    {
        if (!m_init)
        {
            m_init = true;

            lifetime = Mathf.Max(0.1f, lifetime);

            this.owner     = owner;
            this.direction = direction;

            transform.localScale = Vector3.one * size; 

            p_RotateToDir();
            m_animation.SetDuration(lifetime);
            m_animation.Play();

            m_sprite.flipX = flipped;
            m_lifetime     = lifetime;
            m_affectTags   = affectedTags;
            m_onHit        = onHit;
            m_firstFrame   = true;
        }
    }

    private void Awake()
    {
        m_sprite    = GetComponent<SpriteRenderer>();
        m_animation = GetComponent<SpriteFlipbookAnimation>();
    }

    private void Start()
    {
        m_sprite.color  = color;
        m_sprite.sprite = null;
    }

    private void OnValidate()
    {
        m_sprite.color = color;
    }

    private void Update()
    {
        if (m_time >= m_lifetime && !m_firstFrame)
        {
            Destroy(gameObject);
        }
        if (m_init)
        {
            Collider2D[] colliders = new Collider2D[100];
            int count = Physics2D.OverlapCollider(GetComponent<Collider2D>(), new ContactFilter2D().NoFilter(), colliders);

            for (int i = 0; i < count; ++i)
            {
                LivingEntity entity = LivingEntity.FromCollider(colliders[i]);
                if (entity != null && m_onHit != null)
                {
                    if (entity.HasTagsAny(m_affectTags))
                    {
                        m_onHit(entity, this);
                    }
                }
            }

            m_firstFrame = false;
            m_time += Time.deltaTime;
        }
    }

    private void p_RotateToDir()
    {
        if (direction != Vector2.zero)
        {
            transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);
        }
    }

    public delegate void OnHit(LivingEntity entity, Melee melee);
}
