using UnityEngine;

public class RelicSpecialEffectGoldenApple : RelicSpecialEffect
{
    public override void ModifyAttackReceived(LivingEntity.AttackInfo attackInfo, LivingEntity attacker)
    {
        float health = 0.0f;
        if (GameObject.FindGameObjectWithTag("Player").GetComponent<LivingEntity>().statSet.TryGetValue("health", out float h)) {
            health = h;
        }
        attackInfo.damage = Mathf.Min(0.1f * health, attackInfo.damage);
    }
}
