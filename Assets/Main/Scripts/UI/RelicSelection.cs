using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class RelicSelection : MonoBehaviour
{
    [SerializeField] private bool purchaseRequired = false;

    [Header("Relic Pools")]
    [SerializeField] private GameObject[] commonRelics;
    [SerializeField] private GameObject[] rareRelics;
    [SerializeField] private GameObject[] cursedRelics;

    [Header("References")]
    [SerializeField] private Transform       relic1;
    [SerializeField] private Transform       relic2;
    [SerializeField] private Transform       relic3;
    [SerializeField] private TextMeshProUGUI relicName1;
    [SerializeField] private TextMeshProUGUI relicName2;
    [SerializeField] private TextMeshProUGUI relicName3;
    [SerializeField] private TextMeshProUGUI relicDescription1;
    [SerializeField] private TextMeshProUGUI relicDescription2;
    [SerializeField] private TextMeshProUGUI relicDescription3;
    [SerializeField] private TextMeshProUGUI relicPrice1;
    [SerializeField] private TextMeshProUGUI relicPrice2;
    [SerializeField] private TextMeshProUGUI relicPrice3;

    [Header("Events")]
    [SerializeField] private SelectCallback selectCallback;

    private GameObject m_relic1;
    private GameObject m_relic2;
    private GameObject m_relic3;

    public void Clear()
    {
        if (m_relic1 != null)
        {
            Destroy(m_relic1);
            m_relic1 = null;
        }
        if (m_relic2 != null)
        {
            Destroy(m_relic2);
            m_relic2 = null;
        }
        if (m_relic3 != null)
        {
            Destroy(m_relic3);
            m_relic3 = null;
        }
    }

    public void Query(QueryInfo queryInfo)
    {
        System.Random rng = new System.Random();
        QueryResult query = queryInfo.Get(rng, commonRelics, rareRelics, cursedRelics);
        if (query.relic1 != null)
        {
            GameObject go1 = Instantiate(query.relic1, relic1.position, Quaternion.identity, relic1);
            if (go1.TryGetComponent(out Relic r))
            {
                relicName1.text = r.property.name;
                relicDescription1.text = r.property.description;
                if (relicPrice1 != null)
                {
                    relicPrice1.text = "" + r.property.price;
                }
                m_relic1 = go1;
            }
            else
            {
                relicName1.text = "Unknown Relic";
                relicDescription1.text = "";
                if (relicPrice1 != null)
                {
                    relicPrice1.text = "N/A";
                }
                m_relic1 = null;
                Destroy(go1);
            }
        }
        else
        {
            relicName1.text = "Unknown Relic";
            relicDescription1.text = "";
            if (relicPrice1 != null)
            {
                relicPrice1.text = "N/A";
            }
            m_relic1 = null;
        }
        if (query.relic2 != null)
        { 
            GameObject go2 = Instantiate(query.relic2, relic2.position, Quaternion.identity, relic2);
            if (go2.TryGetComponent(out Relic r))
            {
                relicName2.text = r.property.name;
                relicDescription2.text = r.property.description;
                if (relicPrice2 != null)
                {
                    relicPrice2.text = "" + r.property.price;
                }
                m_relic2 = go2;
            }
            else
            {
                relicName2.text = "Unknown Relic";
                relicDescription2.text = "";
                if (relicPrice2 != null)
                {
                    relicPrice2.text = "N/A";
                }
                m_relic2 = null;
                Destroy(go2);
            }
        }
        else
        {
            relicName2.text = "Unknown Relic";
            relicDescription2.text = "";
            if (relicPrice2 != null)
            {
                relicPrice2.text = "N/A";
            }
            m_relic2 = null;
        }
        if (query.relic3 != null)
        {
            GameObject go3 = Instantiate(query.relic3, relic3.position, Quaternion.identity, relic3);
            if (go3.TryGetComponent(out Relic r))
            {
                relicName3.text = r.property.name;
                relicDescription3.text = r.property.description;
                if (relicPrice3 != null)
                {
                    relicPrice3.text = "" + r.property.price;
                }
                m_relic3 = go3;
            }
            else
            {
                relicName3.text = "Unknown Relic";
                relicDescription3.text = "";
                if (relicPrice3 != null)
                {
                    relicPrice3.text = "N/A";
                }
                m_relic3 = null;
                Destroy(go3);
            }
        }
        else
        {
            relicName3.text = "Unknown Relic";
            relicDescription3.text = "";
            if (relicPrice3 != null)
            {
                relicPrice3.text = "N/A";
            }
            m_relic3 = null;
        }
    }

    public void SelectRelic(int index)
    {
        if (index < 3)
        {
            GameObject relic = new GameObject[] { m_relic1, m_relic2, m_relic3 } [index];
            if (relic != null)
            {
                Player player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
                bool canBuy = true;
                if (purchaseRequired)
                {
                    canBuy = p_Purchase(relic.GetComponent<Relic>().property.price);
                }
                if (canBuy)
                {
                    if (player != null)
                    {
                        relic.GetComponent<Relic>().AddToPlayer(player);
                    }
                    selectCallback?.Invoke();
                    if (index == 0)
                    {
                        relicName1.text = "Unknown Relic";
                        relicDescription1.text = "";
                        if (relicPrice1 != null)
                        {
                            relicPrice1.text = "N/A";
                        }
                        if (m_relic1 != null)
                        {
                            Destroy(m_relic1);
                            m_relic1 = null;
                        }
                    }
                    else if (index == 1)
                    {
                        relicName2.text = "Unknown Relic";
                        relicDescription2.text = "";
                        if (relicPrice1 != null)
                        {
                            relicPrice2.text = "N/A";
                        }
                        if (m_relic2 != null)
                        {
                            Destroy(m_relic2);
                            m_relic2 = null;
                        }
                    }
                    else if (index == 2)
                    {
                        relicName3.text = "Unknown Relic";
                        relicDescription3.text = "";
                        if (relicPrice3 != null)
                        {
                            relicPrice3.text = "N/A";
                        }
                        if (m_relic3 != null)
                        {
                            Destroy(m_relic3);
                            m_relic3 = null;
                        }
                    }
                }
            }
        }
    }

    private bool p_Purchase(int amount)
    {
        return GameManager.SpendCoin(amount);
    }

    public struct QueryInfo 
    {
        private Optional<float> m_commonWeight;
        private Optional<float> m_rareWeight;
        private Optional<float> m_cursedWeight;

        public QueryResult Get(System.Random rng, IEnumerable<GameObject> commonRelics, IEnumerable<GameObject> rareRelics, IEnumerable<GameObject> cursedRelics)
        {
            GameObject relic1 = null;
            GameObject relic2 = null;
            GameObject relic3 = null;

            List<GameObject> commons = new List<GameObject>(commonRelics);
            List<GameObject> rares   = new List<GameObject>(rareRelics);
            List<GameObject> curseds = new List<GameObject>(cursedRelics);

            float totalWeight = 0.0f;
            if (m_commonWeight.enabled) totalWeight += m_commonWeight.value;
            if (m_rareWeight.enabled)   totalWeight += m_rareWeight.value;
            if (m_cursedWeight.enabled) totalWeight += m_cursedWeight.value;

            if (totalWeight > 0.0f)
            {
                float random1 = (float)rng.NextDouble() * totalWeight;
                float random2 = (float)rng.NextDouble() * totalWeight;
                float random3 = (float)rng.NextDouble() * totalWeight;

                float w1 = (m_commonWeight.enabled ? m_commonWeight.value : 0.0f);
                float w2 = (m_rareWeight.enabled   ? m_rareWeight.value   : 0.0f) + w1;
                float w3 = (m_cursedWeight.enabled ? m_cursedWeight.value : 0.0f) + w2;

                if (relic1 == null && random1 <= w1 && m_commonWeight.enabled)
                {
                    if (commons.Count > 0)
                    {
                        int idx = rng.Next(commons.Count);
                        relic1 = commons[idx];
                        commons.RemoveAt(idx);
                    }
                }
                if (relic1 == null && random1 <= w2 && m_rareWeight.enabled)
                {
                    if (rares.Count > 0)
                    {
                        int idx = rng.Next(rares.Count);
                        relic1 = rares[idx];
                        rares.RemoveAt(idx);
                    }
                }
                if (relic1 == null && random1 <= w3 && m_cursedWeight.enabled)
                {
                    if (curseds.Count > 0)
                    {
                        int idx = rng.Next(curseds.Count);
                        relic1 = curseds[idx];
                        curseds.RemoveAt(idx);
                    }
                }
                
                if (relic2 == null && random2 <= w1 && m_commonWeight.enabled)
                {
                    if (commons.Count > 0)
                    {
                        int idx = rng.Next(commons.Count);
                        relic2 = commons[idx];
                        commons.RemoveAt(idx);
                    }
                }
                if (relic2 == null && random2 <= w2 && m_rareWeight.enabled)
                {
                    if (rares.Count > 0)
                    {
                        int idx = rng.Next(rares.Count);
                        relic2 = rares[idx];
                        rares.RemoveAt(idx);
                    }
                }
                if (relic2 == null && random2 <= w3 && m_cursedWeight.enabled)
                {
                    if (curseds.Count > 0)
                    {
                        int idx = rng.Next(curseds.Count);
                        relic2 = curseds[idx];
                        curseds.RemoveAt(idx);
                    }
                }
                
                if (relic3 == null && random3 <= w1 && m_commonWeight.enabled)
                {
                    if (commons.Count > 0)
                    {
                        int idx = rng.Next(commons.Count);
                        relic3 = commons[idx];
                        commons.RemoveAt(idx);
                    }
                }
                if (relic3 == null && random3 <= w2 && m_rareWeight.enabled)
                {
                    if (rares.Count > 0)
                    {
                        int idx = rng.Next(rares.Count);
                        relic3 = rares[idx];
                        rares.RemoveAt(idx);
                    }
                }
                if (relic3 == null && random3 <= w3 && m_cursedWeight.enabled)
                {
                    if (curseds.Count > 0)
                    {
                        int idx = rng.Next(curseds.Count);
                        relic3 = curseds[idx];
                        curseds.RemoveAt(idx);
                    }
                }
            }

            return new QueryResult(relic1, relic2, relic3);
        }

        public static QueryInfo Create()
        {
            return new QueryInfo();
        }

        public QueryInfo Common(float weight)
        {
            m_commonWeight = new Optional<float>(weight);
            return this;
        }

        public QueryInfo Rare(float weight)
        {
            m_rareWeight = new Optional<float>(weight);
            return this;
        }

        public QueryInfo Cursed(float weight)
        {
            m_cursedWeight = new Optional<float>(weight);
            return this;
        }
    }

    public struct QueryResult
    {
        public GameObject relic1;
        public GameObject relic2;
        public GameObject relic3;

        public QueryResult(GameObject relic1, GameObject relic2, GameObject relic3)
        {
            this.relic1 = relic1;
            this.relic2 = relic2;
            this.relic3 = relic3;
        }
    }

    [Serializable]
    public class SelectCallback : UnityEvent { }
}
