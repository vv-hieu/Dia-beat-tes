using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class WeaponForging : MonoBehaviour
{
    [Header("Weapon Pools")]
    [SerializeField] private SerializableDictionary<GameObject, float> weapons = new SerializableDictionary<GameObject, float>();

    [Header("References")]
    [SerializeField] private Image           weaponImage;
    [SerializeField] private TextMeshProUGUI weaponName;

    [Header("Events")]
    [SerializeField] private ForgeCallback forgeCallback;

    private GameObject m_weapon;
    private string     m_weaponName;
    private bool       m_init = false;

    public void Forge()
    {
        System.Random rng = new System.Random();
        if (weapons.Count > 0 && p_UseMoney(100))
        {
            float totalWeight = 0.0f;
            foreach (var p in weapons)
            {
                if (p.Key.GetComponent<Weapon>().Name() != m_weaponName)
                {
                    totalWeight += p.Value;
                }
            }

            if (totalWeight > 0.0f)
            {
                float random = (float)rng.NextDouble() * totalWeight;
                float current = 0.0f;
                foreach (var p in weapons)
                {
                    if (p.Key.GetComponent<Weapon>().Name() == m_weaponName)
                    {
                        continue;
                    }

                    current += p.Value;
                    if (random <= current)
                    {
                        m_weapon = p.Key;

                        if (m_weapon.TryGetComponent(out Weapon w))
                        {
                            LivingEntity player = GameObject.FindGameObjectWithTag("Player").GetComponent<LivingEntity>();
                            if (player != null)
                            {
                                player.SetWeapon(m_weapon);
                            }
                            weaponImage.sprite = w.Sprite();
                            weaponName.text    = w.Name();
                            forgeCallback?.Invoke();
                        }
                        break;
                    }
                }
            }
        }
    }

    public void Init()
    {
        if (!m_init)
        {
            m_init = true;

            Weapon defaultWeapon = GameObject.FindGameObjectWithTag("Player").GetComponent<LivingEntity>().GetWeapon();
            if (defaultWeapon != null)
            {
                m_weaponName       = defaultWeapon.Name();
                weaponImage.sprite = defaultWeapon.Sprite();
                weaponName.text    = defaultWeapon.Name();
            }
        }
    }

    private bool p_UseMoney(int amount)
    {
        return true;
    }

    [Serializable]
    public class ForgeCallback : UnityEvent { }
}
