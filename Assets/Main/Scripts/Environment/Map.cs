using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Tilemaps;

public class Map : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Tilemap        walkableTilemap;
    [SerializeField] private Tilemap        wallTilemap;
    [SerializeField] private Tilemap        detailTilemap;
    [SerializeField] private NavMeshSurface navigationMesh;

    [Header("Map Settings")]
    [SerializeField] private Optional<int> mapSeed;
    [SerializeField] private Vector2Int    mapSize;
    [SerializeField] private int           padding;
    [SerializeField] private int           minIslandSize;
    [SerializeField] private float         terrainScale;
    [SerializeField] private float         terrainThreshold;
    [SerializeField] private int           terrainOctaves;
    [SerializeField] private float         terrainPersistance;
    [SerializeField] private float         terrainLacunarity;
    [SerializeField] private float         terrainFalloffStrength;
    [SerializeField] private float         pathScale;
    [SerializeField] private float         pathThreshold;
    [SerializeField] private Vector2Int    spawnStructureSize;

    [Header("Tiles")]
    [SerializeField] private TilesWeighted    groundTiles;
    [SerializeField] private TilesWeighted    detailTiles;
    [SerializeField] private TilesDirectional bridgeTiles;
    [SerializeField] private Tiles16          wallTiles;
    [SerializeField] private Tiles16          pathTiles;
    [SerializeField] private TilesNineSlice   spawnStructureTiles;

    public int seed { get; private set; }

    private TileType[,]  m_map;
    private List<Island> m_islands        = new List<Island>();
    private Graph        m_connectedGraph = new Graph();

    private static Vector2Int[] s_directions = new Vector2Int[] {
        Vector2Int.left,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.up
    };

    public void Generate()
    {
        p_GetSeed();
        p_GenerateMap();
        p_PlaceTiles();

        navigationMesh.BuildNavMesh();
    }

    public void Clear()
    {
        m_islands.Clear();

        walkableTilemap.ClearAllTiles();
        wallTilemap.ClearAllTiles();
        detailTilemap.ClearAllTiles();

        navigationMesh.BuildNavMesh();
    }

    private void Start()
    {
        Generate();
    }

    private void OnDrawGizmosSelected()
    {
        m_connectedGraph.Draw(Color.red, Color.green, -1.0f);
    }

    private void p_GetSeed()
    {
        if (mapSeed.enabled)
        {
            seed = mapSeed.value;
        }
        else
        {
            seed = Environment.TickCount.GetHashCode();
        }
        Debug.Log("[Map generator] Initialized with seed " + seed);
    }

    private void p_GenerateMap()
    {
        m_islands.Clear();

        float[,] fractalNoiseMap = Noise.FractalNoiseMap(2 * mapSize + Vector2Int.one, seed, terrainScale, terrainOctaves, terrainPersistance, terrainLacunarity);
        float[,] falloffMap      = Falloff.FalloffMap(2 * mapSize + Vector2Int.one);
        float[,] whiteNoiseMap   = Noise.WhiteNoiseMap(2 * mapSize + Vector2Int.one, seed);
        float[,] ridgedNoiseMap  = Noise.RidgedNoiseMap(2 * mapSize + Vector2Int.one, seed, pathScale);

        // Generate base ground map

        m_map = new TileType[2 * mapSize.x + 1, 2 * mapSize.y + 1];
        for (int x = -mapSize.x; x <= mapSize.x; ++x)
        {
            for (int y = -mapSize.y; y <= mapSize.y; ++y)
            {
                float value = fractalNoiseMap[x + mapSize.x, y + mapSize.y] - terrainFalloffStrength * falloffMap[x + mapSize.x, y + mapSize.y];
                if (value >= terrainThreshold)
                {
                    m_map[x + mapSize.x, y + mapSize.y] = TileType.Ground;
                }
                else
                {
                    m_map[x + mapSize.x, y + mapSize.y] = TileType.Wall;
                }
            }
        }

        // Place spawn structure

        for (int x = -spawnStructureSize.x - 1; x <= spawnStructureSize.x + 1; ++x)
        {
            for (int y = -spawnStructureSize.y - 1; y <= spawnStructureSize.y + 1; ++y)
            {
                if (p_InBound(x, y))
                {
                    m_map[x + mapSize.x, y + mapSize.y] = TileType.SpawnStructure;
                }
            }
        }

        // Remove small island

        bool[,] scanned = new bool[2 * mapSize.x + 1, 2 * mapSize.y + 1];
        for (int x = -mapSize.x; x <= mapSize.x; ++x)
        {
            for (int y = -mapSize.y; y <= mapSize.y; ++y)
            {
                scanned[x + mapSize.x, y + mapSize.y] = false;
            }
        }
        List<Vector2Int> removedTiles = new List<Vector2Int>();
        for (int x = -mapSize.x; x <= mapSize.x; ++x)
        {
            for (int y = -mapSize.y; y <= mapSize.y; ++y)
            {
                if (!scanned[x + mapSize.x, y + mapSize.y] && p_GetTile(x, y) == TileType.Ground)
                {
                    HashSet<Vector2Int> connectedTiles = new HashSet<Vector2Int>();
                    List<Vector2Int> p = new List<Vector2Int>();
                    p.Add(new Vector2Int(x, y));
                    scanned[x + mapSize.x, y + mapSize.y] = true;
                    while (p.Count > 0)
                    {
                        Vector2Int currentTile = p[0];
                        p.RemoveAt(0);

                        connectedTiles.Add(currentTile);
                        
                        foreach (Vector2Int direction in s_directions)
                        {
                            Vector2Int adjTile = currentTile + direction;
                            if (p_InBound(adjTile))
                            {
                                if ((p_GetTile(adjTile) == TileType.Ground || p_GetTile(adjTile) == TileType.SpawnStructure) && !scanned[adjTile.x + mapSize.x, adjTile.y + mapSize.y] && !p.Contains(adjTile))
                                {
                                    p.Add(adjTile);
                                    scanned[adjTile.x + mapSize.x, adjTile.y + mapSize.y] = true;
                                }
                            }
                        }
                    }

                    if (connectedTiles.Count >= minIslandSize)
                    {
                        m_islands.Add(new Island(connectedTiles, this));
                    }
                    else
                    {
                        removedTiles.AddRange(connectedTiles);
                    }
                }
            }
        }
        foreach (Vector2Int tile in removedTiles)
        {
            m_map[tile.x + mapSize.x, tile.y + mapSize.y] = TileType.Wall;
        }
        Debug.Log("[Map generator] Generated " + m_islands.Count + " island(s)");

        // Connect islands using bridges

        m_connectedGraph.Clear();
        foreach (Island i in m_islands) {
            m_connectedGraph.AddVertex(i.center);
            foreach (Island j in m_islands)
            {
                m_connectedGraph.AddEdge(i.center, j.center, p_DistanceBetweenIslands(i, j));
            }
        }
        Graph temp = m_connectedGraph.MinSpanTree();
        m_connectedGraph.Clear();
        foreach (Graph.Edge e in temp.edges)
        {
            Island i1 = p_FindIslandByCenter(e.v1);
            Island i2 = p_FindIslandByCenter(e.v2);

            if (i1 != null && i2 != null)
            {
                Vector2Int p1, p2;
                int dist = p_PathBetweenIslands(i1, i2, out p1, out p2);
                
                if (p1.x != p2.x && p1.y != p2.y)
                {
                    bool b = whiteNoiseMap[p1.x + mapSize.x, p1.y + mapSize.y] >= 0.5f;
                    Vector2Int p3 = b ? new Vector2Int(p1.x, p2.y) : new Vector2Int(p2.x, p1.y);
                    if (p1.x == p3.x && p1.x == -mapSize.y)
                    {
                        ++p1.x;
                        ++p3.x;
                    }
                    else if (p1.y == p3.y && p1.y == -mapSize.y)
                    {
                        ++p1.y;
                        ++p3.y;
                    }
                    if (p2.x == p3.x && p2.x == -mapSize.y)
                    {
                        ++p1.x;
                        ++p3.x;
                    }
                    else if (p2.y == p3.y && p2.y == -mapSize.y)
                    {
                        ++p1.y;
                        ++p3.y;
                    }
                    m_connectedGraph.AddEdge(p1, p3);
                    m_connectedGraph.AddEdge(p2, p3);
                }
                else
                {
                    if (p1.x == p2.x && p1.x == -mapSize.y)
                    {
                        ++p1.x;
                        ++p2.x;
                    }
                    else if (p1.y == p2.y && p1.y == -mapSize.y)
                    {
                        ++p1.y;
                        ++p2.y;
                    }
                    m_connectedGraph.AddEdge(p1, p2);
                }
            }
        }
        foreach (Graph.Edge e in m_connectedGraph.edges)
        {
            if (e.v1.x == e.v2.x)
            {
                int s = Math.Min(e.v1.y, e.v2.y);
                int t = Math.Max(e.v1.y, e.v2.y);
                for (int i = s; i <= t; ++i)
                {
                    m_map[e.v1.x - 0 + mapSize.x, i + mapSize.y] = TileType.BridgeVertical;
                    m_map[e.v1.x - 1 + mapSize.x, i + mapSize.y] = TileType.BridgeVertical;
                }
                int sx1 = Math.Max(e.v1.x - 1, -mapSize.x);
                int tx1 = e.v1.x;
                int sy1 = Math.Max(e.v1.y - 1, -mapSize.y);
                int ty1 = e.v1.y;
                for (int ix = sx1; ix <= tx1; ++ix)
                {
                    for (int iy = sy1; iy <= ty1; ++iy)
                    {
                        m_map[ix + mapSize.x, iy + mapSize.y] = TileType.BridgeVertical;
                    }
                }
                int sx2 = Math.Max(e.v2.x - 1, -mapSize.x);
                int tx2 = e.v2.x;
                int sy2 = Math.Max(e.v2.y - 1, -mapSize.y);
                int ty2 = e.v2.y;
                for (int ix = sx2; ix <= tx2; ++ix)
                {
                    for (int iy = sy2; iy <= ty2; ++iy)
                    {
                        m_map[ix + mapSize.x, iy + mapSize.y] = TileType.BridgeVertical;
                    }
                }
            }
            else if (e.v1.y == e.v2.y)
            {
                int s = Math.Min(e.v1.x, e.v2.x);
                int t = Math.Max(e.v1.x, e.v2.x);
                for (int i = s; i <= t; ++i)
                {
                    m_map[i + mapSize.x, e.v1.y - 0 + mapSize.y] = TileType.BridgeHorizontal;
                    m_map[i + mapSize.x, e.v1.y - 1 + mapSize.y] = TileType.BridgeHorizontal;
                }
                int sx1 = Math.Max(e.v1.x - 1, -mapSize.x);
                int tx1 = e.v1.x;
                int sy1 = Math.Max(e.v1.y - 1, -mapSize.y);
                int ty1 = e.v1.y;
                for (int ix = sx1; ix <= tx1; ++ix)
                {
                    for (int iy = sy1; iy <= ty1; ++iy)
                    {
                        m_map[ix + mapSize.x, iy + mapSize.y] = TileType.BridgeHorizontal;
                    }
                }
                int sx2 = Math.Max(e.v2.x - 1, -mapSize.x);
                int tx2 = e.v2.x;
                int sy2 = Math.Max(e.v2.y - 1, -mapSize.y);
                int ty2 = e.v2.y;
                for (int ix = sx2; ix <= tx2; ++ix)
                {
                    for (int iy = sy2; iy <= ty2; ++iy)
                    {
                        m_map[ix + mapSize.x, iy + mapSize.y] = TileType.BridgeHorizontal;
                    }
                }
            }
        }

        // Place path

        for (int x = -mapSize.x; x <= mapSize.x; ++x)
        {
            for (int y = -mapSize.y; y <= mapSize.y; ++y)
            {
                if (p_GetTile(x, y) == TileType.Ground && ridgedNoiseMap[x + mapSize.x, y + mapSize.y] >= pathThreshold)
                {
                    m_map[x + mapSize.x, y + mapSize.y] = TileType.Path;
                }
            }
        }
        for (int x = -mapSize.x; x <= mapSize.x; ++x)
        {
            for (int y = -mapSize.y; y <= mapSize.y; ++y)
            {
                if (p_GetTile(x, y) == TileType.Path && (
                    (p_GetTile(x - 1, y) != TileType.Ground && p_GetTile(x - 1, y) != TileType.Path) ||
                    (p_GetTile(x + 1, y) != TileType.Ground && p_GetTile(x + 1, y) != TileType.Path) ||
                    (p_GetTile(x, y - 1) != TileType.Ground && p_GetTile(x, y - 1) != TileType.Path) ||
                    (p_GetTile(x, y + 1) != TileType.Ground && p_GetTile(x, y + 1) != TileType.Path))
                )
                {
                    m_map[x + mapSize.x, y + mapSize.y] = TileType.Ground;
                }
            }
        }
    }

    private void p_PlaceTiles()
    {
        float[,] whiteNoiseMap1 = Noise.WhiteNoiseMap(2 * mapSize + Vector2Int.one, seed);
        float[,] whiteNoiseMap2 = Noise.WhiteNoiseMap(2 * mapSize + Vector2Int.one, seed + 69420);
        float[,] whiteNoiseMap3 = Noise.WhiteNoiseMap(2 * mapSize + Vector2Int.one, seed - 69420);

        walkableTilemap.ClearAllTiles();
        wallTilemap.ClearAllTiles();
        detailTilemap.ClearAllTiles();

        for (int x = -mapSize.x - padding; x <= mapSize.x + padding; ++x)
        {
            for (int y = -mapSize.y - padding; y <= mapSize.y + padding; ++y)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                switch (p_GetTile(x, y))
                {
                    case TileType.Ground:
                        {
                            walkableTilemap.SetTile(cell, groundTiles.Get(whiteNoiseMap1[x + mapSize.x, y + mapSize.y]));
                            detailTilemap.SetTile(cell, detailTiles.Get(whiteNoiseMap2[x + mapSize.x, y + mapSize.y]));
                            break;
                        }
                    case TileType.Wall:
                        {
                            int index = 0;
                            if (p_GetTile(x, y + 1) == TileType.Ground || p_GetTile(x, y + 1) == TileType.SpawnStructure)
                            {
                                index += 1;
                            }
                            if (p_GetTile(x + 1, y) == TileType.Ground || p_GetTile(x + 1, y) == TileType.SpawnStructure)
                            {
                                index += 2;
                            }
                            if (p_GetTile(x, y - 1) == TileType.Ground || p_GetTile(x, y - 1) == TileType.SpawnStructure)
                            {
                                index += 4;
                            }
                            if (p_GetTile(x - 1, y) == TileType.Ground || p_GetTile(x - 1, y) == TileType.SpawnStructure)
                            {
                                index += 8;
                            }
                            wallTilemap.SetTile(cell, wallTiles.GetList()[index]);
                            break;
                        }
                    case TileType.BridgeHorizontal:
                        {
                            walkableTilemap.SetTile(cell, p_GetTile(x, y - 1) == TileType.BridgeHorizontal ?  bridgeTiles.up : bridgeTiles.down);
                            break;
                        }
                    case TileType.BridgeVertical:
                        {
                            walkableTilemap.SetTile(cell, p_GetTile(x - 1, y) == TileType.BridgeVertical ? bridgeTiles.right : bridgeTiles.left);
                            break;
                        }
                    case TileType.Path:
                        {
                            int index = 0;
                            if (p_GetTile(x, y + 1) == TileType.Ground || p_GetTile(x, y + 1) == TileType.SpawnStructure)
                            {
                                index += 1;
                            }
                            if (p_GetTile(x + 1, y) == TileType.Ground || p_GetTile(x + 1, y) == TileType.SpawnStructure)
                            {
                                index += 2;
                            }
                            if (p_GetTile(x, y - 1) == TileType.Ground || p_GetTile(x, y - 1) == TileType.SpawnStructure)
                            {
                                index += 4;
                            }
                            if (p_GetTile(x - 1, y) == TileType.Ground || p_GetTile(x - 1, y) == TileType.SpawnStructure)
                            {
                                index += 8;
                            }
                            walkableTilemap.SetTile(cell, pathTiles.GetList()[index]);
                            break;
                        }
                    case TileType.SpawnStructure:
                        {
                            walkableTilemap.SetTile(cell, spawnStructureTiles.Get(new Vector2Int(x, y), -spawnStructureSize - Vector2Int.one, spawnStructureSize + Vector2Int.one, whiteNoiseMap3[x + mapSize.x, y + mapSize.y]));
                            break;
                        }
                    default:
                        break;
                }
            }
        }
    }

    private bool p_InBound(int x, int y)
    {
        return (
            x >= -mapSize.x &&
            x <=  mapSize.x &&
            y >= -mapSize.y &&
            y <=  mapSize.y
        );
    }

    private bool p_InBound(Vector2Int pos)
    {
        return (
            pos.x >= -mapSize.x &&
            pos.x <=  mapSize.x &&
            pos.y >= -mapSize.y &&
            pos.y <=  mapSize.y
        );
    }

    private TileType p_GetTile(int x, int y)
    {
        if (!p_InBound(x, y))
        {
            return TileType.Wall;
        }
        return m_map[x + mapSize.x, y + mapSize.y];
    }

    private TileType p_GetTile(Vector2Int pos)
    {
        if (!p_InBound(pos))
        {
            return TileType.Wall;
        }
        return m_map[pos.x + mapSize.x, pos.y + mapSize.y];
    }

    private Island p_FindIslandByCenter(Vector2Int center)
    {
        foreach (Island island in m_islands)
        {
            if (island.center == center)
            {
                return island;
            }
        }
        return null;
    }

    private int p_DistanceBetweenIslands(Island i1, Island i2)
    {
        int res = int.MaxValue;
        foreach (Vector2Int tile in i1.tiles)
        {
            if (i2.distanceMap[tile.x + mapSize.x, tile.y + mapSize.y] < res)
            {
                res = i2.distanceMap[tile.x + mapSize.x, tile.y + mapSize.y];
            }
        }
        return res;
    }

    private int p_PathBetweenIslands(Island i1, Island i2, out Vector2Int p1, out Vector2Int p2)
    {
        int res = int.MaxValue;
        p1 = Vector2Int.zero;
        p2 = Vector2Int.zero;
        foreach (Vector2Int tile in i1.tiles)
        {
            if (i2.distanceMap[tile.x + mapSize.x, tile.y + mapSize.y] < res)
            {
                res = i2.distanceMap[tile.x + mapSize.x, tile.y + mapSize.y];
                p1 = tile;
                p2 = i2.pathMap[tile.x + mapSize.x, tile.y + mapSize.y];
            }
        }
        return res;
    }

    private enum TileType
    {
        Ground,
        Wall,
        BridgeHorizontal,
        BridgeVertical,
        Path,
        SpawnStructure
    }

    private class Island
    {
        public Map              map          { get; private set; }
        public List<Vector2Int> tiles        { get; private set; }
        public BoundsInt        bounds       { get; private set; }
        public Vector2Int       center       { get; private set; }
        public int[,]           distanceMap  { get; private set; }
        public Vector2Int[,]    pathMap      { get; private set; }

        public Island(IEnumerable<Vector2Int> tiles, Map map)
        {
            this.map = map;
            this.tiles        = new List<Vector2Int>(tiles);

            p_ComputeBoundsAndCenter();
            p_ComputeDistanceAndPathMap();
        }

        private void p_ComputeBoundsAndCenter()
        {
            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;

            foreach (Vector2Int tile in tiles)
            {
                minX = Math.Min(minX, tile.x);
                maxX = Math.Max(maxX, tile.x);
                minY = Math.Min(minY, tile.y);
                maxY = Math.Max(maxY, tile.y);
            }

            bounds.SetMinMax(new Vector3Int(minX, minY, 0), new Vector3Int(maxX, maxY, 0));
            center = new Vector2Int((minX + maxX + 1) / 2, (minY + maxY + 1) / 2);
        }

        private void p_ComputeDistanceAndPathMap()
        {
            distanceMap = new int[2 * map.mapSize.x + 1, 2 * map.mapSize.y + 1];
            pathMap = new Vector2Int[2 * map.mapSize.x + 1, 2 * map.mapSize.y + 1];
            for (int x = -map.mapSize.x; x <= map.mapSize.x; ++x)
            {
                for (int y = -map.mapSize.y; y <= map.mapSize.y; ++y)
                {
                    distanceMap[x + map.mapSize.x, y + map.mapSize.y] = -1;
                    pathMap[x + map.mapSize.x, y + map.mapSize.y] = new Vector2Int(x, y);
                }
            }
            HashSet<Vector2Int> scannedTiles = new HashSet<Vector2Int>(tiles);
            Dictionary<Vector2Int, Vector2Int> currentTiles = new Dictionary<Vector2Int, Vector2Int>();
            foreach (Vector2Int tile in tiles)
            {
                currentTiles[tile] = tile;
            }
            int d = 0;
            while (currentTiles.Count > 0)
            {
                Dictionary<Vector2Int, Vector2Int> newTiles = new Dictionary<Vector2Int, Vector2Int>();
                foreach (Vector2Int tile in currentTiles.Keys)
                {
                    distanceMap[tile.x + map.mapSize.x, tile.y + map.mapSize.y] = d;
                    pathMap[tile.x + map.mapSize.x, tile.y + map.mapSize.y] = currentTiles[tile];
                    foreach (Vector2Int direction in s_directions)
                    {
                        Vector2Int adjTile = tile + direction;
                        if (map.p_InBound(adjTile) && scannedTiles.Add(adjTile) && distanceMap[adjTile.x + map.mapSize.x, adjTile.y + map.mapSize.y] == -1)
                        {
                            newTiles[adjTile] = currentTiles[tile];
                        }
                    }
                }
                currentTiles = newTiles;
                ++d;
            }
        }
    }

    [Serializable]
    public struct TilesDirectional
    {
        public Tile left;
        public Tile right;
        public Tile down;
        public Tile up;
    }

    [Serializable]
    public struct TilesWeighted
    {
        public Entry[] entries;

        public Tile Get(float sample)
        {
            if (entries.Length > 0)
            {
                float totalWeight = 0.0f;
                foreach (Entry entry in entries)
                {
                    totalWeight += entry.weight;
                }
                float invTotalWeight = 1.0f / totalWeight;

                float currentWeight = 0.0f;
                foreach (Entry entry in entries)
                {
                    currentWeight += entry.weight * invTotalWeight;
                    if (currentWeight >= sample)
                    {
                        return entry.tile;
                    }
                }
            }
            return null;
        }

        [Serializable]
        public struct Entry
        {
            public Tile  tile;
            public float weight;
        }
    }

    [Serializable]
    public struct TilesNineSlice 
    {
        public Tile          left;
        public Tile          right;
        public Tile          bottom;
        public Tile          top;
        public Tile          bottomLeft;
        public Tile          bottomRight;
        public Tile          topLeft;
        public Tile          topRight;
        public TilesWeighted center;

        public Tile Get(Vector2Int pos, Vector2Int min, Vector2Int max, float sample)
        {
            if (max.x < min.x)
            {
                int temp = max.x;
                max.x = min.x;
                min.x = temp;
            }
            if (max.y < min.y)
            {
                int temp = max.y;
                max.y = min.y;
                min.y = temp;
            }
            if (pos.x >= min.x && pos.x <= max.x && pos.y >= min.y && pos.y <= max.y)
            {
                if (pos.x == min.x)
                {
                    return pos.y == min.y ? bottomLeft : pos.y == max.y ? topLeft : left;
                }
                if (pos.x == max.x)
                {
                    return pos.y == min.y ? bottomRight : pos.y == max.y ? topRight : right;
                }
                return pos.y == min.y ? bottom : pos.y == max.y ? top : center.Get(sample);
            }
            return null;
        }
    }

    [Serializable]
    public struct Tiles16
    {
        public Tile tile0;
        public Tile tile1;
        public Tile tile2;
        public Tile tile3;
        public Tile tile4;
        public Tile tile5;
        public Tile tile6;
        public Tile tile7;
        public Tile tile8;
        public Tile tile9;
        public Tile tile10;
        public Tile tile11;
        public Tile tile12;
        public Tile tile13;
        public Tile tile14;
        public Tile tile15;

        public Tile[] GetList()
        {
            return new Tile[] {
                tile0,
                tile1,
                tile2,
                tile3,
                tile4,
                tile5,
                tile6,
                tile7,
                tile8,
                tile9,
                tile10,
                tile11,
                tile12,
                tile13,
                tile14,
                tile15
            };
        }
    }
}

namespace MyEditor
{
    [CustomEditor(typeof(Map))]
    public class MapEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            Map map = (Map)target;
            DrawDefaultInspector();
            if (GUILayout.Button("Generate"))
            {
                map.Generate();
            }
            if (GUILayout.Button("Clear"))
            {
                map.Clear();
            }
        }
    }
}