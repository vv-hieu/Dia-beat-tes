using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Projectile : MonoBehaviour
{
    [Header("Appearance")]
    [SerializeField] private Color    color         = Color.red;
    [SerializeField] private float    size          = 0.1f;
    [SerializeField] private Gradient trailColor    = new Gradient();
    [SerializeField] private float    trailSize     = 0.1f;
    [SerializeField] private float    trailLifeTime = 0.5f;

    public float   lifeTime { get; private set; }
    public Vector2 velocity { get; private set; }

    public LivingEntity owner { get; private set; }

    private SpriteRenderer        m_spriteRenderer;
    private TrailRenderer         m_trailRenderer;
    private Rigidbody2D           m_rigidbody;
    private Collider2D            m_collider;
    private string[]              m_affectTags;
    private OnHit                 m_onHit;
    private bool                  m_init                            = false;
    private bool                  m_interactWithDestructibleTilemap = false;
    private HashSet<LivingEntity> m_hitEntities                     = new HashSet<LivingEntity>();

    public void Init(LivingEntity owner, float lifeTime, float speed, Vector2 direction, string[] affectedTags, OnHit onHit)
    {
        if (!m_init)
        {
            m_init        = true;

            this.owner    = owner;
            this.lifeTime = lifeTime;
            this.velocity = direction.normalized * speed;

            m_affectTags  = affectedTags;
            m_onHit       = onHit;
        }
    }

    private void Start()
    {
        m_spriteRenderer = GetComponent<SpriteRenderer>();
        m_trailRenderer  = GetComponent<TrailRenderer>();
        m_rigidbody      = GetComponent<Rigidbody2D>();
        m_collider       = GetComponent<Collider2D>();

        m_spriteRenderer.color            = color;
        m_trailRenderer.widthMultiplier   = trailSize;
        m_trailRenderer.colorGradient     = trailColor;
        m_trailRenderer.time              = trailLifeTime;
        m_interactWithDestructibleTilemap = TryGetComponent(out DestructibleTilemapInteraction destructibleTilemapInteraction);
        transform.localScale              = Vector3.one * size;
    }

    private void OnValidate()
    {
        if (m_spriteRenderer != null)
        {
            m_spriteRenderer.color = color;
        }
        if (m_trailRenderer != null)
        {
            m_trailRenderer.widthMultiplier = trailSize;
            m_trailRenderer.colorGradient   = trailColor;
            m_trailRenderer.time            = trailLifeTime;
        }
        transform.localScale = Vector3.one * size;
    }

    private void Update()
    {
        if (m_init)
        {
            lifeTime -= Time.deltaTime;
            if (lifeTime <= 0.0f)
            {
                lifeTime = 0.0f;
                Destroy(gameObject);
            }
        }
    }

    private void FixedUpdate()
    {
        Vector2 oldPos = new Vector2(transform.position.x, transform.position.y);
        Vector2 newPos = oldPos + velocity * Time.fixedDeltaTime;
        p_Move(oldPos, newPos);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out LivingEntity entity) && m_onHit != null)
        {
            if (m_hitEntities.Add(entity) && entity.HasTagsAny(m_affectTags))
            {
                m_onHit(entity, this);
            }
        }
    }

    private void p_Move(Vector2 from, Vector2 to)
    {
        Vector2 dest   = to;
        Vector2 movDir = (to - from).normalized;
        float   movDis = Vector2.Distance(from, to);

        ContactFilter2D filter = new ContactFilter2D();
        filter.layerMask   = Physics2D.GetLayerCollisionMask(gameObject.layer);
        filter.useTriggers = true;

        List<RaycastHit2D> results = new List<RaycastHit2D>();
        Physics2D.Raycast(from, movDir, filter, results, movDis);

        foreach (RaycastHit2D raycastHit in results)
        {
            if (raycastHit.collider == m_collider)
            {
                continue;
            }
            if (raycastHit.collider.TryGetComponent(out LivingEntity entity))
            {
                if (entity.HasTagsAny(m_affectTags))
                {
                    if (movDis > raycastHit.distance)
                    {
                        movDis = raycastHit.distance;
                        to     = raycastHit.point;
                    }
                }
            }
            if (m_interactWithDestructibleTilemap)
            {
                if (raycastHit.collider.TryGetComponent(out DestructibleTilemap destructibleTilemap))
                {
                    if (movDis > raycastHit.distance)
                    {
                        movDis = raycastHit.distance;
                        to     = raycastHit.point;
                    }
                }
            }
        }

        m_rigidbody.MovePosition(to);
    }

    public delegate void OnHit(LivingEntity entity, Projectile projectile);
}
