using UnityEngine;

public class RelicSpecialEffect : MonoBehaviour
{
    public virtual void ModifyCritDealt(ref float critChance, ref float critDamage, LivingEntity target)
    {
    }

    public virtual void ModifyCritReceived(ref float critChance, ref float critDamage, LivingEntity attacker)
    {
    }

    public virtual void ModifyAttackDealt(LivingEntity.AttackInfo attackInfo, LivingEntity target)
    {
    }
    
    public virtual void ModifyAttackReceived(LivingEntity.AttackInfo attackInfo, LivingEntity attacker)
    {
    }
}
