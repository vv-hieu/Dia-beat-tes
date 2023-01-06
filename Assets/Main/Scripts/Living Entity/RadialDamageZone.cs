using UnityEngine;

public class RadialDamageZone : MonoBehaviour
{
    private LivingEntity m_owner;
    private bool         m_init = false;
    private bool         m_crit;
    private bool         m_useOwnerAttackDamage;
    private float        m_damage;
    private float        m_knockback;
    private float        m_stunTime;
    private float        m_invulnerableTime;
    private string[]     m_tags;
    private string[]     m_affectedTags;

    public void Init(LivingEntity owner, float damage, float knockback, float stunTime, float invulnerableTime, string[] tags, string[] affectedTags, bool crit, bool useOwnerAttackDamage)
    {
        if (!m_init)
        {
            m_init = true;

            m_owner                = owner;
            m_damage               = damage;
            m_knockback            = knockback;
            m_stunTime             = stunTime;
            m_invulnerableTime     = invulnerableTime;
            m_tags                 = tags;
            m_affectedTags         = affectedTags;
            m_crit                 = crit;
            m_useOwnerAttackDamage = useOwnerAttackDamage;
        }
    }

    private void Update()
    {
        if (m_owner != null && m_owner.statSet.TryGetValue("attackDamage", out float d) && m_useOwnerAttackDamage)
        {
            m_damage = d;
        }

        if (m_init)
        {
            Collider2D[] colliders = new Collider2D[100];
            int count = Physics2D.OverlapCollider(GetComponent<Collider2D>(), new ContactFilter2D().NoFilter(), colliders);

            for (int i = 0; i < count; ++i)
            {
                LivingEntity entity = LivingEntity.FromCollider(colliders[i]);
                if (entity != null)
                {
                    if (entity.HasTagsAny(m_affectedTags))
                    {
                        Vector2 direction = new Vector2(entity.transform.position.x - transform.position.x, entity.transform.position.y - transform.position.y);

                        LivingEntity.HandleAttack(m_owner, entity, LivingEntity.AttackInfo.Create()
                            .Damage(m_damage)
                            .Knockback(m_knockback)
                            .StunTime(m_stunTime)
                            .InvulnerableTime(m_invulnerableTime)
                            .KnockbackDirection(direction)
                            .Tags(m_tags), m_crit);
                    }
                }
            }
        }
    }
}
