using UnityEngine;

public class DestructibleTilemapInteraction : MonoBehaviour
{
    private Collider2D m_collider;

    private void Awake()
    {
        m_collider = GetComponent<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (m_collider != null)
        {
            if (collision.TryGetComponent(out DestructibleTilemap destructibleTilemap))
            {
                destructibleTilemap.BreakTilesInBound(m_collider.bounds);
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (m_collider != null)
        {
            if (collision.TryGetComponent(out DestructibleTilemap destructibleTilemap))
            {
                destructibleTilemap.BreakTilesInBound(m_collider.bounds);
            }
        }
    }
}
