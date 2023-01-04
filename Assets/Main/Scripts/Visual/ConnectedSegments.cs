using UnityEngine;

public class ConnectedSegments : MonoBehaviour
{
    [SerializeField] private Transform[] segments;
    [SerializeField] private float       segmentTargetDistance;
    [SerializeField] private float       smoothSpeed;

    private Vector3[] m_segmentPositions;
    private Vector3[] m_segmentVelocities;

    public Transform[] GetSegments()
    {
        return segments;
    }

    private void Awake()
    {
        m_segmentPositions = new Vector3[segments.Length + 1];
        m_segmentVelocities = new Vector3[segments.Length + 1];
    }

    private void Update()
    {
        if (m_segmentPositions.Length > 0)
        {
            m_segmentPositions[0] = transform.position;
            for (int i = 1; i < m_segmentPositions.Length; ++i)
            {
                Vector3 targetPos = m_segmentPositions[i - 1] + (m_segmentPositions[i] - m_segmentPositions[i - 1]).normalized * segmentTargetDistance;
                m_segmentPositions[i] = Vector3.SmoothDamp(m_segmentPositions[i], targetPos, ref m_segmentVelocities[i], 1.0f / smoothSpeed);

                Vector3 position = m_segmentPositions[i];
                Vector3 direction = m_segmentPositions[i - 1] - m_segmentPositions[i];
                position.z = 0.0f;
                direction.z = 0.0f;

                segments[i - 1].position = position;
                segments[i - 1].rotation = Quaternion.LookRotation(Vector3.forward, direction);
            }
        }
    }
}
