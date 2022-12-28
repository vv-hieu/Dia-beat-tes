using UnityEngine;

public class RelicSpecialEffectHellsteelSilverBullet : RelicSpecialEffect
{
    public override void ModifyAttackDealt(LivingEntity.AttackInfo attackInfo, LivingEntity target)
    {
        if (target.HasTag("Undead"))
        {
            attackInfo.damage += 2.0f;
        }
        else
        {
            attackInfo.damage -= 2.0f;
        }
    }
}
