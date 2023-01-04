using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss : MonoBehaviour
{
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
        livingEntity.onDeath = OnDeath;
        OnStart();
    }

    private void Update()
    {
        OnUpdate();
    }
}
