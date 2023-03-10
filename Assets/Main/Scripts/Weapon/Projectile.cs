using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Projectile : MonoBehaviour
{
    [Header("Appearance")]
    [SerializeField] private Color    color           = Color.red;
    [SerializeField] private float    size            = 0.1f;
    [SerializeField] private Gradient trailColor      = new Gradient();
    [SerializeField] private float    trailSize       = 0.1f;
    [SerializeField] private float    trailLifeTime   = 0.5f;
    [SerializeField] private float    angularVelocity = 0.0f;

    public float        lifeTime { get; private set; }
    public Vector2      velocity { get; private set; }
    public LivingEntity owner    { get; private set; }

    private SpriteRenderer m_sprite;
    private TrailRenderer  m_trail;
    private Rigidbody2D    m_rigidbody;
    private Collider2D     m_collider;
    private string[]       m_affectTags;
    private OnHit          m_onHit;
    private bool           m_init                            = false;
    private bool           m_interactWithDestructibleTilemap = false;
    private float          m_rotate                          = 0.0f;

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

    private void Awake()
    {
        m_sprite    = GetComponent<SpriteRenderer>();
        m_trail     = GetComponent<TrailRenderer>();
        m_rigidbody = GetComponent<Rigidbody2D>();
        m_collider  = GetComponent<Collider2D>();
    }

    private void Start()
    {
        m_sprite.color                    = color;
        m_trail.widthMultiplier           = trailSize;
        m_trail.colorGradient             = trailColor;
        m_trail.time                      = trailLifeTime;
        m_interactWithDestructibleTilemap = TryGetComponent(out DestructibleTilemapInteraction destructibleTilemapInteraction);
        transform.localScale              = Vector3.one * size;
    }

    private void OnValidate()
    {
        m_sprite.color          = color;
        m_trail.widthMultiplier = trailSize;
        m_trail.colorGradient   = trailColor;
        m_trail.time            = trailLifeTime;
        transform.localScale    = Vector3.one * size;
    }

    private void Update()
    {
        if (m_init)
        {
            lifeTime -= Time.deltaTime * (GameStateManager.instance.currentState == GameState.Gameplay ? 1.0f : 0.0f);
            if (lifeTime <= 0.0f)
            {
                lifeTime = 0.0f;
                Destroy(gameObject);
            }
            m_rotate += Time.deltaTime * (GameStateManager.instance.currentState == GameState.Gameplay ? 1.0f : 0.0f) * angularVelocity;
        }
    }

    private void FixedUpdate()
    {
        Vector2 oldPos = new Vector2(transform.position.x, transform.position.y);
        Vector2 newPos = oldPos + velocity * Time.fixedDeltaTime * (GameStateManager.instance.currentState == GameState.Gameplay ? 1.0f : 0.0f);
        p_Move(oldPos, newPos);
        p_RotateToDir();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        LivingEntity entity = LivingEntity.FromCollider(collision);
        if (entity != null && m_onHit != null)
        {
            if (entity.HasTagsAny(m_affectTags))
            {
                m_onHit(entity, this);
            }
        }

        if (collision.gameObject.layer == LayerMask.NameToLayer("ProjectileBlocking"))
        {
            Destroy(gameObject);
        }   
    }

    private void p_Move(Vector2 from, Vector2 to)
    {
        if (from == to)
        {
            return;
        }

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

    private void p_RotateToDir()
    {
        Vector2 rotDir = velocity;
        if (velocity == Vector2.zero)
        {
            rotDir = Vector2.right;
        }
        float ca = Mathf.Cos(m_rotate * Mathf.Deg2Rad);
        float sa = Mathf.Sin(m_rotate * Mathf.Deg2Rad);
        rotDir = new Vector2(ca * rotDir.x - sa * rotDir.y, sa * rotDir.x + ca * rotDir.y);
        m_sprite.transform.rotation = Quaternion.LookRotation(Vector3.forward, rotDir);
    }

    public delegate void OnHit(LivingEntity entity, Projectile projectile);
}
