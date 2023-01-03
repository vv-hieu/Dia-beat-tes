using UnityEngine;

public class GunWeapon : RangedWeapon
{
    [Header("Projectile")]
    [SerializeField] private GameObject projectile;
    [SerializeField] private Transform  projectileSpawnPosition;
    [SerializeField] private float      recoil;

    [Header("Weapon Stats")]
    [SerializeField] private float damage;
    [SerializeField] private float knockback;
    [SerializeField] private float stunTime;
    [SerializeField] private float fireRate;
    [SerializeField] private float reloadTime;
    [SerializeField] private float bulletCount;
    [SerializeField] private float bulletSpread;
    [SerializeField] private float bulletSpeed;
    [SerializeField] private float bulletLifetime;
    [SerializeField] private float bulletCapacity;

    [Header("Tags")]
    [SerializeField] private string[] tags;

    private int   m_currentBulletCount;
    private float m_time         = 0.0f;
    private float m_reloadRotate = 360.0f;
    private bool  m_reloaded     = false;

    public override void OnStart()
    {
        p_Reload();
        if (projectileSpawnPosition == null)
        {
            projectileSpawnPosition = transform;
        }
    }

    public override void OnCoolDown()
    {
        OffsetTo(0.0f, 0.5f / p_FireRate());
        m_reloaded = false;
    }

    public override void OnUse()
    {
        if (projectile != null)
        {
            int bulletToSpawn = p_BulletCount();
            if (bulletToSpawn > m_currentBulletCount)
            {
                bulletToSpawn = m_currentBulletCount;
            }
            float spreadAngle         = p_BulletSpread();
            float angleBetweenBullets = spreadAngle / (bulletToSpawn + 1.0f);
            float angle               = angleBetweenBullets - spreadAngle * 0.5f;

            m_currentBulletCount -= bulletToSpawn;
            while (bulletToSpawn > 0)
            {
                p_SpawnProjectileAtAngle(angle);
                angle += angleBetweenBullets;
                --bulletToSpawn;
            }
        }
        OffsetTo(-Mathf.Abs(recoil), 0.5f / p_FireRate());
    }

    public override void CoolingDown()
    {
        if (m_currentBulletCount <= 0)
        {
            if (!m_reloaded && m_time >= 0.5f / p_FireRate())
            {
                m_reloaded = true;
                RotateTo(GetAngle() + m_reloadRotate, p_ReloadTime());
                m_reloadRotate *= -1.0f;
            }
        }

        m_time += Time.deltaTime;
    }

    public override void Using()
    {
        m_time += Time.deltaTime;
    }

    public override bool CoolDownEnded()
    {
        if (m_currentBulletCount > 0)
        {
            if (m_time >= 0.5f / p_FireRate())
            {
                m_time = 0.0f;
                return true;
            }
            return false;
        }

        if (m_time >= p_ReloadTime() + 0.5f / p_FireRate())
        {
            p_Reload();
            m_time = 0.0f;
            return true;
        }
        return false;
    }

    public override bool UseEnded()
    {
        if (m_time >= 0.5f / p_FireRate())
        {
            m_time = 0.0f;
            return true;
        }
        return false;
    }

    public override Optional<float> ReachDistance()
    {
        return new Optional<float>(0.8f * p_BulletLifetime() * p_BulletSpeed());
    }

    private void p_Reload()
    {
        m_currentBulletCount = p_BulletCapacity();
    }

    private float p_FireRate()
    {
        float userAttackSpeed = 0.0f;
        float a = 1.0f;
        float b = 0.0f;
        float c = 1.0f;
        if (GetUser() != null)
        {
            userAttackSpeed = GetUser().statSet.GetValueAndCoefficients("attackSpeed", out a, out b, out c);
        }
        return c * a * fireRate + userAttackSpeed;
    }

    private float p_ReloadTime()
    {
        float userReloadSpeed = 0.0f;
        if (GetUser() != null)
        {
            userReloadSpeed = GetUser().statSet.GetValue("reloadSpeed");
        }
        return reloadTime / (1.0f + userReloadSpeed * 0.4f);
    }

    private int p_BulletCount()
    {
        float userBulletCount = 0.0f;
        float a = 1.0f;
        float b = 0.0f;
        float c = 1.0f;
        if (GetUser() != null)
        {
            userBulletCount = GetUser().statSet.GetValueAndCoefficients("bulletCount", out a, out b, out c);
        }
        return (int)(c * a * bulletCount + userBulletCount);
    }

    private int p_BulletCapacity()
    {
        float userBulletCapacity = 0.0f;
        float a = 1.0f;
        float b = 0.0f;
        float c = 1.0f;
        if (GetUser() != null)
        {
            userBulletCapacity = GetUser().statSet.GetValueAndCoefficients("bulletCapacity", out a, out b, out c);
        }
        return (int)(c * a * bulletCapacity + userBulletCapacity);
    }

    private float p_Damage()
    {
        float userAttackDamage = 0.0f;
        float a = 1.0f;
        float b = 0.0f;
        float c = 1.0f;
        if (GetUser() != null)
        {
            userAttackDamage = GetUser().statSet.GetValueAndCoefficients("attackDamage", out a, out b, out c);
        }
        return c * a * damage + userAttackDamage;
    }

    private float p_BulletSpread()
    {
        float userBulletPrecision = 0.0f;
        if (GetUser() != null)
        {
            userBulletPrecision = GetUser().statSet.GetValue("bulletPrecision");
        }
        return bulletSpread * (1.0f - userBulletPrecision * 0.08f);
    }

    private float p_BulletSpeed()
    {
        float userBulletSpeed = 0.0f;
        float a = 1.0f;
        float b = 0.0f;
        float c = 1.0f;
        if (GetUser() != null)
        {
            userBulletSpeed = GetUser().statSet.GetValueAndCoefficients("bulletSpeed", out a, out b, out c);
        }
        return c * a * bulletSpeed + userBulletSpeed;
    }

    private float p_BulletLifetime()
    {
        float userBulletLifetime = 0.0f;
        float a = 1.0f;
        float b = 0.0f;
        float c = 1.0f;
        if (GetUser() != null)
        {
            userBulletLifetime = GetUser().statSet.GetValueAndCoefficients("bulletLifetime", out a, out b, out c);
        }
        return c * a * bulletLifetime + userBulletLifetime;
    }

    private void p_SpawnProjectileAtAngle(float angle)
    {
        float sa = Mathf.Sin(angle * Mathf.Deg2Rad);
        float ca = Mathf.Cos(angle * Mathf.Deg2Rad);

        Vector2 dir = new Vector2(ca * direction.x - sa * direction.y, sa * direction.x + ca * direction.y);

        GameObject go = Instantiate(projectile, projectileSpawnPosition.position, Quaternion.identity, GetUser().transform.parent);
        Projectile p = go.GetComponent<Projectile>();
        p.Init(GetUser(), p_BulletLifetime(), p_BulletSpeed(), dir, GetAffectedTags(), p_OnProjectileHit);
    }

    private void p_OnProjectileHit(LivingEntity entity, Projectile projectile)
    {
        LivingEntity.HandleAttack(projectile.owner, entity, LivingEntity.AttackInfo.Create()
           .KnockbackDirection(projectile.velocity)
           .Knockback(knockback)
           .Damage(p_Damage())
           .InvulnerableTime(stunTime)
           .StunTime(stunTime)
           .Tags(new string[] { "Projectile" })
        );

        Destroy(projectile.gameObject);
    }
}
