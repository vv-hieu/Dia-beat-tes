using UnityEngine;

public class BurstVFX : MonoBehaviour
{
    [SerializeField] private GameObject onStart;
    [SerializeField] private GameObject onDestroy;

    private void Start()
    {
        if (onStart != null)
        {
            Instantiate(onStart, transform.position, transform.rotation, transform.parent);
        }
    }

    private void OnDestroy()
    {
        if (onDestroy != null)
        {
            Instantiate(onDestroy, transform.position, transform.rotation, transform.parent);
        }
    }
}
