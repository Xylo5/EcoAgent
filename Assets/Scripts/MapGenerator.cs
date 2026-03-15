using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Modular map generator for Level_0.
/// Generates a road network and fills remaining space with buildable empty tiles.
/// Roads permanently block grid cells; empty space tiles leave cells free for building placement.
/// </summary>
public class MapGenerator : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;

    [Header("Road Tiles")]
    public CityTileData roadStart;
    public CityTileData roadEnd;
    public CityTileData roadFiller;
    public CityTileData roadIntersection;
    public CityTileData roadBranch;

    [Header("Fill Tiles")]
    public CityTileData emptySpace;

    [Header("Generation Region")]
    [Tooltip("Bottom-left cell of the generation region.")]
    public Vector2Int regionMin = new Vector2Int(0, 0);
    [Tooltip("Top-right cell (exclusive) of the generation region.")]
    public Vector2Int regionMax = new Vector2Int(100, 100);

    [Header("Road Network")]
    [Tooltip("Number of horizontal roads spanning the map.")]
    [Range(2, 15)]
    public int horizontalRoads = 6;
    [Tooltip("Number of vertical roads spanning the map.")]
    [Range(2, 15)]
    public int verticalRoads = 6;
    [Tooltip("Perlin noise scale controlling road curvature. Lower = smoother, wider curves.")]
    [Range(0.005f, 0.1f)]
    public float curveScale = 0.03f;
    [Tooltip("Maximum sideways drift amplitude in cells.")]
    [Range(1f, 15f)]
    public float curveAmplitude = 6f;
    [Tooltip("Number of extra short connector roads between main roads.")]
    [Range(0, 20)]
    public int connectorRoads = 8;
    [Tooltip("How strongly connector roads prefer starting near map edges (0 = uniform, 1 = edges only).")]
    [Range(0f, 1f)]
    public float edgeBias = 0.85f;

    [Header("General")]
    [Tooltip("Random seed. 0 = random each run.")]
    public int randomSeed = 0;
    public bool generateOnStart = true;
    public bool fillEmptySpace = true;

    // Internal grid tracking
    private bool[,] roadMap;
    private int gridWidth;
    private int gridHeight;
    private float cellSize;
    private Vector3 terrainOrigin;
    private bool placedStart;

    // Cardinal directions: North, East, South, West
    private static readonly Vector2Int[] Cardinals =
    {
        Vector2Int.up,    // +Z
        Vector2Int.right, // +X
        Vector2Int.down,  // -Z
        Vector2Int.left   // -X
    };

    void Start()
    {
        if (generateOnStart)
            Generate();
    }

    // ═══════════════════════════════════════════
    //  PUBLIC API
    // ═══════════════════════════════════════════

    public void Generate()
    {
        if (gridManager == null)
        {
            Debug.LogError("[MapGenerator] No GridManager assigned!");
            return;
        }

        ClearMap();
        CacheGridSettings();

        int seed = (randomSeed != 0) ? randomSeed : System.Environment.TickCount;
        Random.InitState(seed);

        roadMap = new bool[gridWidth, gridHeight];
        placedStart = false;

        GenerateRoadNetwork();
        InstantiateRoadTiles();

        if (fillEmptySpace)
            FillEmptySpace();

        int roadCount = CountRoadCells();
        Debug.Log($"[MapGenerator] Map generated — {roadCount} road cells, seed {seed}.");
    }

    public void ClearMap()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }

    // ═══════════════════════════════════════════
    //  PHASE 1: ROAD NETWORK
    // ═══════════════════════════════════════════

    private void GenerateRoadNetwork()
    {
        int regionW = regionMax.x - regionMin.x;
        int regionH = regionMax.y - regionMin.y;

        // Per-generation Perlin offset so each seed produces unique curves
        float perlinOffsetX = Random.Range(0f, 10000f);
        float perlinOffsetZ = Random.Range(0f, 10000f);

        // Phase A: Horizontal roads — Perlin-curved, 2 cells wide
        for (int i = 0; i < horizontalRoads; i++)
        {
            int baseZ = regionMin.y + Mathf.RoundToInt((i + 0.5f) * regionH / horizontalRoads);
            baseZ = Mathf.Clamp(baseZ, regionMin.y + 1, regionMax.y - 2);

            for (int x = regionMin.x; x < regionMax.x; x++)
            {
                float noise = Mathf.PerlinNoise((x + perlinOffsetX) * curveScale, i * 17.3f);
                int drift = Mathf.RoundToInt((noise - 0.5f) * 2f * curveAmplitude);
                int z = Mathf.Clamp(baseZ + drift, regionMin.y, regionMax.y - 2);

                // Paint 2-wide road (this cell + the one above)
                roadMap[x, z] = true;
                roadMap[x, z + 1] = true;
            }
        }

        // Phase B: Vertical roads — Perlin-curved, 2 cells wide
        for (int i = 0; i < verticalRoads; i++)
        {
            int baseX = regionMin.x + Mathf.RoundToInt((i + 0.5f) * regionW / verticalRoads);
            baseX = Mathf.Clamp(baseX, regionMin.x + 1, regionMax.x - 2);

            for (int z = regionMin.y; z < regionMax.y; z++)
            {
                float noise = Mathf.PerlinNoise(i * 23.7f, (z + perlinOffsetZ) * curveScale);
                int drift = Mathf.RoundToInt((noise - 0.5f) * 2f * curveAmplitude);
                int x = Mathf.Clamp(baseX + drift, regionMin.x, regionMax.x - 2);

                // Paint 2-wide road (this cell + the one to the right)
                roadMap[x, z] = true;
                roadMap[x + 1, z] = true;
            }
        }

        // Phase C: Connector roads — biased toward map edges, 2 cells wide
        for (int i = 0; i < connectorRoads; i++)
        {
            Vector2Int start = PickEdgeBiasedRoadCell(regionW, regionH);
            if (start.x < 0) break; // no road cells exist

            int dirIndex = Random.Range(0, 4);
            Vector2Int dir = Cardinals[dirIndex];
            // Perpendicular offset for 2-wide connectors
            Vector2Int perp = new Vector2Int(Mathf.Abs(dir.y), Mathf.Abs(dir.x));
            int length = Random.Range(5, 20);
            Vector2Int current = start;

            for (int s = 0; s < length; s++)
            {
                if (!InRegion(current)) break;
                roadMap[current.x, current.y] = true;
                // Second lane
                Vector2Int adj = current + perp;
                if (InRegion(adj))
                    roadMap[adj.x, adj.y] = true;
                current += dir;
            }
        }
    }

    /// <summary>
    /// Picks a road cell biased toward map edges. Higher edgeBias = stronger preference for edges.
    /// </summary>
    private Vector2Int PickEdgeBiasedRoadCell(int regionW, int regionH)
    {
        List<Vector2Int> roadCells = GetAllRoadCells();
        if (roadCells.Count == 0) return new Vector2Int(-1, -1);

        // Try up to 30 times to find an edge-biased cell
        for (int attempt = 0; attempt < 30; attempt++)
        {
            Vector2Int candidate = roadCells[Random.Range(0, roadCells.Count)];

            // Normalized distance from center (0 = center, 1 = edge)
            float dx = Mathf.Abs(candidate.x - regionMin.x - regionW * 0.5f) / (regionW * 0.5f);
            float dz = Mathf.Abs(candidate.y - regionMin.y - regionH * 0.5f) / (regionH * 0.5f);
            float edgeness = Mathf.Max(dx, dz); // 0..1

            // Accept with probability proportional to edgeness
            float acceptChance = Mathf.Lerp(1f - edgeBias, 1f, edgeness);
            if (Random.value < acceptChance)
                return candidate;
        }

        // Fallback: pick any road cell
        return roadCells[Random.Range(0, roadCells.Count)];
    }

    // ═══════════════════════════════════════════
    //  PHASE 2: CLASSIFY & INSTANTIATE ROADS
    // ═══════════════════════════════════════════

    private void InstantiateRoadTiles()
    {
        for (int x = regionMin.x; x < regionMax.x; x++)
        {
            for (int z = regionMin.y; z < regionMax.y; z++)
            {
                if (!roadMap[x, z]) continue;

                Vector2Int cell = new Vector2Int(x, z);
                bool hasN = InBounds(x, z + 1) && roadMap[x, z + 1];
                bool hasS = InBounds(x, z - 1) && roadMap[x, z - 1];
                bool hasE = InBounds(x + 1, z) && roadMap[x + 1, z];
                bool hasW = InBounds(x - 1, z) && roadMap[x - 1, z];
                int neighbors = (hasN ? 1 : 0) + (hasS ? 1 : 0) + (hasE ? 1 : 0) + (hasW ? 1 : 0);

                CityTileData tile;
                float rotation = 0f;

                switch (neighbors)
                {
                    case 0:
                        // Isolated cell
                        tile = roadStart;
                        break;

                    case 1:
                        // Dead end
                        if (!placedStart)
                        {
                            tile = roadStart;
                            placedStart = true;
                        }
                        else
                        {
                            tile = roadEnd;
                        }
                        if (hasN) rotation = 0f;
                        else if (hasE) rotation = 90f;
                        else if (hasS) rotation = 180f;
                        else rotation = 270f;
                        break;

                    case 2:
                        if ((hasN && hasS) || (hasE && hasW))
                        {
                            // Straight segment
                            tile = roadFiller;
                            rotation = (hasE && hasW) ? 90f : 0f;
                        }
                        else
                        {
                            // L-turn (use branch as fallback since we have no L-piece)
                            tile = roadBranch;
                            if (hasN && hasE) rotation = 0f;
                            else if (hasE && hasS) rotation = 90f;
                            else if (hasS && hasW) rotation = 180f;
                            else rotation = 270f;
                        }
                        break;

                    case 3:
                        // T-junction
                        tile = roadBranch;
                        if (!hasS) rotation = 0f;
                        else if (!hasW) rotation = 90f;
                        else if (!hasN) rotation = 180f;
                        else rotation = 270f;
                        break;

                    case 4:
                        // 4-way intersection
                        tile = roadIntersection;
                        rotation = 0f;
                        break;

                    default:
                        tile = roadFiller;
                        break;
                }

                InstantiateTile(cell, tile, rotation, blockCells: true);
            }
        }
    }

    // ═══════════════════════════════════════════
    //  PHASE 3: FILL EMPTY SPACE
    // ═══════════════════════════════════════════

    private void FillEmptySpace()
    {
        if (emptySpace == null) return;

        for (int x = regionMin.x; x < regionMax.x; x++)
        {
            for (int z = regionMin.y; z < regionMax.y; z++)
            {
                if (roadMap[x, z]) continue;

                Vector2Int cell = new Vector2Int(x, z);

                // Skip cells already permanently occupied (e.g. by EnvironmentSpawner)
                if (Application.isPlaying && gridManager.IsCellPermanent(cell))
                    continue;

                InstantiateTile(cell, emptySpace, 0f, blockCells: false);
            }
        }
    }

    // ═══════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════

    private void InstantiateTile(Vector2Int cell, CityTileData tileData, float rotationY, bool blockCells)
    {
        if (tileData == null) return;
        GameObject prefab = tileData.GetRandomPrefab();
        if (prefab == null) return;

        int size = tileData.sizeInCells;
        Vector3 position = GetWorldPosition(cell, size);
        Quaternion rotation = Quaternion.Euler(0f, rotationY, 0f);

        GameObject instance;
        #if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            instance = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, transform);
            instance.transform.position = position;
            instance.transform.rotation = rotation;
        }
        else
        #endif
        {
            instance = Instantiate(prefab, position, rotation, transform);
        }

        instance.name = $"{tileData.tileName}_{cell.x}_{cell.y}";
        SetLayerRecursive(instance, LayerMask.NameToLayer("Ignore Raycast"));

        if (blockCells && Application.isPlaying)
        {
            gridManager.OccupyCellsPermanent(cell, size);
        }
    }

    private Vector3 GetWorldPosition(Vector2Int cell, int size)
    {
        float x = terrainOrigin.x + cell.x * cellSize + (size * cellSize) * 0.5f;
        float z = terrainOrigin.z + cell.y * cellSize + (size * cellSize) * 0.5f;
        float y = gridManager.terrain != null
            ? gridManager.terrain.SampleHeight(new Vector3(x, 0, z)) + terrainOrigin.y
            : terrainOrigin.y;
        return new Vector3(x, y, z);
    }

    private void CacheGridSettings()
    {
        gridWidth = gridManager.gridWidth;
        gridHeight = gridManager.gridHeight;
        cellSize = gridManager.cellSize > 0f ? gridManager.cellSize : 2.5f;
        terrainOrigin = (gridManager.terrain != null)
            ? gridManager.terrain.transform.position
            : Vector3.zero;
    }

    private bool InRegion(Vector2Int cell)
    {
        return cell.x >= regionMin.x && cell.x < regionMax.x &&
               cell.y >= regionMin.y && cell.y < regionMax.y;
    }

    private bool InBounds(int x, int z)
    {
        return x >= 0 && x < gridWidth && z >= 0 && z < gridHeight;
    }

    private List<Vector2Int> GetAllRoadCells()
    {
        List<Vector2Int> cells = new List<Vector2Int>();
        for (int x = regionMin.x; x < regionMax.x; x++)
            for (int z = regionMin.y; z < regionMax.y; z++)
                if (roadMap[x, z])
                    cells.Add(new Vector2Int(x, z));
        return cells;
    }

    private int CountRoadCells()
    {
        int count = 0;
        for (int x = 0; x < gridWidth; x++)
            for (int z = 0; z < gridHeight; z++)
                if (roadMap[x, z]) count++;
        return count;
    }

    private void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, layer);
    }
}
