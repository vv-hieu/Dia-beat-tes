using UnityEngine;

public class RelicSpecialEffectHellsteelBlade : RelicSpecialEffect
{
    public override void ModifyAttackDealt(LivingEntity.AttackInfo attackInfo, LivingEntity target)
    {
        int curseRelicCount = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>().cursedRelicCount;
        attackInfo.damage += curseRelicCount * 0.2f;
    }
}
