using UnityEngine;

public class Collectible : MonoBehaviour
{
    [Header("Collectible")]
    [SerializeField] private CollectibleType type = CollectibleType.Unknown;

    [Header("VFX")]
    [SerializeField] private GameObject pickupEffect;

    [Header("References")]
    [SerializeField] private Transform sprite;
    [SerializeField] private Transform shadow;

    [HideInInspector] public string id;

    private Vector3 m_originalSpritePos;
    private Vector3 m_originalShadowScale;
    private Vector3 m_originalScale;
    private float   m_time = 0.0f;

    private static System.Random RANDOM       = new System.Random();
    private static float         DESPAWN_TIME = 0.5f * 60.0f;

    private void Start()
    {
        m_originalSpritePos   = sprite.localPosition;
        m_originalShadowScale = shadow.localScale;
        m_originalScale       = transform.localScale;
    }

    private void Update()
    {
        m_time += Time.deltaTime;
        float d = Mathf.Cos(2.0f * m_time);
        sprite.localPosition = m_originalSpritePos + Vector3.up * d * 0.02f;
        shadow.localScale = m_originalShadowScale * (1.0f + d * 0.05f);
        transform.localScale = m_originalScale * Mathf.Clamp01(m_time * 5.0f);

        if (m_time >= DESPAWN_TIME - 5.0f)
        {
            bool enabled = (int)((m_time - DESPAWN_TIME) * 4.0f) % 2 != 0;
            sprite.gameObject.SetActive(enabled);
            shadow.gameObject.SetActive(enabled);

            if (m_time >= DESPAWN_TIME)
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        LivingEntity livingEntity = LivingEntity.FromCollider(collision);
        if (livingEntity != null)
        {
            Player player = livingEntity.GetComponent<Player>();
            if (player != null)
            {
                if (player.CanPickUp(type))
                {
                    if (pickupEffect != null)
                    {
                        Instantiate(pickupEffect, player.transform.position, Quaternion.identity, player.transform);
                    }
                    switch (type)
                    {
                        case CollectibleType.CakePiece:
                            {
                                player?.AddFatness(0.05f);
                                break;
                            }
                        case CollectibleType.Cherry:
                            {
                                livingEntity.Heal((float)RANDOM.NextDouble() * 2.0f + 1.0f);
                                break;
                            }
                        case CollectibleType.Strawberry:
                            {
                                livingEntity.Heal((float)RANDOM.NextDouble() * 2.0f + 3.0f);
                                break;
                            }
                        case CollectibleType.EnergyDrink:
                            {

                                break;
                            }
                        default:
                            break;
                    }
                    Destroy(gameObject);
                }
            }
        }
    }

    public enum CollectibleType
    {
        Unknown = -1,

        CakePiece,  // Increases player's fat-o-meter
        Cherry,     // Heal 1 - 3 health
        Strawberry, // Heal 3 - 5 health
        EnergyDrink // Frenzy mode
    }
}
