using UnityEngine;

public class Boss : MonoBehaviour
{
    [SerializeField] private AudioClip deathSound;

    protected LivingEntity livingEntity { get; private set; }

    private bool m_died = false;

    protected virtual void OnAwake()
    {
    }

    protected virtual void OnStart()
    {
    }

    protected virtual void OnUpdate()
    {
    }

    protected virtual void OnDeath()
    {
    }

    private void Awake()
    {
        livingEntity = GetComponent<LivingEntity>();
        OnAwake();
    }

    private void Start()
    {
        livingEntity.onDeath = p_OnDeath;
        OnStart();
    }

    private void Update()
    {
        OnUpdate();
    }

    private void p_OnDeath()
    {
        OnDeath();
        SoundManager.PlaySound(deathSound);
    }
}
