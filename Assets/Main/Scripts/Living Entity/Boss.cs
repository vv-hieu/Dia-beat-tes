using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss : MonoBehaviour
{
    protected LivingEntity livingEntity { get; private set; }

    protected virtual void OnStart()
    {
    }

    protected virtual void OnUpdate()
    {
    }

    private void Awake()
    {
        livingEntity = GetComponent<LivingEntity>();
    }

    private void Start()
    {
        OnStart();
    }

    private void Update()
    {
        OnUpdate();
    }
}
