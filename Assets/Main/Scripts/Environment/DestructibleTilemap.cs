using UnityEngine;
using UnityEngine.Tilemaps;

public class DestructibleTilemap : MonoBehaviour
{
    [Header("Visual Effects")]
    [SerializeField] SerializableDictionary<TileBase, GameObject> breakingEffects = new SerializableDictionary<TileBase, GameObject>();

    private Tilemap m_tilemap;
    
    public void BreakTile(float x, float y)
    {
        Vector3Int cell = m_tilemap.WorldToCell(new Vector3(x, y, 0.0f));
        TileBase tile = m_tilemap.GetTile(cell);
        if (tile != null)
        {
            if (breakingEffects.TryGetValue(tile, out GameObject fx))
            {
                Vector3 cellCenter = m_tilemap.GetCellCenterWorld(cell);
                Instantiate(fx, cellCenter, Quaternion.identity, transform);
            }
            m_tilemap.SetTile(cell, null);
        }
    }

    public void BreakTilesInBound(Bounds bounds)
    {
        Vector3Int cellMin = m_tilemap.WorldToCell(bounds.min);
        Vector3Int cellMax = m_tilemap.WorldToCell(bounds.max);
        if (cellMin.x > cellMax.x)
        {
            int temp = cellMin.x;
            cellMin.x = cellMax.x;
            cellMax.x = temp;
        }
        if (cellMin.y > cellMax.y)
        {
            int temp = cellMin.y;
            cellMin.y = cellMax.y;
            cellMax.y = temp;
        }

        for (int x = cellMin.x; x <= cellMax.x; ++x)
        {
            for (int y = cellMin.y; y <= cellMax.y; ++y)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                TileBase tile = m_tilemap.GetTile(cell);
                if (tile != null)
                {
                    if (breakingEffects.TryGetValue(tile, out GameObject fx))
                    {
                        Vector3 cellCenter = m_tilemap.GetCellCenterWorld(cell);
                        Instantiate(fx, cellCenter, Quaternion.identity, transform);
                    }
                    m_tilemap.SetTile(cell, null);
                }
            }
        }
    }

    private void Start()
    {
        m_tilemap = GetComponent<Tilemap>();
    }
}
