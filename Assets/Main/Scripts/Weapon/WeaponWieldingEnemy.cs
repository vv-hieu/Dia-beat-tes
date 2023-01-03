using UnityEngine;

public class WeaponWieldingEnemy : Enemy
{
    [SerializeField] private GameObject weapon;

    private bool m_weaponHasNoReach = false;

    protected override void OnStart()
    {
        livingEntity.SetWeapon(weapon);
        m_weaponHasNoReach = true;
        SetStopDistance(5.0f);
        Weapon w = livingEntity.GetWeapon();
        if (w != null)
        {
            Optional<float> reach = w.ReachDistance();
            if (reach.enabled)
            {
                m_weaponHasNoReach = false;
                SetStopDistance(reach.value);
            }
        }
    }

    protected override void OnUpdate()
    {
        if (!m_weaponHasNoReach)
        {
            Vector2 targetPos = new Vector2(target.transform.position.x, target.transform.position.y);
            SetDestination(targetPos);
            livingEntity.AimWeaponAt(targetPos);
        }
        if (IsInStopDistance() || m_weaponHasNoReach)
        {
            livingEntity.UseWeapon(new string[] { "Friendly" });
        }
    }
}
