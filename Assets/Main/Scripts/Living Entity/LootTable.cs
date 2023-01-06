using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class LootTable : ScriptableObject
{
    public LootPool[] pools;

    public List<GameObject> Get(System.Random rng, float luck)
    {
        List<GameObject> res = new List<GameObject>();
        foreach (LootPool pool in pools)
        {
            res.AddRange(pool.Get(rng, luck));
        }
        return res;
    }

    [Serializable]
    public struct LootPool
    {
        [Range(0.0f, 1.0f)] public float chance;
        public float additionalChancePerLuckLevel;
        public int rolls;
        public Entry[] entries;

        public List<GameObject> Get(System.Random rng, float luck)
        {
            List<GameObject> res = new List<GameObject>();

            float totalWeight = 0.0f;
            foreach (Entry entry in entries)
            {
                totalWeight += entry.Weight(luck);
            }

            if (totalWeight > 0.0f)
            {
                for (int i = 0; i < rolls; ++i)
                {
                    if ((float)rng.NextDouble() > Mathf.Clamp01(chance + additionalChancePerLuckLevel * luck))
                    {
                        continue;
                    }

                    float random = (float)rng.NextDouble() * totalWeight;
                    float current = 0.0f;
                    foreach (Entry entry in entries)
                    {
                        current += entry.Weight(luck);
                        if (random <= current)
                        {
                            res.Add(entry.item);
                            break;
                        }
                    }
                }
            }

            return res;
        }

        [Serializable]
        public struct Entry
        {
            public GameObject item;
            public float      baseWeight;
            public float      additionalWeightPerLuckLevel;

            public float Weight(float luck)
            {
                return Mathf.Max(0.0f, baseWeight + additionalWeightPerLuckLevel * luck);
            }
        }
    }
}
