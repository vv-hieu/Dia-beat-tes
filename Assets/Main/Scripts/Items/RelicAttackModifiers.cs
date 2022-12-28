using UnityEngine;

public class RelicAttackModifiers : MonoBehaviour
{
    public void temp(LivingEntity.AttackInfo attackInfo, LivingEntity receiver)
    {
        attackInfo.damage *= 2.0f;
    }

    public void AttackReceivedModifier_chainmail_armor(LivingEntity.AttackInfo attackInfo, LivingEntity attacker)
    {
        Debug.Log("Lmao 2");
        attackInfo.carryOverDamage = false;
    }
}
