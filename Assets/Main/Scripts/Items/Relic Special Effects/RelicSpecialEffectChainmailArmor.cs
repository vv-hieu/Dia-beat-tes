public class RelicSpecialEffectChainmailArmor : RelicSpecialEffect
{
    public override void ModifyAttackReceived(LivingEntity.AttackInfo attackInfo, LivingEntity attacker)
    {
        attackInfo.carryOverDamage = false;
    }
}
