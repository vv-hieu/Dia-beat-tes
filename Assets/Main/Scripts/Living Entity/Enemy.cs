using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    private NavMeshAgent m_navMeshAgent;

    protected Player       target       { get; private set; }
    protected LivingEntity livingEntity { get; private set; }

    protected void SetDestination(Vector2 pos)
    {
        if (m_navMeshAgent.isOnNavMesh)
        {
            m_navMeshAgent.SetDestination(new Vector3(pos.x, pos.y, 0.0f));
        }
    }

    protected void SetStopDistance(float distance)
    {
        if (m_navMeshAgent != null)
        {
            m_navMeshAgent.stoppingDistance = distance;
        }
    }

    protected bool IsInStopDistance()
    {
        if (m_navMeshAgent == null || target == null)
        {
            return false;
        }
        return Vector2.Distance(new Vector2(transform.position.x, transform.position.y), new Vector2(target.transform.position.x, target.transform.position.y)) <= m_navMeshAgent.stoppingDistance;
    }

    protected virtual void OnStart()
    {
    }

    protected virtual void OnUpdate()
    {
    }

    private void Awake()
    {
        m_navMeshAgent = GetComponent<NavMeshAgent>();
        livingEntity   = GetComponent<LivingEntity>();

        livingEntity.statSet.AddModifier("local_difficulty_modifier", new LocalDifficultyStatModifier());
    }

    private void Start()
    {
        m_navMeshAgent.updateUpAxis   = false;
        m_navMeshAgent.updateRotation = false;

        OnStart();
    }

    private void Update()
    {
        target = GameManager.GetGameContext().player;

        OnUpdate();

        transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, 0.0f);
    }

    private class LocalDifficultyStatModifier : LivingEntity.StatModifier
    {
        public override List<LivingEntity.StatModifyingOperation> Modify()
        {
            int completedLevels = GameManager.GetGameContext().completedLevels;

            List<LivingEntity.StatModifyingOperation> res = new List<LivingEntity.StatModifyingOperation>();

            res.Add(LivingEntity.StatModifyingOperation.AdditionValue("attackDamage", 0.75f * completedLevels));
            res.Add(LivingEntity.StatModifyingOperation.AdditionValue("meleeRange"  , 1.25f * completedLevels));
            res.Add(LivingEntity.StatModifyingOperation.AdditionValue("bulletSpeed" , 1.50f * completedLevels));

            res.Add(LivingEntity.StatModifyingOperation.AdditionPercent("speed"      , 0.35f * completedLevels));
            res.Add(LivingEntity.StatModifyingOperation.AdditionPercent("attackSpeed", 0.15f * completedLevels));

            res.Add(LivingEntity.StatModifyingOperation.AdditionValue("shield", 0.5f * completedLevels));

            return res;
        }
    }
}
