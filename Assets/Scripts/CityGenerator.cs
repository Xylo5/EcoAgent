using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Procedurally places buildings on the grid near roads.
/// Generation is done in Scene view via Editor button — not at runtime.
/// Attach to an empty GameObject and assign references in Inspector.
/// </summary>
public class CityGenerator : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;
    public RoadGenerator roadGenerator;

    [Header("Buildings (assign BuildingData assets)")]
    public BuildingData[] buildings;

    [Header("Generation Settings")]
    public int randomSeed = 0;
    [Range(5, 80)]
    [Tooltip("Percentage of road-adjacent candidate cells to attempt filling.")]
    public int density = 20;
    [Range(1, 3)]
    [Tooltip("How many cells away from a road a building can be placed.")]
    public int roadProximity = 2;

    [Header("Debug")]
    [Tooltip("Extra Y offset above terrain for placed buildings.")]
    public float debugYOffset = 0.15f;

    // Internal
    private bool[,] occupiedMap;
    private bool[,] roadCellMap;
    private int gridWidth, gridHeight;
    private float cellSize;

    void Start()
    {
        // No regeneration at runtime — just mark existing children as permanent
        if (gridManager != null)
            MarkExistingBuildingsAtRuntime();
    }

    // ──────────────────────────────────────────────
    //  Public API
    // ──────────────────────────────────────────────

    public void Generate()
    {
        CacheGridSettings();
        ClearMap();
        InitGrids();

        if (randomSeed != 0)
            Random.InitState(randomSeed);
        else
            Random.InitState(System.Environment.TickCount);

        BuildOccupiedMap();
        BuildRoadCellMap();

        List<Vector2Int> candidates = FindRoadAdjacentCells();
        Debug.Log($"[CityGenerator] Found {candidates.Count} candidate cells near roads.");
        ShuffleList(candidates);

        // Pick a random subset of building types for this seed
        BuildingData[] selectedTypes = SelectBuildingTypes();
        if (selectedTypes.Length == 0)
        {
            Debug.LogWarning("[CityGenerator] No buildings assigned or selected.");
            return;
        }

        int maxPlacements = Mathf.Max(1, (candidates.Count * density) / 100);
        int placed = 0;

        for (int i = 0; i < candidates.Count && placed < maxPlacements; i++)
        {
            Vector2Int cell = candidates[i];
            BuildingData data = selectedTypes[Random.Range(0, selectedTypes.Length)];

            // Random rotation (0, 90, 180, 270)
            int rotStep = Random.Range(0, 4);
            int sx = (rotStep % 2 == 0) ? data.sizeX : data.sizeZ;
            int sz = (rotStep % 2 == 0) ? data.sizeZ : data.sizeX;

            // Check bounds
            if (cell.x + sx > gridWidth || cell.y + sz > gridHeight)
                continue;

            // Check if area is free
            if (!IsAreaFree(cell, sx, sz))
                continue;

            // Place building
            PlaceBuilding(cell, data, rotStep, sx, sz);
            MarkOccupied(cell, sx, sz);
            placed++;
        }

        Debug.Log($"[CityGenerator] Done. Placed {placed} buildings (density {density}%, seed {randomSeed}).");
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

    // ──────────────────────────────────────────────
    //  Setup
    // ──────────────────────────────────────────────

    private void CacheGridSettings()
    {
        gridWidth = gridManager.gridWidth;
        gridHeight = gridManager.gridHeight;
        cellSize = gridManager.cellSize > 0f ? gridManager.cellSize : 2.5f;
    }

    private void InitGrids()
    {
        occupiedMap = new bool[gridWidth, gridHeight];
        roadCellMap = new bool[gridWidth, gridHeight];
    }

    /// <summary>
    /// Scans RoadGenerator's children (roads, rivers, mountains) to build
    /// a map of which cells are already occupied.
    /// Also marks any cells that already have CityGenerator children (re-generate case).
    /// </summary>
    private void BuildOccupiedMap()
    {
        Vector3 origin = gridManager.GridOrigin;

        // Scan road generator children (skip empty space fillers — those are non-blocking)
        if (roadGenerator != null)
        {
            string emptyName = roadGenerator.emptySpace != null ? roadGenerator.emptySpace.tileName : "";

            foreach (Transform child in roadGenerator.transform)
            {
                // Skip empty space filler tiles — they don't block placement
                if (!string.IsNullOrEmpty(emptyName) && child.name.StartsWith(emptyName))
                    continue;

                MarkBoundsAsOccupied(child, origin);
            }
        }
    }

    /// <summary>
    /// Builds a map of which cells are road cells (from RoadGenerator children).
    /// Used to determine road adjacency.
    /// </summary>
    private void BuildRoadCellMap()
    {
        if (roadGenerator == null) return;

        Vector3 origin = gridManager.GridOrigin;
        string emptyName = roadGenerator.emptySpace != null ? roadGenerator.emptySpace.tileName : "";
        int roadCellCount = 0;

        foreach (Transform child in roadGenerator.transform)
        {
            // Skip river and mountain placeholders — they are obstacles, not roads
            if (child.name.StartsWith("River_") || child.name.StartsWith("Mountain_"))
                continue;

            // Skip empty space filler tiles
            if (!string.IsNullOrEmpty(emptyName) && child.name.StartsWith(emptyName))
                continue;

            Renderer[] renderers = child.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) continue;

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                b.Encapsulate(renderers[i].bounds);

            int minX = Mathf.FloorToInt((b.min.x - origin.x) / cellSize);
            int minZ = Mathf.FloorToInt((b.min.z - origin.z) / cellSize);
            int maxX = Mathf.CeilToInt((b.max.x - origin.x) / cellSize);
            int maxZ = Mathf.CeilToInt((b.max.z - origin.z) / cellSize);

            minX = Mathf.Clamp(minX, 0, gridWidth);
            minZ = Mathf.Clamp(minZ, 0, gridHeight);
            maxX = Mathf.Clamp(maxX, 0, gridWidth);
            maxZ = Mathf.Clamp(maxZ, 0, gridHeight);

            for (int x = minX; x < maxX; x++)
                for (int z = minZ; z < maxZ; z++)
                {
                    roadCellMap[x, z] = true;
                    roadCellCount++;
                }
        }

        Debug.Log($"[CityGenerator] Road cell map: {roadCellCount} cells from {roadGenerator.transform.childCount} children.");
    }

    private void MarkBoundsAsOccupied(Transform child, Vector3 origin)
    {
        Renderer[] renderers = child.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);

        int minX = Mathf.FloorToInt((b.min.x - origin.x) / cellSize);
        int minZ = Mathf.FloorToInt((b.min.z - origin.z) / cellSize);
        int maxX = Mathf.CeilToInt((b.max.x - origin.x) / cellSize);
        int maxZ = Mathf.CeilToInt((b.max.z - origin.z) / cellSize);

        minX = Mathf.Clamp(minX, 0, gridWidth);
        minZ = Mathf.Clamp(minZ, 0, gridHeight);
        maxX = Mathf.Clamp(maxX, 0, gridWidth);
        maxZ = Mathf.Clamp(maxZ, 0, gridHeight);

        for (int x = minX; x < maxX; x++)
            for (int z = minZ; z < maxZ; z++)
                occupiedMap[x, z] = true;
    }

    // ──────────────────────────────────────────────
    //  Road Adjacency & Candidate Finding
    // ──────────────────────────────────────────────

    /// <summary>
    /// Finds all empty cells adjacent to road cells (within roadProximity distance).
    /// </summary>
    private List<Vector2Int> FindRoadAdjacentCells()
    {
        List<Vector2Int> candidates = new List<Vector2Int>();

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                if (occupiedMap[x, z] || roadCellMap[x, z])
                    continue;

                if (IsNearRoad(x, z))
                    candidates.Add(new Vector2Int(x, z));
            }
        }

        return candidates;
    }

    private bool IsNearRoad(int cx, int cz)
    {
        int range = roadProximity;
        for (int dx = -range; dx <= range; dx++)
        {
            for (int dz = -range; dz <= range; dz++)
            {
                int nx = cx + dx;
                int nz = cz + dz;
                if (nx >= 0 && nx < gridWidth && nz >= 0 && nz < gridHeight)
                {
                    if (roadCellMap[nx, nz])
                        return true;
                }
            }
        }
        return false;
    }

    // ──────────────────────────────────────────────
    //  Building Type Selection
    // ──────────────────────────────────────────────

    /// <summary>
    /// Randomly selects a subset of assigned building types for this seed.
    /// Picks between 3 and all assigned types, ensuring variety across seeds.
    /// </summary>
    private BuildingData[] SelectBuildingTypes()
    {
        if (buildings == null || buildings.Length == 0)
            return new BuildingData[0];

        // Filter out nulls
        List<BuildingData> valid = new List<BuildingData>();
        foreach (var b in buildings)
            if (b != null) valid.Add(b);

        if (valid.Count == 0)
            return new BuildingData[0];

        // Pick a random subset: minimum 3, maximum all
        int minTypes = Mathf.Min(3, valid.Count);
        int count = Random.Range(minTypes, valid.Count + 1);

        // Shuffle and take first 'count'
        ShuffleList(valid);
        BuildingData[] selected = new BuildingData[count];
        for (int i = 0; i < count; i++)
            selected[i] = valid[i];

        string names = "";
        foreach (var s in selected)
            names += s.buildingName + ", ";
        Debug.Log($"[CityGenerator] Selected {count} building types: {names.TrimEnd(',', ' ')}");

        return selected;
    }

    // ──────────────────────────────────────────────
    //  Building Placement
    // ──────────────────────────────────────────────

    private void PlaceBuilding(Vector2Int cell, BuildingData data, int rotStep, int sx, int sz)
    {
        if (data.prefab == null)
        {
            Debug.LogWarning($"[CityGenerator] {data.buildingName} has no prefab assigned.");
            return;
        }

        Vector3 pos = GetWorldPosition(cell, sx, sz);
        float yAngle = rotStep * 90f;
        Quaternion rotation = Quaternion.Euler(0f, yAngle, 0f);

        GameObject instance = Instantiate(data.prefab, pos, rotation, transform);
        instance.name = $"{data.buildingName}_{cell.x}_{cell.y}";

        // Scale to fit grid (same logic as BuildingPlacer.AutoScaleToGrid)
        AutoScaleToGrid(instance, data);

        // Bounds-based centering after scaling
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            Vector3 offset = new Vector3(
                instance.transform.position.x - bounds.center.x,
                instance.transform.position.y - bounds.min.y,
                instance.transform.position.z - bounds.center.z
            );
            instance.transform.position += offset;
        }

        // Remove colliders so they don't interfere with raycasts
        Collider col = instance.GetComponentInChildren<Collider>();
        if (col != null)
        {
            if (Application.isPlaying) Destroy(col);
            else DestroyImmediate(col);
        }

        SetLayerRecursive(instance, LayerMask.NameToLayer("Ignore Raycast"));

        // Mark cells as permanent in edit mode (for the occupiedMap within this generation)
        // Runtime permanent marking is done in MarkExistingBuildingsAtRuntime
    }

    private void AutoScaleToGrid(GameObject obj, BuildingData data)
    {
        if (obj == null || data == null) return;

        obj.transform.localScale = Vector3.one;

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        float maxModelSize = Mathf.Max(bounds.size.x, bounds.size.z);
        if (maxModelSize < 0.001f) return;

        float targetX = data.sizeX * cellSize;
        float targetZ = data.sizeZ * cellSize;
        float maxTargetSize = Mathf.Max(targetX, targetZ);
        float scale = (maxTargetSize / maxModelSize) * data.scaleMultiplier;

        obj.transform.localScale = Vector3.one * scale;
    }

    private Vector3 GetWorldPosition(Vector2Int cell, int sx, int sz)
    {
        Vector3 origin = gridManager.GridOrigin;
        float x = origin.x + cell.x * cellSize + (sx * cellSize) * 0.5f;
        float z = origin.z + cell.y * cellSize + (sz * cellSize) * 0.5f;

        Terrain t = gridManager.terrain;
        if (t == null) t = Terrain.activeTerrain;

        float y;
        if (t != null)
        {
            y = t.SampleHeight(new Vector3(x, 0, z)) + t.transform.position.y + debugYOffset;
        }
        else
        {
            y = origin.y;
        }

        return new Vector3(x, y, z);
    }

    // ──────────────────────────────────────────────
    //  Occupancy Helpers
    // ──────────────────────────────────────────────

    private bool IsAreaFree(Vector2Int start, int sx, int sz)
    {
        for (int x = start.x; x < start.x + sx; x++)
            for (int z = start.y; z < start.y + sz; z++)
                if (x < 0 || x >= gridWidth || z < 0 || z >= gridHeight || occupiedMap[x, z])
                    return false;
        return true;
    }

    private void MarkOccupied(Vector2Int start, int sx, int sz)
    {
        for (int x = start.x; x < start.x + sx; x++)
            for (int z = start.y; z < start.y + sz; z++)
                if (x >= 0 && x < gridWidth && z >= 0 && z < gridHeight)
                    occupiedMap[x, z] = true;
    }

    // ──────────────────────────────────────────────
    //  Runtime — Mark Existing Scene Objects
    // ──────────────────────────────────────────────

    /// <summary>
    /// At runtime, scans all children (placed in Scene view) and marks
    /// their grid cells as permanently occupied so the building placer
    /// knows those cells are taken.
    /// </summary>
    private void MarkExistingBuildingsAtRuntime()
    {
        CacheGridSettings();
        Vector3 origin = gridManager.GridOrigin;

        foreach (Transform child in transform)
        {
            Renderer[] renderers = child.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) continue;

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                b.Encapsulate(renderers[i].bounds);

            int minX = Mathf.FloorToInt((b.min.x - origin.x) / cellSize);
            int minZ = Mathf.FloorToInt((b.min.z - origin.z) / cellSize);
            int maxX = Mathf.CeilToInt((b.max.x - origin.x) / cellSize);
            int maxZ = Mathf.CeilToInt((b.max.z - origin.z) / cellSize);

            minX = Mathf.Max(minX, 0);
            minZ = Mathf.Max(minZ, 0);
            maxX = Mathf.Min(maxX, gridWidth);
            maxZ = Mathf.Min(maxZ, gridHeight);

            for (int x = minX; x < maxX; x++)
                for (int z = minZ; z < maxZ; z++)
                    gridManager.OccupyCellsPermanent(new Vector2Int(x, z), 1);
        }

        Debug.Log($"[CityGenerator] Marked {transform.childCount} buildings as permanent at runtime.");
    }

    // ──────────────────────────────────────────────
    //  Utility
    // ──────────────────────────────────────────────

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    private void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, layer);
    }
}
