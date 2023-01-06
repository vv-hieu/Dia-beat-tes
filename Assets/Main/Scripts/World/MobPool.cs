using System;
using UnityEngine;

[CreateAssetMenu()]
public class MobPool : ScriptableObject
{
    public Entry[] entries;

    public GameObject Get(System.Random rng)
    {
        float totalWeight = 0.0f;
        foreach (Entry entry in entries)
        {
            totalWeight += entry.weight;
        }

        if (totalWeight > 0.0f)
        {
            float random = (float)rng.NextDouble() * totalWeight;
            float current = 0.0f;

            foreach (Entry entry in entries)
            {
                current += entry.weight;
                if (random <= current)
                {
                    return entry.mob;
                }
            }
        }

        return null;
    }

    [Serializable]
    public struct Entry
    {
        public GameObject mob;
        public float weight;
    }
}
