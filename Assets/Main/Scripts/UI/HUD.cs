using UnityEngine;
using TMPro;

public class HUD : MonoBehaviour
{
    [SerializeField] private HealthBar          healthBar;
    [SerializeField] private BulletCountDisplay bulletDisplay;

    [SerializeField] private TextMeshProUGUI currentHealth;
    [SerializeField] private TextMeshProUGUI currentShield;
    [SerializeField] private TextMeshProUGUI currentTimer;
    [SerializeField] private TextMeshProUGUI totalCoin;
    [SerializeField] private RectTransform   bullet;

    private LivingEntity m_livingEntity;
    private Player       m_player;

    private void Awake()
    {
        GameObject go = GameObject.FindGameObjectWithTag("Player");
        if (go != null)
        {
            m_livingEntity = go.GetComponent<LivingEntity>();
            m_player       = go.GetComponent<Player>();

            healthBar.SetEntity(m_livingEntity);
            bulletDisplay.SetEntity(m_livingEntity);
        }
    }

    private void Update()
    {
        if (currentHealth != null && currentShield != null)
        {
            currentHealth.text = "" + (Mathf.Round(m_livingEntity.currentHealth *10.0f) * 0.1f);
            currentShield.text = "" + (Mathf.Round(m_livingEntity.currentShield *10.0f) * 0.1f);
        }

        if (bullet != null)
        {
            float bulletCapacity = 0.0f;
            if (m_livingEntity != null && m_livingEntity.GetWeapon() != null)
            {
                bulletCapacity = m_livingEntity.GetWeapon().BulletCapacity();
            }
            bullet.sizeDelta = new Vector2(Mathf.Floor(bulletCapacity) * 60.0f, 60.0f);
        }

        if (currentTimer != null)
        {
            int seconds = (int)GameManager.GetGameContext().timeSinceStart;
            int m = seconds / 60;
            int s = seconds % 60;
            currentTimer.text = "" + (m / 10) + "" + (m % 10) + " : " + (s / 10) + "" + (s % 10);
        }

        if (totalCoin != null)
        {
            totalCoin.text = "" + GameManager.GetGameContext().totalCoinCount;
        }
    }
}
