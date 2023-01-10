using UnityEngine;

public class ConnectedSegments : MonoBehaviour
{
    [SerializeField] private Transform[] segments;
    [SerializeField] private float       segmentTargetDistance;
    [SerializeField] private float       smoothSpeed;
    [SerializeField] private float       wiggleSpeed;
    [SerializeField] private float       wiggleAngle;

    private Vector3[] m_segmentPositions;
    private Vector3[] m_segmentVelocities;

    public Transform[] GetSegments(bool includeSelf = false)
    {
        if (includeSelf)
        {
            Transform[] res = new Transform[segments.Length + 1];
            res[0] = transform;
            for (int i = 0; i < segments.Length; i++)
            {
                res[i + 1] = segments[i];
            }
            return res;
        }
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
                Vector3 targetDir = m_segmentPositions[i] - m_segmentPositions[i - 1];
                targetDir.z = 0.0f;
                targetDir.Normalize();
                float angle = (Mathf.Sin(Time.time * wiggleSpeed + i * 0.2f) * wiggleAngle * (1.0f - (i * 1.0f / (segments.Length - 1)))) * Mathf.Deg2Rad;
                float ca = Mathf.Cos(angle);
                float sa = Mathf.Sin(angle);
                targetDir = new Vector3(ca * targetDir.x - sa * targetDir.y, sa * targetDir.x + ca * targetDir.y, 0.0f);

                Vector3 targetPos = m_segmentPositions[i - 1] + targetDir * segmentTargetDistance;
                m_segmentPositions[i] = Vector3.SmoothDamp(m_segmentPositions[i], targetPos, ref m_segmentVelocities[i], 1.0f / smoothSpeed);

                Vector3 position  = m_segmentPositions[i];
                Vector3 direction = m_segmentPositions[i - 1] - m_segmentPositions[i];
                position.z = 0.0f;
                direction.z = 0.0f;

                segments[i - 1].position = position;
                segments[i - 1].rotation = Quaternion.LookRotation(Vector3.forward, direction);
            }
        }
    }
}
