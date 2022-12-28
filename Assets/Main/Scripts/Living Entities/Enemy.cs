using UnityEngine;
using UnityEngine.AI;

[ExecuteInEditMode]
public class Enemy : MonoBehaviour
{
    private NavMeshAgent m_navMeshAgent;

    private void Start()
    {
        m_navMeshAgent = GetComponent<NavMeshAgent>();
        m_navMeshAgent.updateUpAxis   = false;
        m_navMeshAgent.updateRotation = false;
    }

    private void Update()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            return;
        }

        if (m_navMeshAgent.isOnNavMesh)
        {
            m_navMeshAgent.SetDestination(player.transform.position);
        }
        transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, 0.0f);
    }
}
