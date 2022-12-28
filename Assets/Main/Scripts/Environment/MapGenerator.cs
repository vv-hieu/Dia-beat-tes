using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.AI;

public class MapGenerator : MonoBehaviour
{
    [Header("Map Configuration")]
    [SerializeField]                    private Optional<int> seed                     = new Optional<int>();
    [SerializeField]                    private Vector2Int    mapSize                  = new Vector2Int(50, 50);
    [SerializeField]                    private int           padding                  = 30;
    [SerializeField]                    private float         falloffStrength          = 1.0f;
    [SerializeField][Range(0.0f, 1.0f)] private float         groundDensity            = 0.8f;
    [SerializeField]                    private float         groundNoiseScale         = 0.1f;
    [SerializeField][Range(0.0f, 1.0f)] private float         groundVariantDensity     = 0.1f;
    [SerializeField]                    private float         pathNoiseScale           = 0.1f;
    [SerializeField][Range(0.0f, 1.0f)] private float         pathDensity              = 0.1f;
    [SerializeField]                    private float         detailNoiseScale         = 0.1f;
    [SerializeField][Range(0.0f, 1.0f)] private float         detailDensity            = 0.3f;
    [SerializeField]                    private IslandFilter  islandFilter;

    [Header("Component References")]
    [SerializeField] private Tilemap        groundTilemap;
    [SerializeField] private Tilemap        wallTilemap;
    [SerializeField] private Tilemap        detailTilemap;
    [SerializeField] private NavMeshSurface navigationMesh;

    [Header("Ground")]
    [SerializeField] private Tile ground;
    [SerializeField] private Tile groundVariant;

    [Header("Path")]
    [SerializeField] private Tile path;
    [SerializeField] private Tile pathLeft;
    [SerializeField] private Tile pathRight;
    [SerializeField] private Tile pathBottom;
    [SerializeField] private Tile pathTop;
    [SerializeField] private Tile pathOuterBottomLeft;
    [SerializeField] private Tile pathOuterBottomRight;
    [SerializeField] private Tile pathOuterTopLeft;
    [SerializeField] private Tile pathOuterTopRight;
    [SerializeField] private Tile pathInnerBottomLeft;
    [SerializeField] private Tile pathInnerBottomRight;
    [SerializeField] private Tile pathInnerTopLeft;
    [SerializeField] private Tile pathInnerTopRight;

    [Header("Bridge")]
    [SerializeField] private Tile bridgeHorizontalBottom;
    [SerializeField] private Tile bridgeHorizontalTop;
    [SerializeField] private Tile bridgeHorizontalBottomLeft;
    [SerializeField] private Tile bridgeHorizontalBottomRight;
    [SerializeField] private Tile bridgeHorizontalTopLeft;
    [SerializeField] private Tile bridgeHorizontalTopRight;
    [SerializeField] private Tile bridgeVerticalLeft;
    [SerializeField] private Tile bridgeVerticalRight;
    [SerializeField] private Tile bridgeVerticalBottomLeft;
    [SerializeField] private Tile bridgeVerticalBottomRight;
    [SerializeField] private Tile bridgeVerticalTopLeft;
    [SerializeField] private Tile bridgeVerticalTopRight;
    [SerializeField] private Tile bridgeSupport;
    [SerializeField] private Tile bridgeSupportLeft;
    [SerializeField] private Tile bridgeSupportRight;
    [SerializeField] private Tile bridgeSupportGroundedLeft;
    [SerializeField] private Tile bridgeSupportGroundedRight;

    [Header("Wall")]
    [SerializeField] private Tile wall;
    [SerializeField] private Tile wallLeft;
    [SerializeField] private Tile wallRight;
    [SerializeField] private Tile wallBottom;
    [SerializeField] private Tile wallTop;
    [SerializeField] private Tile wallOuterBottomLeft;
    [SerializeField] private Tile wallOuterBottomRight;
    [SerializeField] private Tile wallOuterTopLeft;
    [SerializeField] private Tile wallOuterTopRight;
    [SerializeField] private Tile wallInnerBottomLeft;
    [SerializeField] private Tile wallInnerBottomRight;
    [SerializeField] private Tile wallInnerTopLeft;
    [SerializeField] private Tile wallInnerTopRight;

    [Header("Detail")]
    [SerializeField] private Tile detail;

    private System.Random    m_rng;
    private Vector2          m_whiteNoiseOffset;
    private Vector2          m_perlinNoiseOffset;
    private int[,]           m_map;
    private int[,]           m_islandIndex;
    private List<Island>     m_islands = new List<Island>();
    private int              m_maxIslandIndex;
    private Graph            m_bridgeLayout = new Graph();

    private static uint GROUND_NOISE_ID = 0;
    private static uint PATH_NOISE_ID   = 1;
    private static uint DETAIL_NOISE_ID = 2;

    public List<Island> islands
    {
        get
        {
            return m_islands;
        }
    }

    public Island mainIsland
    {
        get
        {
            if (m_islands.Count == 0)
            {
                return null;
            }
            return m_islands[m_maxIslandIndex];
        }
    }

    public Map map { get; private set; }

    private void Start()
    {
        // Initialize random number generator
        p_InitializeRng();

        // Generate map
        p_GenerateMap();

        // Set tiles base on generated map
        p_SetTiles();

        // Bake nav mesh
        navigationMesh.BuildNavMesh();
    }

    private void OnDrawGizmosSelected()
    {
        BoundsInt mapBound = new BoundsInt(Vector3Int.FloorToInt(transform.position) - new Vector3Int(mapSize.x, mapSize.y, 0), new Vector3Int(2 * mapSize.x + 1, 2 * mapSize.y + 1));
        p_DrawBounds(mapBound, Color.white, -1.0f);

        BoundsInt padBound = new BoundsInt(Vector3Int.FloorToInt(transform.position) - new Vector3Int(mapSize.x + padding, mapSize.y + padding, 0), new Vector3Int(2 * (mapSize.x + padding) + 1, 2 * (mapSize.y + padding) + 1));
        p_DrawBounds(padBound, Color.black, -1.0f);

        int islandIndex = 0;
        foreach (Island island in m_islands)
        {
            p_DrawBounds(island.bounds, Color.green, -1.0f);
            p_DrawString("ID: " + islandIndex++, island.center.x, island.center.y, Color.black, -3.0f);
        }

        p_DrawGraph(m_bridgeLayout, Color.blue, -2.0f);
    }

    private bool p_IsWall(int x, int y)
    {
        if (x < -mapSize.x)
        {
            return true;
        }
        if (x > mapSize.x)
        {
            return true;
        }
        if (y < -mapSize.y)
        {
            return true;
        }
        if (y > mapSize.y)
        {
            return true;
        }
        return m_map[x + mapSize.x, y + mapSize.y] == 0;
    }

    private bool p_IsGround(int x, int y)
    {
        if (x < -mapSize.x)
        {
            return false;
        }
        if (x > mapSize.x)
        {
            return false;
        }
        if (y < -mapSize.y)
        {
            return false;
        }
        if (y > mapSize.y)
        {
            return false;
        }
        return m_map[x + mapSize.x, y + mapSize.y] == 1;
    }

    private bool p_IsPath(int x, int y)
    {
        if (x < -mapSize.x)
        {
            return false;
        }
        if (x > mapSize.x)
        {
            return false;
        }
        if (y < -mapSize.y)
        {
            return false;
        }
        if (y > mapSize.y)
        {
            return false;
        }
        return m_map[x + mapSize.x, y + mapSize.y] == 2;
    }

    private bool p_IsBridgeHorizontal(int x, int y)
    {
        if (x < -mapSize.x)
        {
            return false;
        }
        if (x > mapSize.x)
        {
            return false;
        }
        if (y < -mapSize.y)
        {
            return false;
        }
        if (y > mapSize.y)
        {
            return false;
        }
        return m_map[x + mapSize.x, y + mapSize.y] == 3;
    }

    private bool p_IsBridgeVertical(int x, int y)
    {
        if (x < -mapSize.x)
        {
            return false;
        }
        if (x > mapSize.x)
        {
            return false;
        }
        if (y < -mapSize.y)
        {
            return false;
        }
        if (y > mapSize.y)
        {
            return false;
        }
        return m_map[x + mapSize.x, y + mapSize.y] == 4;
    }

    private bool p_IsBridge(int x, int y)
    {
        if (x < -mapSize.x)
        {
            return false;
        }
        if (x > mapSize.x)
        {
            return false;
        }
        if (y < -mapSize.y)
        {
            return false;
        }
        if (y > mapSize.y)
        {
            return false;
        }
        return m_map[x + mapSize.x, y + mapSize.y] == 3 || m_map[x + mapSize.x, y + mapSize.y] == 4;
    }

    private bool p_IsWalkable(int x, int y)
    {
        if (x < -mapSize.x)
        {
            return false;
        }
        if (x > mapSize.x)
        {
            return false;
        }
        if (y < -mapSize.y)
        {
            return false;
        }
        if (y > mapSize.y)
        {
            return false;
        }
        return m_map[x + mapSize.x, y + mapSize.y] > 0;
    }

    private void p_InitializeRng()
    {
        if (seed.Enabled)
        {
            m_rng = new System.Random(seed.Value);
            Debug.Log("[Map Generator]: Initialized with set seed: " + seed.Value);
        }
        else
        {
            m_rng = new System.Random();
            Debug.Log("[Map Generator]: Initialized with a random seed");
        }

        m_whiteNoiseOffset = new Vector2(
            (float)m_rng.NextDouble(),
            (float)m_rng.NextDouble()
        ) * 0.2f;

        m_perlinNoiseOffset = new Vector2(
            (float)m_rng.NextDouble(),
            (float)m_rng.NextDouble()
        ) * 32.0f;
    }

    private void p_GenerateMap()
    {
        // Initialize map
        m_map = new int[2 * mapSize.x + 1, 2 * mapSize.y + 1];
        for (int x = -mapSize.x; x <= mapSize.x; ++x)
        {
            for (int y = -mapSize.y; y <= mapSize.y; ++y)
            {
                float value = p_PerlinNoise(x * groundNoiseScale, y * groundNoiseScale, GROUND_NOISE_ID) - p_Falloff(x, y);
                if (value >= 1.0f - groundDensity)
                {
                    m_map[x + mapSize.x, y + mapSize.y] = p_RidgedNoise(x * pathNoiseScale, y * pathNoiseScale, PATH_NOISE_ID) >= (1.0f - pathDensity) ? 2 : 1;
                }
                else
                {
                    m_map[x + mapSize.x, y + mapSize.y] = 0;
                }
            }
        }

        // Polishing map
        bool polishingMap;

        // Polishing map - Remove narrow walls
        polishingMap = true;
        while (polishingMap)
        {
            polishingMap = false;
            for (int x = -mapSize.x; x <= mapSize.x; ++x)
            {
                for (int y = -mapSize.y; y <= mapSize.y; ++y)
                {
                    if (!p_IsWalkable(x, y))
                    {
                        if (
                            (p_IsWalkable(x - 1, y - 1) && p_IsWalkable(x + 1, y + 1)) ||
                            (p_IsWalkable(x - 1, y + 1) && p_IsWalkable(x + 1, y - 1)) ||
                            (p_IsWalkable(x - 1, y    ) && p_IsWalkable(x + 1, y    )) ||
                            (p_IsWalkable(x    , y - 1) && p_IsWalkable(x    , y + 1)) ||
                            (p_IsWalkable(x - 1, y    ) && p_IsWalkable(x + 1, y - 1)) && !p_IsWalkable(x, y - 1) ||
                            (p_IsWalkable(x - 1, y    ) && p_IsWalkable(x + 1, y + 1)) && !p_IsWalkable(x, y + 1) ||
                            (p_IsWalkable(x + 1, y    ) && p_IsWalkable(x - 1, y - 1)) && !p_IsWalkable(x, y - 1) ||
                            (p_IsWalkable(x + 1, y    ) && p_IsWalkable(x - 1, y + 1)) && !p_IsWalkable(x, y + 1) ||
                            (p_IsWalkable(x    , y - 1) && p_IsWalkable(x - 1, y + 1)) && !p_IsWalkable(x - 1, y) ||
                            (p_IsWalkable(x    , y - 1) && p_IsWalkable(x + 1, y + 1)) && !p_IsWalkable(x + 1, y) ||
                            (p_IsWalkable(x    , y + 1) && p_IsWalkable(x - 1, y - 1)) && !p_IsWalkable(x - 1, y) ||
                            (p_IsWalkable(x    , y + 1) && p_IsWalkable(x + 1, y - 1)) && !p_IsWalkable(x + 1, y) 
                        )
                        {
                            m_map[x + mapSize.x, y + mapSize.y] = 1;
                            polishingMap = true;
                        }
                    }
                }
            }
        }

        // Polishing map - Remove paths near walls
        for (int x = -mapSize.x; x <= mapSize.x; ++x)
        {
            for (int y = -mapSize.y; y <= mapSize.y; ++y)
            {
                if (p_IsPath(x, y))
                {
                    if (
                        !p_IsWalkable(x - 1, y) ||
                        !p_IsWalkable(x + 1, y) ||
                        !p_IsWalkable(x, y - 1) ||
                        !p_IsWalkable(x, y + 1)
                    )
                    {
                        m_map[x + mapSize.x, y + mapSize.y] = 1;
                    }
                }
            }
        }

        // Polishing map - Remove narrow paths
        polishingMap = true;
        while (polishingMap)
        {
            polishingMap = false;
            for (int x = -mapSize.x; x <= mapSize.x; ++x)
            {
                for (int y = -mapSize.y; y <= mapSize.y; ++y)
                {
                    if (p_IsPath(x, y))
                    {
                        if (
                            (p_IsGround(x - 1, y - 1) && p_IsGround(x + 1, y + 1)) ||
                            (p_IsGround(x - 1, y + 1) && p_IsGround(x + 1, y - 1)) ||
                            (p_IsGround(x - 1, y) && p_IsGround(x + 1, y)) ||
                            (p_IsGround(x, y - 1) && p_IsGround(x, y + 1))
                        )
                        {
                            m_map[x + mapSize.x, y + mapSize.y] = 1;
                            polishingMap = true;
                        }
                    }
                }
            }
        }

        // Island filter
        int maxIslandSize = 0;

        bool[,] scanned = new bool[2 * mapSize.x + 1, 2 * mapSize.y + 1];
        for (int x = -mapSize.x; x <= mapSize.x; ++x)
        {
            for (int y = -mapSize.y; y <= mapSize.y; ++y)
            {
                scanned[x + mapSize.x, y + mapSize.y] = false;
            }
        }

        for (int x = -mapSize.x; x <= mapSize.x; ++x)
        {
            for (int y = -mapSize.y; y <= mapSize.y; ++y)
            {
                if (p_IsWalkable(x, y) && !scanned[x + mapSize.x, y + mapSize.y])
                {
                    List<KeyValuePair<int, int>> connectedTiles = new List<KeyValuePair<int, int>>();
                    connectedTiles.Add(new KeyValuePair<int, int>(x, y));
                    int i = 0;
                    while (i < connectedTiles.Count)
                    {
                        int x2 = connectedTiles[i].Key;
                        int y2 = connectedTiles[i].Value;

                        KeyValuePair<int, int> tile1 = new KeyValuePair<int, int>(x2 - 1, y2);
                        KeyValuePair<int, int> tile2 = new KeyValuePair<int, int>(x2 + 1, y2);
                        KeyValuePair<int, int> tile3 = new KeyValuePair<int, int>(x2, y2 - 1);
                        KeyValuePair<int, int> tile4 = new KeyValuePair<int, int>(x2, y2 + 1);

                        if (p_IsWalkable(tile1.Key, tile1.Value) && !connectedTiles.Contains(tile1))
                        {
                            connectedTiles.Add(tile1);
                        }
                        if (p_IsWalkable(tile2.Key, tile2.Value) && !connectedTiles.Contains(tile2))
                        {
                            connectedTiles.Add(tile2);
                        }
                        if (p_IsWalkable(tile3.Key, tile3.Value) && !connectedTiles.Contains(tile3))
                        {
                            connectedTiles.Add(tile3);
                        }
                        if (p_IsWalkable(tile4.Key, tile4.Value) && !connectedTiles.Contains(tile4))
                        {
                            connectedTiles.Add(tile4);
                        }
                        ++i;
                    }

                    List<Vector2Int> island = new List<Vector2Int>();
                    foreach (KeyValuePair<int, int> tile in connectedTiles)
                    {
                        scanned[tile.Key + mapSize.x, tile.Value + mapSize.y] = true;
                        island.Add(new Vector2Int(tile.Key, tile.Value));
                    }

                    if (connectedTiles.Count > maxIslandSize)
                    {
                        maxIslandSize = connectedTiles.Count;
                        m_maxIslandIndex = m_islands.Count;
                    }

                    m_islands.Add(new Island(island));
                }
            }
        }

        if (m_islands.Count > 0)
        {
            int oldIslandCount = m_islands.Count;
            Debug.Log("[Map generator]: Genrated " + oldIslandCount + " island(s)");

            List<Island> removedIslands = new List<Island>();
            List<Island> newIslands = new List<Island>();
            int threshold = 0;
            switch (islandFilter.mode)
            {
                case IslandFilter.Mode.MaxOnly:
                    threshold = maxIslandSize;
                    break;
                case IslandFilter.Mode.Threshold:
                    threshold = islandFilter.threshold;
                    break;
                default:
                    break;
            }
            foreach (Island island in m_islands)
            {
                if (island.tiles.Count >= threshold)
                {
                    newIslands.Add(island);
                }
                else
                {
                    removedIslands.Add(island);
                }
            }
            m_islands = newIslands;

            foreach (Island removedIsland in removedIslands)
            {
                foreach (Vector2Int tile in removedIsland.tiles)
                {
                    m_map[tile.x + mapSize.x, tile.y + mapSize.y] = 0;
                }
            }

            Debug.Log("[Map generator]: Removed " + (oldIslandCount - m_islands.Count) + " island(s), " + m_islands.Count + " island(s) remaining");
        }
        m_islandIndex = new int[2 * mapSize.x + 1, 2 * mapSize.y + 1];
        for (int x = -mapSize.x; x <= mapSize.x; ++x)
        {
            for (int y = -mapSize.y; y <= mapSize.y; ++y)
            {
                m_islandIndex[x + mapSize.x, y + mapSize.y] = -1;
            }
        }
        int islandIndex = 0;
        foreach (Island island in m_islands)
        {
            foreach (Vector2Int tile in island.tiles)
            {
                m_islandIndex[tile.x + mapSize.x, tile.y + mapSize.y] = islandIndex;
            }
            ++islandIndex;
        }

        // Generate bridges
        Graph grid = new Graph();
        SortedSet<int> gridX = new SortedSet<int>();
        SortedSet<int> gridY = new SortedSet<int>();
        foreach (Island island in m_islands)
        {
            gridX.Add(island.center.x);
            gridY.Add(island.center.y);
        }
        bool beginY = true;
        int prevY = 0;
        foreach (int y in gridY)
        {
            if (beginY)
            {
                prevY = y;
                beginY = false;
            }
            else {
                foreach (int x in gridX)
                {
                    Vector2Int v0 = new Vector2Int(x, prevY);
                    Vector2Int v1 = new Vector2Int(x, y);
                    grid.AddEdge(v0, v1);
                    for (int y2 = prevY; y2 <= y; ++y2)
                    {
                        if (p_IsWalkable(x, y2 - 1) && p_IsWalkable(x - 1, y2 - 1) && (p_IsWall(x, y2) || p_IsWall(x - 1, y2)))
                        {
                            Vector2Int splitPoint = new Vector2Int(x, y2);
                            grid.SplitEdge(v0, v1, splitPoint);
                            v0 = splitPoint;
                            continue;
                        }
                        if (p_IsWalkable(x, y2) && p_IsWalkable(x - 1, y2) && (p_IsWall(x, y2 - 1) || p_IsWall(x - 1, y2 - 1)))
                        {
                            Vector2Int splitPoint = new Vector2Int(x, y2);
                            grid.SplitEdge(v0, v1, splitPoint);
                            v0 = splitPoint;
                            continue;
                        }
                    }
                }
                prevY = y;
            }
        }
        bool beginX = true;
        int prevX = 0;
        foreach (int x in gridX)
        {
            if (beginX)
            {
                prevX = x;
                beginX = false;
            }
            else
            {
                foreach (int y in gridY)
                {
                    Vector2Int v0 = new Vector2Int(prevX, y);
                    Vector2Int v1 = new Vector2Int(x, y);
                    grid.AddEdge(v0, v1);
                    for (int x2 = prevX; x2 <= x; ++x2)
                    {
                        if (p_IsWalkable(x2 - 1, y) && p_IsWalkable(x2 - 1, y - 1) && (p_IsWall(x2, y) || p_IsWall(x2, y - 1)))
                        {
                            Vector2Int splitPoint = new Vector2Int(x2, y);
                            grid.SplitEdge(v0, v1, splitPoint);
                            v0 = splitPoint;
                            continue;
                        }
                        if (p_IsWalkable(x2, y) && p_IsWalkable(x2, y - 1) && (p_IsWall(x2 - 1, y) || p_IsWall(x2 - 1, y - 1)))
                        {
                            Vector2Int splitPoint = new Vector2Int(x2, y);
                            grid.SplitEdge(v0, v1, splitPoint);
                            v0 = splitPoint;
                            continue;
                        }
                    }
                }
                prevX = x;
            }
        }
        List<KeyValuePair<Vector2Int, Vector2Int>> removedEdges = new List<KeyValuePair<Vector2Int, Vector2Int>>();
        foreach (KeyValuePair<Vector2Int, Vector2Int> edge in grid.edges)
        {
            int v0IslandIndex = (
                p_IsWalkable(edge.Key.x, edge.Key.y) ? m_islandIndex[edge.Key.x + mapSize.x, edge.Key.y + mapSize.y] :
                p_IsWalkable(edge.Key.x - 1, edge.Key.y) ? m_islandIndex[edge.Key.x - 1 + mapSize.x, edge.Key.y + mapSize.y] :
                p_IsWalkable(edge.Key.x, edge.Key.y - 1) ? m_islandIndex[edge.Key.x + mapSize.x, edge.Key.y - 1 + mapSize.y] :
                p_IsWalkable(edge.Key.x - 1, edge.Key.y - 1) ? m_islandIndex[edge.Key.x - 1 + mapSize.x, edge.Key.y - 1 + mapSize.y] : -1
            );

            int v1IslandIndex = (
                p_IsWalkable(edge.Value.x, edge.Value.y) ? m_islandIndex[edge.Value.x + mapSize.x, edge.Value.y + mapSize.y] :
                p_IsWalkable(edge.Value.x - 1, edge.Value.y) ? m_islandIndex[edge.Value.x - 1 + mapSize.x, edge.Value.y + mapSize.y] :
                p_IsWalkable(edge.Value.x, edge.Value.y - 1) ? m_islandIndex[edge.Value.x + mapSize.x, edge.Value.y - 1 + mapSize.y] :
                p_IsWalkable(edge.Value.x - 1, edge.Value.y - 1) ? m_islandIndex[edge.Value.x - 1 + mapSize.x, edge.Value.y - 1 + mapSize.y] : -1
            );

            if (v0IslandIndex != -1 && v1IslandIndex != -1 && v0IslandIndex == v1IslandIndex)
            {
                removedEdges.Add(edge);
            }
        }
        foreach (KeyValuePair<Vector2Int, Vector2Int> edge in removedEdges)
        {
            grid.RemoveEdge(edge.Key, edge.Value);
        }
        List<KeyValuePair<Vector2Int, Vector2Int>> edgesToAdd = new List<KeyValuePair<Vector2Int, Vector2Int>>();
        foreach (Vector2Int vertex in grid.vertices)
        {
            int vIslandIndex = (
                p_IsWalkable(vertex.x    , vertex.y    ) ? m_islandIndex[vertex.x     + mapSize.x, vertex.y     + mapSize.y] :
                p_IsWalkable(vertex.x - 1, vertex.y    ) ? m_islandIndex[vertex.x - 1 + mapSize.x, vertex.y     + mapSize.y] :
                p_IsWalkable(vertex.x    , vertex.y - 1) ? m_islandIndex[vertex.x     + mapSize.x, vertex.y - 1 + mapSize.y] :
                p_IsWalkable(vertex.x - 1, vertex.y - 1) ? m_islandIndex[vertex.x - 1 + mapSize.x, vertex.y - 1 + mapSize.y] : -1
            );
            if (vIslandIndex >= 0)
            {
                Vector2Int islandCenter = m_islands[vIslandIndex].center;
                edgesToAdd.Add(new KeyValuePair<Vector2Int, Vector2Int>(islandCenter, vertex));
            }
        }
        Graph bridgeGraph = new Graph();
        foreach (KeyValuePair<Vector2Int, Vector2Int> edgeToAdd in edgesToAdd)
        {
            bridgeGraph.AddEdge(edgeToAdd.Key, edgeToAdd.Value);
            grid.AddEdge(edgeToAdd.Key, edgeToAdd.Value);
        }
        while (true)
        {
            Vector2Int v0 = Vector2Int.zero;
            Vector2Int v1 = Vector2Int.zero;
            int minLength = int.MaxValue;
            bool found = false;
            foreach (KeyValuePair<Vector2Int, Vector2Int> edge in grid.edges)
            {
                if (!bridgeGraph.IsConnected(edge.Key, edge.Value) && (bridgeGraph.vertices.Contains(edge.Key) || bridgeGraph.vertices.Contains(edge.Value)))
                {
                    int length = Mathf.Abs(edge.Key.x - edge.Value.x) + Mathf.Abs(edge.Key.y - edge.Value.y);
                    if (length < minLength)
                    {
                        minLength = length;
                        v0 = edge.Key;
                        v1 = edge.Value;
                        found = true;
                    }
                }
            }
            if (!found)
            {
                break;
            }
            bridgeGraph.AddEdge(v0, v1);
        }
        foreach (Island island in m_islands)
        {
            bridgeGraph.RemoveVertex(island.center);
        }
        Graph newBridgeGraph = new Graph();
        foreach (Graph component in bridgeGraph.ConnectedComponents())
        {
            HashSet<int> componentIslandIndices = new HashSet<int>();
            foreach (Vector2Int componentVertex in component.vertices)
            {
                int vIslandIndex = (
                    p_IsWalkable(componentVertex.x    , componentVertex.    y) ? m_islandIndex[componentVertex.x     + mapSize.x, componentVertex.y     + mapSize.y] :
                    p_IsWalkable(componentVertex.x - 1, componentVertex.y    ) ? m_islandIndex[componentVertex.x - 1 + mapSize.x, componentVertex.y     + mapSize.y] :
                    p_IsWalkable(componentVertex.x    , componentVertex.y - 1) ? m_islandIndex[componentVertex.x     + mapSize.x, componentVertex.y - 1 + mapSize.y] :
                    p_IsWalkable(componentVertex.x - 1, componentVertex.y - 1) ? m_islandIndex[componentVertex.x - 1 + mapSize.x, componentVertex.y - 1 + mapSize.y] : -1            
                );
                if (vIslandIndex >= 0)
                {
                    componentIslandIndices.Add(vIslandIndex);
                }
            }
            if (componentIslandIndices.Count > 1)
            {
                newBridgeGraph.AddSubgraph(component);
            }
        }
        while (true)
        {
            bool valid = true;
            Vector2Int vertexToRemove = Vector2Int.zero;
            foreach (Vector2Int vertex in newBridgeGraph.vertices)
            {
                int edgeCount = 0;
                foreach (KeyValuePair<Vector2Int, Vector2Int> edge in newBridgeGraph.edges)
                {
                    if (edge.Key == vertex || edge.Value == vertex)
                    {
                        ++edgeCount;
                    }
                    if (edgeCount >= 2)
                    {
                        break;
                    }
                }
                if (edgeCount <= 1)
                {
                    int vIslandIndex = (
                        p_IsWalkable(vertex.x    , vertex.y    ) ? m_islandIndex[vertex.x     + mapSize.x, vertex.y     + mapSize.y] :
                        p_IsWalkable(vertex.x - 1, vertex.y    ) ? m_islandIndex[vertex.x - 1 + mapSize.x, vertex.y     + mapSize.y] :
                        p_IsWalkable(vertex.x    , vertex.y - 1) ? m_islandIndex[vertex.x     + mapSize.x, vertex.y - 1 + mapSize.y] :
                        p_IsWalkable(vertex.x - 1, vertex.y - 1) ? m_islandIndex[vertex.x - 1 + mapSize.x, vertex.y - 1 + mapSize.y] : -1
                    );
                    if (vIslandIndex < 0)
                    {
                        valid = false;
                        vertexToRemove = vertex;
                        break;
                    }
                }
            }
            if (valid)
            {
                break;
            }
            else
            {
                newBridgeGraph.RemoveVertex(vertexToRemove);
            }
        }
        while (true)
        {
            bool valid = true;
            Vector2Int vertexToRemove = Vector2Int.zero;
            Vector2Int v1 = Vector2Int.zero;
            Vector2Int v2 = Vector2Int.zero;
            foreach (Vector2Int vertex in newBridgeGraph.vertices)
            {
                List<KeyValuePair<Vector2Int, Vector2Int>> edges = new List<KeyValuePair<Vector2Int, Vector2Int>>();
                foreach (KeyValuePair<Vector2Int, Vector2Int> edge in newBridgeGraph.edges)
                {
                    if (edge.Key == vertex || edge.Value == vertex)
                    {
                        edges.Add(edge);
                    }
                }
                if (edges.Count == 2)
                {
                    v1 = edges[0].Key;
                    if (edges[0].Key == vertex)
                    {
                        v1 = edges[0].Value;
                    }
                    v2 = edges[1].Key;
                    if (edges[1].Key == vertex)
                    {
                        v2 = edges[1].Value;
                    }
                }
                if ((v1.x == vertex.x && v2.x == vertex.x) || (v1.y == vertex.y && v2.y == vertex.y))
                {
                    vertexToRemove = vertex;
                    valid = false;
                    break;
                }
            }
            if (valid)
            {
                break;
            }
            else
            {
                newBridgeGraph.AddEdge(v1, v2);
                newBridgeGraph.RemoveVertex(vertexToRemove);
            }
        }
        m_bridgeLayout.AddSubgraph(newBridgeGraph);

        foreach (KeyValuePair<Vector2Int, Vector2Int> edge in m_bridgeLayout.edges)
        {
            Vector2Int v0 = edge.Key;
            Vector2Int v1 = edge.Value;
            if (v0.y == v1.y)
            {
                if (v0.x > v1.x)
                {
                    Vector2Int temp = v0;
                    v0 = v1;
                    v1 = temp;
                }
                int v0IslandIndex = (
                    p_IsWalkable(v0.x    , v0.y    ) ? m_islandIndex[v0.x     + mapSize.x, v0.y     + mapSize.y] :
                    p_IsWalkable(v0.x - 1, v0.y    ) ? m_islandIndex[v0.x - 1 + mapSize.x, v0.y     + mapSize.y] :
                    p_IsWalkable(v0.x    , v0.y - 1) ? m_islandIndex[v0.x     + mapSize.x, v0.y - 1 + mapSize.y] :
                    p_IsWalkable(v0.x - 1, v0.y - 1) ? m_islandIndex[v0.x - 1 + mapSize.x, v0.y - 1 + mapSize.y] : -1
                );
                if (v0IslandIndex < 0)
                {
                    --v0.x;
                    v0.x = v0.x < -mapSize.x ? -mapSize.x : v0.x;
                }
                int v1IslandIndex = (
                    p_IsWalkable(v1.x    , v1.y    ) ? m_islandIndex[v1.x     + mapSize.x, v1.y     + mapSize.y] :
                    p_IsWalkable(v1.x - 1, v1.y    ) ? m_islandIndex[v1.x - 1 + mapSize.x, v1.y     + mapSize.y] :
                    p_IsWalkable(v1.x    , v1.y - 1) ? m_islandIndex[v1.x     + mapSize.x, v1.y - 1 + mapSize.y] :
                    p_IsWalkable(v1.x - 1, v1.y - 1) ? m_islandIndex[v1.x - 1 + mapSize.x, v1.y - 1 + mapSize.y] : -1
                );
                if (v1IslandIndex < 0)
                {
                    ++v1.x;
                    v1.x = v1.x > mapSize.x ? mapSize.x : v1.x;
                }
                p_GenerateBridge(v0.x, v0.y, BridgeDirection.Right, v1.x - v0.x);
            }
        }
        foreach (KeyValuePair<Vector2Int, Vector2Int> edge in m_bridgeLayout.edges)
        {
            Vector2Int v0 = edge.Key;
            Vector2Int v1 = edge.Value;
            if (v0.x == v1.x)
            {
                if (v0.y > v1.y)
                {
                    Vector2Int temp = v0;
                    v0 = v1;
                    v1 = temp;
                }
                int v0IslandIndex = (
                    p_IsWalkable(v0.x    , v0.y    ) ? m_islandIndex[v0.x     + mapSize.x, v0.y     + mapSize.y] :
                    p_IsWalkable(v0.x - 1, v0.y    ) ? m_islandIndex[v0.x - 1 + mapSize.x, v0.y     + mapSize.y] :
                    p_IsWalkable(v0.x    , v0.y - 1) ? m_islandIndex[v0.x     + mapSize.x, v0.y - 1 + mapSize.y] :
                    p_IsWalkable(v0.x - 1, v0.y - 1) ? m_islandIndex[v0.x - 1 + mapSize.x, v0.y - 1 + mapSize.y] : -1
                );
                if (v0IslandIndex < 0)
                {
                    --v0.y;
                    v0.y = v0.y < -mapSize.y ? -mapSize.y : v0.y;
                }
                int v1IslandIndex = (
                    p_IsWalkable(v1.x    , v1.y    ) ? m_islandIndex[v1.x     + mapSize.x, v1.y     + mapSize.y] :
                    p_IsWalkable(v1.x - 1, v1.y    ) ? m_islandIndex[v1.x - 1 + mapSize.x, v1.y     + mapSize.y] :
                    p_IsWalkable(v1.x    , v1.y - 1) ? m_islandIndex[v1.x     + mapSize.x, v1.y - 1 + mapSize.y] :
                    p_IsWalkable(v1.x - 1, v1.y - 1) ? m_islandIndex[v1.x - 1 + mapSize.x, v1.y - 1 + mapSize.y] : -1
                );
                if (v1IslandIndex < 0)
                {
                    ++v1.y;
                    v1.y = v1.y > mapSize.y ? mapSize.y : v1.y;
                }
                p_GenerateBridge(v0.x, v0.y, BridgeDirection.Top, v1.y - v0.y);
            }
        }

        for (int x = -mapSize.x; x <= mapSize.x; ++x)
        {
            for (int y = -mapSize.y; y <= mapSize.y; ++y)
            {
                if (p_IsWall(x, y) && p_IsBridgeHorizontal(x, y + 1) && p_IsWalkable(x, y - 1))
                {
                    m_map[x + mapSize.x, y + mapSize.y] = 1;
                }
            }
        }

        bool[,] ground = new bool[2 * mapSize.x + 1, 2 * mapSize.y + 1];
        for (int x = -mapSize.x; x <= mapSize.x; ++x)
        {
            for (int y = -mapSize.y; y <= mapSize.y; ++y)
            {
                ground[x + mapSize.x, y + mapSize.y] = (m_map[x + mapSize.x , y + mapSize.y] > 0);
            }
        }
        map = new Map(mapSize, ground);
    }

    private void p_GenerateBridge(int x, int y, BridgeDirection direction, int length)
    {
        Vector2Int t = (((int)direction & (int)BridgeDirection.Positive) != 0 ? 1 : -1) * (((int)direction & (int)BridgeDirection.Vertical) != 0 ? Vector2Int.up : Vector2Int.right);
        Vector2Int n = (((int)direction & (int)BridgeDirection.Vertical) == 0 ? Vector2Int.up : Vector2Int.right);
        int        b = ((int)direction & (int)BridgeDirection.Vertical) == 0 ? 3 : 4;

        for (int i = 0; i < length; ++i)
        {
            int x1 = x + i * t.x;
            int y1 = y + i * t.y;
            int x2 = x1 - n.x;
            int y2 = y1 - n.y;
            if (!p_IsBridge(x1, y1))
            {
                m_map[x1 + mapSize.x, y1 + mapSize.y] = b;
            }
            if (!p_IsBridge(x2, y2))
            {
                m_map[x2 + mapSize.x, y2 + mapSize.y] = b;
            }
        }
    }

    private void p_SetTiles()
    {
        for (int x = -(mapSize.x + padding); x <= (mapSize.x + padding); ++x)
        {
            for (int y = -(mapSize.y + padding); y <= (mapSize.y + padding); ++y)
            {
                if (x < -mapSize.x || x > mapSize.x || y < -mapSize.y || y > mapSize.y)
                {
                    // Set wall tile
                    Tile tileToSet = wall;
                    if (p_IsBridge(x, y + 1))
                    {
                        tileToSet = bridgeSupport;
                        if (p_IsGround(x - 1, y))
                        {
                            tileToSet = bridgeSupportGroundedLeft;
                        }
                        else if(p_IsGround(x + 1, y))
                        {
                            tileToSet = bridgeSupportGroundedRight;
                        }
                        else if (!p_IsBridge(x - 1, y + 1))
                        {
                            tileToSet = bridgeSupportLeft;
                        }
                        else if (!p_IsBridge(x + 1, y + 1))
                        {
                            tileToSet = bridgeSupportRight;
                        }
                    }
                    else if (p_IsGround(x - 1, y))
                    {
                        tileToSet = wallLeft;
                        if (p_IsGround(x, y - 1))
                        {
                            tileToSet = wallOuterBottomLeft;
                        }
                        else if (p_IsGround(x, y + 1))
                        {
                            tileToSet = wallOuterTopLeft;
                        }
                    }
                    else if (p_IsGround(x + 1, y))
                    {
                        tileToSet = wallRight;
                        if (p_IsGround(x, y - 1))
                        {
                            tileToSet = wallOuterBottomRight;
                        }
                        else if (p_IsGround(x, y + 1))
                        {
                            tileToSet = wallOuterTopRight;
                        }
                    }
                    else if (p_IsGround(x, y - 1))
                    {
                        tileToSet = wallBottom;
                    }
                    else if (p_IsGround(x, y + 1))
                    {
                        tileToSet = wallTop;
                    }
                    else if (p_IsGround(x - 1, y - 1))
                    {
                        tileToSet = wallInnerBottomLeft;
                    }
                    else if (p_IsGround(x - 1, y + 1))
                    {
                        tileToSet = wallInnerTopLeft;
                    }
                    else if (p_IsGround(x + 1, y - 1))
                    {
                        tileToSet = wallInnerBottomRight;
                    }
                    else if (p_IsGround(x + 1, y + 1))
                    {
                        tileToSet = wallInnerTopRight;
                    }
                    wallTilemap.SetTile(new Vector3Int(x, y, 0), tileToSet);
                    continue;
                }

                if (p_IsGround(x, y))
                {
                    // Set ground tile
                    Tile tileToSet = ground;
                    if (p_WhiteNoise(x, y, GROUND_NOISE_ID) <= groundVariantDensity)
                    {
                        tileToSet = groundVariant;
                    }
                    groundTilemap.SetTile(new Vector3Int(x, y, 0), tileToSet);

                    // Set detail tile
                    if (p_PerlinNoise(x * detailNoiseScale, y * detailNoiseScale    , DETAIL_NOISE_ID) <= detailDensity)
                    {
                        detailTilemap.SetTile(new Vector3Int(x, y, 0), detail);
                    }
                }
                else if (p_IsPath(x, y))
                {
                    // Set path tile
                    Tile tileToSet = path;
                    if (p_IsGround(x - 1, y))
                    {
                        tileToSet = pathLeft;
                        if (p_IsGround(x, y - 1))
                        {
                            tileToSet = pathOuterBottomLeft;
                        }
                        else if (p_IsGround(x, y + 1))
                        {
                            tileToSet = pathOuterTopLeft;
                        }
                    }
                    else if (p_IsGround(x + 1, y))
                    {
                        tileToSet = pathRight;
                        if (p_IsGround(x, y - 1))
                        {
                            tileToSet = pathOuterBottomRight;
                        }
                        else if (p_IsGround(x, y + 1))
                        {
                            tileToSet = pathOuterTopRight;
                        }
                    }
                    else if (p_IsGround(x, y - 1))
                    {
                        tileToSet = pathBottom;
                    }
                    else if (p_IsGround(x, y + 1))
                    {
                        tileToSet = pathTop;
                    }
                    else if (p_IsGround(x - 1, y - 1))
                    {
                        tileToSet = pathInnerBottomLeft;
                    }
                    else if (p_IsGround(x - 1, y + 1))
                    {
                        tileToSet = pathInnerTopLeft;
                    }
                    else if (p_IsGround(x + 1, y - 1))
                    {
                        tileToSet = pathInnerBottomRight;
                    }
                    else if (p_IsGround(x + 1, y + 1))
                    {
                        tileToSet = pathInnerTopRight;
                    }
                    groundTilemap.SetTile(new Vector3Int(x, y, 0), tileToSet);
                }
                else if (p_IsBridgeHorizontal(x, y))
                {
                    Tile tileToSet;
                    if (p_IsBridgeHorizontal(x, y + 1))
                    {
                        tileToSet = bridgeHorizontalBottom;
                        if (!p_IsBridgeHorizontal(x - 1, y))
                        {
                            tileToSet = bridgeHorizontalBottomLeft;
                        }
                        else if (!p_IsBridgeHorizontal(x + 1, y))
                        {
                            tileToSet = bridgeHorizontalBottomRight;
                        }
                    }
                    else
                    {
                        tileToSet = bridgeHorizontalTop;
                        if (!p_IsBridgeHorizontal(x - 1, y))
                        {
                            tileToSet = bridgeHorizontalTopLeft;
                        }
                        else if (!p_IsBridgeHorizontal(x + 1, y))
                        {
                            tileToSet = bridgeHorizontalTopRight;
                        }
                    }
                    groundTilemap.SetTile(new Vector3Int(x, y, 0), tileToSet);
                }
                else if (p_IsBridgeVertical(x, y))
                {
                    Tile tileToSet;
                    if (p_IsBridgeVertical(x + 1, y))
                    {
                        tileToSet = bridgeVerticalLeft;
                        if (!p_IsBridgeVertical(x, y - 1))
                        {
                            tileToSet = bridgeVerticalBottomLeft;
                        }
                        else if (!p_IsBridgeVertical(x, y + 1))
                        {
                            tileToSet = bridgeVerticalTopLeft;
                        }
                    }
                    else
                    {
                        tileToSet = bridgeVerticalRight;
                        if (!p_IsBridgeVertical(x, y - 1))
                        {
                            tileToSet = bridgeVerticalBottomRight;
                        }
                        else if (!p_IsBridgeVertical(x, y + 1))
                        {
                            tileToSet = bridgeVerticalTopRight;
                        }
                    }
                    groundTilemap.SetTile(new Vector3Int(x, y, 0), tileToSet);
                }
                else
                {
                    // Set wall tile
                    Tile tileToSet = wall;
                    if (p_IsBridge(x, y + 1))
                    {
                        tileToSet = bridgeSupport;
                        if (p_IsGround(x - 1, y))
                        {
                            tileToSet = bridgeSupportGroundedLeft;
                        }
                        else if (p_IsGround(x + 1, y))
                        {
                            tileToSet = bridgeSupportGroundedRight;
                        }
                        else if (!p_IsBridge(x - 1, y + 1))
                        {
                            tileToSet = bridgeSupportLeft;
                        }
                        else if (!p_IsBridge(x + 1, y + 1))
                        {
                            tileToSet = bridgeSupportRight;
                        }
                    }
                    else if (p_IsGround(x - 1, y))
                    {
                        tileToSet = wallLeft;
                        if (p_IsGround(x, y - 1))
                        {
                            tileToSet = wallOuterBottomLeft;
                        }
                        else if (p_IsGround(x, y + 1))
                        {
                            tileToSet = wallOuterTopLeft;
                        }
                    }
                    else if (p_IsGround(x + 1, y))
                    {
                        tileToSet = wallRight;
                        if (p_IsGround(x, y - 1))
                        {
                            tileToSet = wallOuterBottomRight;
                        }
                        else if (p_IsGround(x, y + 1))
                        {
                            tileToSet = wallOuterTopRight;
                        }
                    }
                    else if (p_IsGround(x, y - 1))
                    {
                        tileToSet = wallBottom;
                    }
                    else if (p_IsGround(x, y + 1))
                    {
                        tileToSet = wallTop;
                    }
                    else if (p_IsGround(x - 1, y - 1))
                    {
                        tileToSet = wallInnerBottomLeft;
                    }
                    else if (p_IsGround(x - 1, y + 1))
                    {
                        tileToSet = wallInnerTopLeft;
                    }
                    else if (p_IsGround(x + 1, y - 1))
                    {
                        tileToSet = wallInnerBottomRight;
                    }
                    else if (p_IsGround(x + 1, y + 1))
                    {
                        tileToSet = wallInnerTopRight;
                    }
                    wallTilemap.SetTile(new Vector3Int(x, y, 0), tileToSet);
                }
            }
        }
    }

    private float p_Falloff(int x, int y)
    {
        float u = Mathf.Abs((float)x / mapSize.x);
        float v = Mathf.Abs((float)y / mapSize.y);
        return Mathf.Pow(u * u + v * v, falloffStrength * 0.5f);
    }

    private float p_WhiteNoise(float x, float y, uint i = 0)
    {
        Vector2 pos = new Vector2(x / mapSize.x, y / mapSize.y) + m_whiteNoiseOffset * (i + 1);
        pos = new Vector2(pos.x - Mathf.Floor(pos.x / 289.0f) * 289.0f, pos.y - Mathf.Floor(pos.y / 289.0f) * 289.0f);
        float res = Mathf.Abs(Mathf.Sin(43758.5453f * Vector2.Dot(pos, new Vector2(12.9898f, 78.233f))));
        res -= Mathf.Floor(res);
        return res;
    }

    private float p_PerlinNoise(float x, float y, uint i = 0)
    {
        return Mathf.PerlinNoise(x + m_perlinNoiseOffset.x * (i + 1), y + m_perlinNoiseOffset.y * (i + 1));
    }

    private float p_RidgedNoise(float x, float y, uint i = 0)
    {
        return 1.0f - 2.0f * Mathf.Abs(p_PerlinNoise(x, y, i) - 0.5f);
    }

    private void p_DrawBounds(BoundsInt b, Color color, float z = 0.0f)
    {
        Vector3 p1 = new Vector3(b.min.x, b.min.y, z);
        Vector3 p2 = new Vector3(b.max.x, b.min.y, z);
        Vector3 p3 = new Vector3(b.max.x, b.max.y, z);
        Vector3 p4 = new Vector3(b.min.x, b.max.y, z);

        Debug.DrawLine(p1, p2, color, 0.0f);
        Debug.DrawLine(p2, p3, color, 0.0f);
        Debug.DrawLine(p3, p4, color, 0.0f);
        Debug.DrawLine(p4, p1, color, 0.0f);

        Color temp = Gizmos.color;
        Gizmos.color = color;
        Vector3 center = Vector3Int.FloorToInt(b.center);
        center.z = z;
        Gizmos.DrawSphere(center, 0.2f);
        Gizmos.color = temp;
    }

    private void p_DrawGraph(Graph graph, Color color, float z = 0.0f)
    {
        foreach (KeyValuePair<Vector2Int, Vector2Int> edge in graph.edges)
        {
            Vector3 v1 = new Vector3(edge.Key.x  , edge.Key.y  , z);
            Vector3 v2 = new Vector3(edge.Value.x, edge.Value.y, z);
            Debug.DrawLine(v1, v2, color, 0.0f);
        }

        foreach (Vector2Int vertex in graph.vertices)
        {
            Color temp = Gizmos.color;
            Gizmos.color = color;
            Gizmos.DrawSphere(new Vector3(vertex.x, vertex.y, z), 0.2f);
            Gizmos.color = temp;
        }
    }

    private void p_DrawString(string text, int x, int y, Color color, float z = 0.0f)
    {
        GUIStyle style = new GUIStyle();
        style.normal.textColor = color;
        style.fontStyle = FontStyle.Bold;
        Handles.Label(new Vector3(x, y, z), new GUIContent(text), style);
    }

    [System.Serializable]
    public struct IslandFilter
    {
        public Mode mode;
        public int  threshold;

        public IslandFilter(Mode mode, int threshold)
        {
            this.mode      = mode;
            this.threshold = threshold;
        }

        public enum Mode
        {
            None,
            MaxOnly,
            Threshold
        }
    }

    public class Island
    {
        public Vector2Int       center { get; private set; }
        public BoundsInt        bounds { get; private set; }
        public List<Vector2Int> tiles  { get; private set; }

        public Island(List<Vector2Int> tiles)
        {
            this.tiles = tiles;
            center = Vector2Int.zero;
            bounds = new BoundsInt(Vector3Int.zero, Vector3Int.zero);
            Vector3Int minBound = new Vector3Int(int.MaxValue, int.MaxValue, 0);
            Vector3Int maxBound = new Vector3Int(int.MinValue, int.MinValue, 0);
            if (tiles.Count > 0)
            {
                foreach (Vector2Int tile in tiles)
                {
                    minBound.x = minBound.x < tile.x ? minBound.x : tile.x;
                    minBound.y = minBound.y < tile.y ? minBound.y : tile.y;
                    maxBound.x = maxBound.x > tile.x ? maxBound.x : tile.x;
                    maxBound.y = maxBound.y > tile.y ? maxBound.y : tile.y;
                }
                bounds = new BoundsInt(minBound, maxBound - minBound + new Vector3Int(1, 1, 0));
                center = Vector2Int.FloorToInt(new Vector2(bounds.center.x, bounds.center.y));
            }
        }
    }

    public class Map
    {
        private bool[,] tiles;
        
        public Vector2Int mapSize { get; private set; }

        private bool p_IsInBound(int x, int y)
        {
            return (x >= -mapSize.x && x <= mapSize.x && y >= -mapSize.y && y <= mapSize.y);
        }

        public Map(Vector2Int mapSize, bool[,] tiles)
        {
            this.mapSize = mapSize;
            this.tiles = tiles;
        }

        public bool IsWalkable(int x, int y)
        {
            if (!p_IsInBound(x, y))
            {
                return false;
            }
            return tiles[x + mapSize.x, y + mapSize.y];
        }
    }

    private enum BridgeDirection
    {
        Unknown = -1,

        Vertical = 1,
        Positive = 2,

        Left   = 0,
        Right  = Positive,
        Bottom = Vertical,
        Top    = Vertical | Positive
    }
}

namespace Editor
{
    [CustomPropertyDrawer(typeof(MapGenerator.IslandFilter))]
    public class MapGeneratorIslandFilterPropertyDrawer : PropertyDrawer
    {
        private SerializedProperty modeProperty;
        private SerializedProperty thresholdProperty;
        private bool               cached = false;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!cached)
            {
                cached            = true;
                modeProperty      = property.FindPropertyRelative("mode");
                thresholdProperty = property.FindPropertyRelative("threshold");
            }

            EditorGUI.LabelField(position, label);
            
            position.x += EditorGUIUtility.labelWidth;
            position.width -= EditorGUIUtility.labelWidth + EditorGUIUtility.fieldWidth + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(position, modeProperty, GUIContent.none);
            
            position.x += position.width + EditorGUIUtility.standardVerticalSpacing;
            position.width = EditorGUIUtility.fieldWidth;
            EditorGUI.BeginDisabledGroup(modeProperty.enumValueFlag != (int)MapGenerator.IslandFilter.Mode.Threshold);
            EditorGUI.PropertyField(position, thresholdProperty, GUIContent.none);
            EditorGUI.EndDisabledGroup();
        }
    }
}