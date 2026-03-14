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
    [Tooltip("Chance (0-1) a road drifts sideways each step, creating gentle curves.")]
    [Range(0f, 0.3f)]
    public float wanderChance = 0.08f;
    [Tooltip("Number of extra short connector roads between main roads.")]
    [Range(0, 20)]
    public int connectorRoads = 8;

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

        // Phase A: Horizontal roads — evenly spaced, each spans full width with gentle wandering
        for (int i = 0; i < horizontalRoads; i++)
        {
            // Evenly space along Z axis
            int baseZ = regionMin.y + Mathf.RoundToInt((i + 0.5f) * regionH / horizontalRoads);
            baseZ = Mathf.Clamp(baseZ, regionMin.y, regionMax.y - 1);
            int z = baseZ;

            for (int x = regionMin.x; x < regionMax.x; x++)
            {
                z = Mathf.Clamp(z, regionMin.y, regionMax.y - 1);
                roadMap[x, z] = true;

                // Gentle wander: drift up or down by 1 cell
                if (Random.value < wanderChance)
                    z += (Random.value < 0.5f) ? 1 : -1;
            }
        }

        // Phase B: Vertical roads — evenly spaced, each spans full height with gentle wandering
        for (int i = 0; i < verticalRoads; i++)
        {
            int baseX = regionMin.x + Mathf.RoundToInt((i + 0.5f) * regionW / verticalRoads);
            baseX = Mathf.Clamp(baseX, regionMin.x, regionMax.x - 1);
            int x = baseX;

            for (int z = regionMin.y; z < regionMax.y; z++)
            {
                x = Mathf.Clamp(x, regionMin.x, regionMax.x - 1);
                roadMap[x, z] = true;

                if (Random.value < wanderChance)
                    x += (Random.value < 0.5f) ? 1 : -1;
            }
        }

        // Phase C: Random connector roads — short roads linking main roads for variety
        for (int i = 0; i < connectorRoads; i++)
        {
            List<Vector2Int> roadCells = GetAllRoadCells();
            if (roadCells.Count == 0) break;

            Vector2Int start = roadCells[Random.Range(0, roadCells.Count)];
            int dirIndex = Random.Range(0, 4);
            int length = Random.Range(5, 20);
            Vector2Int current = start;
            for (int s = 0; s < length; s++)
            {
                if (!InRegion(current)) break;
                roadMap[current.x, current.y] = true;
                current += Cardinals[dirIndex];
            }
        }
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
