using UnityEngine;

public class RelicSpecialEffectHellsteelDagger : RelicSpecialEffect
{
    public override void ModifyCritDealt(ref float critChance, ref float critDamage, LivingEntity target)
    {
        int curseRelicCount = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>().cursedRelicCount;
        critChance += 2.0f * curseRelicCount;
        critDamage += 0.25f * curseRelicCount;
    }
}
