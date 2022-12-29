using UnityEngine;

public class RelicEffect : MonoBehaviour
{
    public virtual LivingEntity.StatModifier GenerateStatModifier(GameManager.GameContext context)
    {
        return new LivingEntity.StatModifier();
    }

    public virtual LivingEntity.AttackModifier GenerateAttackDealtModifier(GameManager.GameContext context)
    {
        return new LivingEntity.AttackModifier();
    }
    
    public virtual LivingEntity.AttackModifier GenerateAttackReceivedModifier(GameManager.GameContext context)
    {
        return new LivingEntity.AttackModifier();
    }
}
