using UnityEngine;

public class BurstVFX : MonoBehaviour
{
    [SerializeField] private GameObject onStart;
    [SerializeField] private GameObject onDestroy;

    private ParticleSystem m_particle;
    private bool           m_stopped = false;

    private void Awake()
    {
        m_particle = GetComponent<ParticleSystem>();
    }

    private void Start()
    {
        if (onStart != null)
        {
            Instantiate(onStart, transform.position, transform.rotation, transform.parent);
        }
    }

    private void Update()
    {
        if (m_particle != null && !m_particle.isEmitting)
        {
            if (onDestroy != null && !m_stopped)
            {
                Instantiate(onDestroy, transform.position, transform.rotation, transform.parent);
                m_stopped = true;
            }
        }
    }

    private void OnDestroy()
    {
        if (onDestroy != null && !m_stopped)
        {
            Instantiate(onDestroy, transform.position, transform.rotation, transform.parent);
            m_stopped = true;
        }
    }
}
