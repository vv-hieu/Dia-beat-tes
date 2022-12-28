using UnityEngine;

public class RelicSpecialEffectHellsteelArmor : RelicSpecialEffect
{
    public override void ModifyAttackReceived(LivingEntity.AttackInfo attackInfo, LivingEntity attacker)
    {
        int curseRelicCount = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>().cursedRelicCount;
        attackInfo.damage -= curseRelicCount * 0.2f;
    }
}
