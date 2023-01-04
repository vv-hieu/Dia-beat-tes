using UnityEngine;

[ExecuteInEditMode]
public class Attach : MonoBehaviour
{
    [SerializeField] private Transform attach;
    [SerializeField] private Vector2   offset;
    [SerializeField] private bool      destroyAttach = false;
    private void Update()
    {
        if (attach != null)
        {
            attach.position = new Vector3(transform.position.x + offset.x, transform.position.y + offset.y, 0.0f);
        }
    }

    private void OnDestroy()
    {
        if (destroyAttach && attach != null)
        {
            Destroy(attach.gameObject);
        }
    }
}
