using UnityEngine;

public enum RiverDirection { Random, NorthSouth, EastWest }

/// <summary>
/// Generates a straight-line road network on a grid, creating city blocks.
/// Roads are 2x2 cells each. Blocks are filled with non-blocking empty space.
/// Optionally generates river and mountain obstacles before roads.
/// Attach to an empty GameObject in the scene and assign references in Inspector.
/// </summary>
public class RoadGenerator : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;

    [Header("Road Tiles (2x2 each — assign CityTileData assets)")]
    public CityTileData roadStart;
    public CityTileData roadEnd;
    public CityTileData roadFiller1;
    public CityTileData roadFiller2;
    public CityTileData roadFiller3;
    public CityTileData roadFiller4;
    public CityTileData roadFiller5;
    public CityTileData roadIntersection;
    public CityTileData roadBranch;

    [Header("Fill Tile (non-blocking)")]
    public CityTileData emptySpace;

    [Header("Environment — River")]
    public bool generateRiver = false;
    public RiverDirection riverDirection = RiverDirection.Random;
    [Tooltip("Optional prefab for river visuals. Leave empty for default blue placeholder.")]
    public GameObject riverPrefab;

    [Header("Environment — Mountains")]
    public bool generateMountains = false;
    [Range(0, 5)] public int mountainCount = 1;
    [Tooltip("Optional prefab for mountain visuals. Leave empty for default brown placeholder.")]
    public GameObject mountainPrefab;

    [Header("Road Network")]
    [Range(3, 12)] public int horizontalRoads = 5;
    [Range(3, 12)] public int verticalRoads = 5;
    [Tooltip("Max random offset per road (in 2-cell blocks). Keeps block sizes realistic.")]
    [Range(0, 2)] public int roadJitter = 1;

    [Header("Border Margin (cells from grid edge with no roads)")]
    [Range(2, 16)] public int borderMargin = 2;

    [Header("General")]
    public int randomSeed = 0;
    public bool fillEmptySpace = true;

    [Header("Debug")]
    [Tooltip("Extra Y offset above terrain for visibility testing. Set to 0 for production.")]
    public float debugYOffset = 0.15f;
    [Tooltip("Read-only. Shows the uniform scale applied to all road tiles (computed from Filler1).")]
    [SerializeField] private float lastComputedScale;

    // Internal state
    private bool[,] roadMap;      // true = road cell
    private bool[,] placedMap;    // true = prefab already placed here
    private bool[,] obstacleMap;  // true = river or mountain cell (roads cannot use)
    private int gridWidth, gridHeight;
    private float cellSize;
    private float referenceScale; // scale computed from filler1, applied to all road tiles

    void Start()
    {
        // Generation is done in Scene view via the Editor button.
        // At runtime, just mark existing obstacle cells as permanent
        // so the grid knows they are occupied.
        if (gridManager != null)
            MarkExistingObstaclesAtRuntime();
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

        // Environment obstacles first — roads will avoid these cells
        if (generateRiver)
            GenerateRiver();
        if (generateMountains)
            GenerateMountains();

        GenerateRoadNetwork();
        ComputeReferenceScale();
        InstantiateRoadTiles();

        if (fillEmptySpace)
            FillEmptySpace();

        Debug.Log($"[RoadGenerator] Done. Road cells: {CountRoadCells()}, Grid: {gridWidth}x{gridHeight}");
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
        roadMap = new bool[gridWidth, gridHeight];
        placedMap = new bool[gridWidth, gridHeight];
        obstacleMap = new bool[gridWidth, gridHeight];
    }

    /// <summary>
    /// Computes scale factor from roadFiller1 so all road tiles match its size.
    /// </summary>
    private void ComputeReferenceScale()
    {
        referenceScale = 1f;
        if (roadFiller1 == null) return;
        GameObject prefab = roadFiller1.GetRandomPrefab();
        if (prefab == null) return;

        GameObject temp = Instantiate(prefab);
        Renderer[] renderers = temp.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                b.Encapsulate(renderers[i].bounds);

            float currentSize = Mathf.Max(b.size.x, b.size.z);
            if (currentSize > 0.01f)
                referenceScale = (2f * cellSize) / currentSize;
        }
        DestroyImmediate(temp);
        lastComputedScale = referenceScale;
    }

    /// <summary>
    /// Logs the native bounds size and applied scale for every assigned road prefab.
    /// Call from a custom editor button or context menu.
    /// </summary>
    [ContextMenu("Log Prefab Scales")]
    public void LogPrefabScales()
    {
        float cs = gridManager != null && gridManager.cellSize > 0f ? gridManager.cellSize : 2.5f;
        float targetSize = 2f * cs;

        CityTileData[] tiles = { roadStart, roadEnd, roadFiller1, roadFiller2, roadFiller3, roadFiller4, roadFiller5, roadIntersection, roadBranch };
        string[] names = { "Start", "End", "Filler1", "Filler2", "Filler3", "Filler4", "Filler5", "Intersection", "Branch" };

        Debug.Log($"[RoadGenerator] Target tile size: {targetSize} world units (2x2 cells @ {cs})");

        for (int i = 0; i < tiles.Length; i++)
        {
            if (tiles[i] == null) { Debug.Log($"  {names[i]}: NOT ASSIGNED"); continue; }
            GameObject prefab = tiles[i].GetRandomPrefab();
            if (prefab == null) { Debug.Log($"  {names[i]}: NO PREFAB"); continue; }

            GameObject temp = Instantiate(prefab);
            Renderer[] renderers = temp.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                for (int j = 1; j < renderers.Length; j++)
                    b.Encapsulate(renderers[j].bounds);

                float nativeSize = Mathf.Max(b.size.x, b.size.z);
                float scale = nativeSize > 0.01f ? targetSize / nativeSize : 1f;
                Debug.Log($"  {names[i]}: native bounds {b.size.x:F2} x {b.size.z:F2}, max={nativeSize:F2}, scale={scale:F4}");
            }
            else
            {
                Debug.Log($"  {names[i]}: NO RENDERERS");
            }
            DestroyImmediate(temp);
        }

        Debug.Log($"  Reference scale (from Filler1, applied to all): {lastComputedScale:F4}");
    }

    // ──────────────────────────────────────────────
    //  Environment — River & Mountains
    // ──────────────────────────────────────────────

    private void GenerateRiver()
    {
        int riverWidth = 6;

        // Decide direction
        bool northSouth;
        if (riverDirection == RiverDirection.NorthSouth)
            northSouth = true;
        else if (riverDirection == RiverDirection.EastWest)
            northSouth = false;
        else
            northSouth = Random.value < 0.5f;

        if (northSouth)
        {
            // River runs along Z axis (full height), 6 cells wide in X
            int maxStart = gridWidth - riverWidth;
            int startX = Random.Range(borderMargin, maxStart - borderMargin + 1);

            // Mark obstacle cells
            for (int x = startX; x < startX + riverWidth; x++)
                for (int z = 0; z < gridHeight; z++)
                    if (InBounds(x, z))
                    {
                        obstacleMap[x, z] = true;
                        placedMap[x, z] = true;
                    }

            // Create placeholder visual
            CreateRiverPlaceholder(startX, 0, riverWidth, gridHeight, northSouth);
            Debug.Log($"[RoadGenerator] River: North-South at X={startX}, width={riverWidth}");
        }
        else
        {
            // River runs along X axis (full width), 6 cells wide in Z
            int maxStart = gridHeight - riverWidth;
            int startZ = Random.Range(borderMargin, maxStart - borderMargin + 1);

            for (int x = 0; x < gridWidth; x++)
                for (int z = startZ; z < startZ + riverWidth; z++)
                    if (InBounds(x, z))
                    {
                        obstacleMap[x, z] = true;
                        placedMap[x, z] = true;
                    }

            CreateRiverPlaceholder(0, startZ, gridWidth, riverWidth, northSouth);
            Debug.Log($"[RoadGenerator] River: East-West at Z={startZ}, width={riverWidth}");
        }

        // Mark cells permanent at runtime
        if (Application.isPlaying)
        {
            for (int x = 0; x < gridWidth; x++)
                for (int z = 0; z < gridHeight; z++)
                    if (obstacleMap[x, z])
                        gridManager.OccupyCellsPermanent(new Vector2Int(x, z), 1);
        }
    }

    private void CreateRiverPlaceholder(int cellX, int cellZ, int widthCells, int heightCells, bool northSouth)
    {
        Vector3 origin = gridManager.GridOrigin;

        float worldX = origin.x + cellX * cellSize + (widthCells * cellSize) * 0.5f;
        float worldZ = origin.z + cellZ * cellSize + (heightCells * cellSize) * 0.5f;

        Terrain t = gridManager.terrain;
        if (t == null) t = Terrain.activeTerrain;
        float terrainBaseY = (t != null) ? t.transform.position.y : origin.y;
        float worldY = terrainBaseY + 0.05f; // Sit just above terrain

        Vector3 pos = new Vector3(worldX, worldY, worldZ);
        Vector3 targetScale = new Vector3(
            widthCells * cellSize,
            0.02f,
            heightCells * cellSize
        );

        if (riverPrefab != null)
        {
            // Use custom prefab — scale it to fit the river area
            GameObject river = Instantiate(riverPrefab, pos, Quaternion.identity, transform);
            river.name = "River_Custom";

            Renderer[] renderers = river.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    b.Encapsulate(renderers[i].bounds);

                Vector3 nativeSize = b.size;
                float scaleX = nativeSize.x > 0.01f ? targetScale.x / nativeSize.x : 1f;
                float scaleZ = nativeSize.z > 0.01f ? targetScale.z / nativeSize.z : 1f;
                Vector3 orig = river.transform.localScale;
                float scaleY = nativeSize.y > 0.01f ? targetScale.y / nativeSize.y : 1f;
                river.transform.localScale = new Vector3(orig.x * scaleX, orig.y * scaleY, orig.z * scaleZ);

                // Bounds-based centering
                Vector3 offset = b.center - pos;
                offset.x *= scaleX;
                offset.z *= scaleZ;
                offset.y = 0f;
                river.transform.position -= offset;
            }

            Collider col = river.GetComponentInChildren<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }

            SetLayerRecursive(river, LayerMask.NameToLayer("Ignore Raycast"));
        }
        else
        {
            // Default placeholder cube
            GameObject river = GameObject.CreatePrimitive(PrimitiveType.Cube);
            river.name = "River_Placeholder";
            river.transform.SetParent(transform);
            river.transform.position = pos;
            river.transform.localScale = targetScale;

            Renderer rend = river.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetFloat("_Surface", 1);
            mat.SetFloat("_Blend", 0);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
            mat.color = new Color(0.1f, 0.4f, 0.9f, 0.6f);
            rend.material = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            Collider col = river.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }

            SetLayerRecursive(river, LayerMask.NameToLayer("Ignore Raycast"));
        }
    }

    private void GenerateMountains()
    {
        int mountainSize = 20;

        for (int m = 0; m < mountainCount; m++)
        {
            // Try to find a valid position that doesn't overlap obstacles
            int maxAttempts = 100;
            bool placed = false;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                int startX = Random.Range(borderMargin, gridWidth - borderMargin - mountainSize);
                int startZ = Random.Range(borderMargin, gridHeight - borderMargin - mountainSize);

                // Check for overlap with existing obstacles
                bool overlaps = false;
                for (int x = startX; x < startX + mountainSize && !overlaps; x++)
                    for (int z = startZ; z < startZ + mountainSize && !overlaps; z++)
                        if (obstacleMap[x, z])
                            overlaps = true;

                if (overlaps) continue;

                // Mark cells
                for (int x = startX; x < startX + mountainSize; x++)
                    for (int z = startZ; z < startZ + mountainSize; z++)
                    {
                        obstacleMap[x, z] = true;
                        placedMap[x, z] = true;
                    }

                // Mark permanent at runtime
                if (Application.isPlaying)
                    gridManager.OccupyCellsPermanent(new Vector2Int(startX, startZ), mountainSize);

                CreateMountainPlaceholder(startX, startZ, mountainSize);
                Debug.Log($"[RoadGenerator] Mountain {m + 1}: at cell ({startX},{startZ}), size {mountainSize}x{mountainSize}");
                placed = true;
                break;
            }

            if (!placed)
                Debug.LogWarning($"[RoadGenerator] Could not place mountain {m + 1} — no valid position found after {maxAttempts} attempts.");
        }
    }

    private void CreateMountainPlaceholder(int cellX, int cellZ, int size)
    {
        Vector3 origin = gridManager.GridOrigin;

        float worldX = origin.x + cellX * cellSize + (size * cellSize) * 0.5f;
        float worldZ = origin.z + cellZ * cellSize + (size * cellSize) * 0.5f;

        Terrain t = gridManager.terrain;
        if (t == null) t = Terrain.activeTerrain;
        float worldY = (t != null) ? t.transform.position.y + debugYOffset : origin.y;

        float worldSize = size * cellSize;

        if (mountainPrefab != null)
        {
            // Use custom prefab — scale it to fit the mountain area
            Vector3 pos = new Vector3(worldX, worldY, worldZ);
            GameObject mountain = Instantiate(mountainPrefab, pos, Quaternion.identity, transform);
            mountain.name = $"Mountain_Custom_{cellX}_{cellZ}";

            Renderer[] renderers = mountain.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    b.Encapsulate(renderers[i].bounds);

                Vector3 nativeSize = b.size;
                float scaleXZ = nativeSize.x > 0.01f ? worldSize / Mathf.Max(nativeSize.x, nativeSize.z) : 1f;
                float scaleY = nativeSize.y > 0.01f ? (worldSize * 0.5f) / nativeSize.y : 1f;
                mountain.transform.localScale = new Vector3(scaleXZ, scaleY, scaleXZ);

                // Bounds-based centering
                Vector3 offset = b.center - pos;
                offset.x *= scaleXZ;
                offset.z *= scaleXZ;
                offset.y = 0f;
                mountain.transform.position -= offset;
            }

            Collider col = mountain.GetComponentInChildren<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }

            SetLayerRecursive(mountain, LayerMask.NameToLayer("Ignore Raycast"));
        }
        else
        {
            // Default placeholder cube
            GameObject mountain = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mountain.name = $"Mountain_Placeholder_{cellX}_{cellZ}";
            mountain.transform.SetParent(transform);
            mountain.transform.position = new Vector3(worldX, worldY + worldSize * 0.25f, worldZ);
            mountain.transform.localScale = new Vector3(worldSize, worldSize * 0.5f, worldSize);

            // Brown/gray material
            Renderer rend = mountain.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.45f, 0.35f, 0.25f, 1f);
            rend.material = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

            Collider col = mountain.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }

            SetLayerRecursive(mountain, LayerMask.NameToLayer("Ignore Raycast"));
        }
    }

    // ──────────────────────────────────────────────
    //  Road Network — straight lines with jitter
    // ──────────────────────────────────────────────

    private void GenerateRoadNetwork()
    {
        int minX = borderMargin;
        int maxX = gridWidth - borderMargin;
        int minZ = borderMargin;
        int maxZ = gridHeight - borderMargin;

        int regionW = maxX - minX;
        int regionH = maxZ - minZ;

        // Horizontal roads (run along X axis)
        for (int i = 0; i < horizontalRoads; i++)
        {
            int baseZ = minZ + Mathf.RoundToInt((i + 0.5f) * regionH / horizontalRoads);
            baseZ += Random.Range(-roadJitter, roadJitter + 1) * 2;
            baseZ = Mathf.Clamp(baseZ, minZ, maxZ - 2);
            baseZ = AlignToEven(baseZ);

            int startX, endX;
            GetRoadExtent(minX, maxX, i, horizontalRoads, out startX, out endX);
            startX = AlignToEven(startX);
            endX = AlignToEven(endX);

            for (int x = startX; x < endX; x += 2)
                PaintBlock(x, baseZ);
        }

        // Vertical roads (run along Z axis)
        for (int i = 0; i < verticalRoads; i++)
        {
            int baseX = minX + Mathf.RoundToInt((i + 0.5f) * regionW / verticalRoads);
            baseX += Random.Range(-roadJitter, roadJitter + 1) * 2;
            baseX = Mathf.Clamp(baseX, minX, maxX - 2);
            baseX = AlignToEven(baseX);

            int startZ, endZ;
            GetRoadExtent(minZ, maxZ, i, verticalRoads, out startZ, out endZ);
            startZ = AlignToEven(startZ);
            endZ = AlignToEven(endZ);

            for (int z = startZ; z < endZ; z += 2)
                PaintBlock(baseX, z);
        }
    }

    private void GetRoadExtent(int regionMin, int regionMax, int index, int totalRoads, out int start, out int end)
    {
        start = regionMin;
        end = regionMax;

        // ~15% chance to trim an inner road
        if (index > 0 && index < totalRoads - 1 && Random.value < 0.15f)
        {
            int regionSpan = regionMax - regionMin;
            int trim = Random.Range(2, regionSpan / 5);
            trim = AlignToEven(trim);

            if (Random.value < 0.5f)
                start += trim;
            else
                end -= trim;
        }
    }

    private void PaintBlock(int x, int z)
    {
        // Check if any cell in this 2x2 block is an obstacle
        for (int dx = 0; dx < 2; dx++)
            for (int dz = 0; dz < 2; dz++)
            {
                int cx = x + dx;
                int cz = z + dz;
                if (!InBounds(cx, cz) || obstacleMap[cx, cz])
                    return; // skip entire block
            }

        for (int dx = 0; dx < 2; dx++)
            for (int dz = 0; dz < 2; dz++)
            {
                int cx = x + dx;
                int cz = z + dz;
                if (InBounds(cx, cz))
                    roadMap[cx, cz] = true;
            }
    }

    // ──────────────────────────────────────────────
    //  Road Tile Classification & Instantiation
    // ──────────────────────────────────────────────

    private void InstantiateRoadTiles()
    {
        bool placedAnyStart = false;

        for (int z = 0; z < gridHeight - 1; z += 2)
        {
            for (int x = 0; x < gridWidth - 1; x += 2)
            {
                if (!IsRoadBlock(x, z))
                    continue;
                if (placedMap[x, z])
                    continue;

                // Check 4 cardinal neighbors (adjacent 2x2 blocks)
                bool hasN = IsRoadAt(x, z + 2);
                bool hasS = IsRoadAt(x, z - 1);
                bool hasE = IsRoadAt(x + 2, z);
                bool hasW = IsRoadAt(x - 1, z);

                int neighbors = (hasN ? 1 : 0) + (hasS ? 1 : 0) + (hasE ? 1 : 0) + (hasW ? 1 : 0);

                CityTileData tile;
                float rot = 0f;

                switch (neighbors)
                {
                    case 0:
                        tile = roadStart;
                        break;

                    case 1:
                        // Dead end — bias roadStart near edges
                        if (!placedAnyStart || IsNearEdge(x, z))
                        {
                            tile = roadStart;
                            placedAnyStart = true;
                            // roadStart: rotated back 90 from aligned
                            if (hasN) rot = 270f;
                            else if (hasE) rot = 0f;
                            else if (hasS) rot = 90f;
                            else rot = 180f;
                        }
                        else
                        {
                            tile = roadEnd;
                            // roadEnd: cap facing away from neighbor
                            if (hasN) rot = 180f;
                            else if (hasE) rot = 270f;
                            else if (hasS) rot = 0f;
                            else rot = 90f;
                        }
                        break;

                    case 2:
                        if ((hasN && hasS) || (hasE && hasW))
                        {
                            // Straight segment
                            tile = GetRandomFiller();
                            rot = (hasE && hasW) ? 90f : 0f;
                        }
                        else
                        {
                            // Corner — use branch tile
                            tile = roadBranch;
                            if (hasN && hasE) rot = 0f;
                            else if (hasE && hasS) rot = 90f;
                            else if (hasS && hasW) rot = 180f;
                            else rot = 270f; // N+W
                        }
                        break;

                    case 3:
                        // T-junction — rotate so the missing side is correct
                        tile = roadBranch;
                        if (!hasS) rot = 0f;
                        else if (!hasW) rot = 90f;
                        else if (!hasN) rot = 180f;
                        else rot = 270f; // missing E
                        break;

                    case 4:
                        tile = roadIntersection;
                        rot = 0f;
                        break;

                    default:
                        tile = GetRandomFiller();
                        break;
                }

                Vector2Int cell = new Vector2Int(x, z);
                InstantiateTile(cell, tile, rot, blockCells: true, tileSize: 2);
                MarkPlaced(x, z);
            }
        }
    }

    // ──────────────────────────────────────────────
    //  Tile Instantiation — with bounds-based centering & scaling
    // ──────────────────────────────────────────────

    private void InstantiateTile(Vector2Int cell, CityTileData tileData, float rotationY, bool blockCells, int tileSize)
    {
        if (tileData == null || tileData.GetRandomPrefab() == null)
        {
            Debug.LogWarning($"[RoadGenerator] Missing tile/prefab at cell {cell}");
            return;
        }

        GameObject prefab = tileData.GetRandomPrefab();
        Vector3 targetPos = GetWorldPosition(cell, tileSize);
        Quaternion rotation = Quaternion.Euler(0f, rotationY, 0f);

        // Instantiate (always clone, never PrefabUtility, so we can modify transforms)
        GameObject instance = Instantiate(prefab, targetPos, rotation, transform);

        // --- Bounds-based centering + uniform scale from filler1 ---
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            // Apply the same scale as filler1 to all road tiles
            instance.transform.localScale = Vector3.one * referenceScale;

            // After scaling, the offset from targetPos also scales
            Vector3 offset = (bounds.center - targetPos) * referenceScale;
            offset.y = 0f;
            instance.transform.position -= offset;
        }

        instance.name = $"{tileData.tileName}_{cell.x}_{cell.y}";
        SetLayerRecursive(instance, LayerMask.NameToLayer("Ignore Raycast"));

        if (blockCells && Application.isPlaying)
            gridManager.OccupyCellsPermanent(cell, tileSize);
    }

    // ──────────────────────────────────────────────
    //  Fill empty blocks with non-blocking space
    // ──────────────────────────────────────────────

    private void FillEmptySpace()
    {
        if (emptySpace == null) return;

        int fillSize = emptySpace.sizeInCells;
        if (fillSize < 1) fillSize = 1;

        for (int z = 0; z < gridHeight; z += fillSize)
        {
            for (int x = 0; x < gridWidth; x += fillSize)
            {
                bool blocked = false;
                for (int dx = 0; dx < fillSize && !blocked; dx++)
                    for (int dz = 0; dz < fillSize && !blocked; dz++)
                    {
                        int cx = x + dx, cz = z + dz;
                        if (!InBounds(cx, cz) || roadMap[cx, cz] || placedMap[cx, cz] || obstacleMap[cx, cz])
                            blocked = true;
                    }

                if (blocked) continue;

                Vector2Int cell = new Vector2Int(x, z);
                InstantiateTile(cell, emptySpace, 0f, blockCells: false, tileSize: fillSize);

                for (int dx = 0; dx < fillSize; dx++)
                    for (int dz = 0; dz < fillSize; dz++)
                    {
                        int cx = x + dx, cz = z + dz;
                        if (InBounds(cx, cz))
                            placedMap[cx, cz] = true;
                    }
            }
        }
    }

    // ──────────────────────────────────────────────
    //  World Position & Terrain Height
    // ──────────────────────────────────────────────

    private Vector3 GetWorldPosition(Vector2Int cell, int size)
    {
        Vector3 origin = gridManager.GridOrigin;
        float cs = cellSize;

        float x = origin.x + cell.x * cs + (size * cs) * 0.5f;
        float z = origin.z + cell.y * cs + (size * cs) * 0.5f;

        Terrain t = gridManager.terrain;
        if (t == null) t = Terrain.activeTerrain;

        float y;
        if (t != null)
        {
            float yMax = t.SampleHeight(new Vector3(x, 0, z));

            if (size > 1)
            {
                float hs = (size * cs) * 0.5f - 0.1f;
                yMax = Mathf.Max(yMax, t.SampleHeight(new Vector3(x - hs, 0, z - hs)));
                yMax = Mathf.Max(yMax, t.SampleHeight(new Vector3(x + hs, 0, z - hs)));
                yMax = Mathf.Max(yMax, t.SampleHeight(new Vector3(x - hs, 0, z + hs)));
                yMax = Mathf.Max(yMax, t.SampleHeight(new Vector3(x + hs, 0, z + hs)));
            }

            y = yMax + t.transform.position.y + debugYOffset;
        }
        else
        {
            y = origin.y;
        }

        return new Vector3(x, y, z);
    }

    // ──────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────

    private bool IsRoadBlock(int x, int z)
    {
        return InBounds(x, z) && InBounds(x + 1, z + 1)
            && roadMap[x, z] && roadMap[x + 1, z]
            && roadMap[x, z + 1] && roadMap[x + 1, z + 1];
    }

    private bool IsRoadAt(int x, int z)
    {
        return InBounds(x, z) && roadMap[x, z];
    }

    private bool InBounds(int x, int z)
    {
        return x >= 0 && x < gridWidth && z >= 0 && z < gridHeight;
    }

    private bool IsNearEdge(int x, int z)
    {
        int threshold = borderMargin + 6;
        return x < threshold || x > gridWidth - threshold
            || z < threshold || z > gridHeight - threshold;
    }

    private void MarkPlaced(int x, int z)
    {
        for (int dx = 0; dx < 2; dx++)
            for (int dz = 0; dz < 2; dz++)
                if (InBounds(x + dx, z + dz))
                    placedMap[x + dx, z + dz] = true;
    }

    /// <summary>
    /// Weighted random filler. Filler1 and Filler2 appear ~3x more often.
    /// Weights: f1=3, f2=3, f3=1, f4=1, f5=1 (total 9)
    /// </summary>
    private CityTileData GetRandomFiller()
    {
        int roll = Random.Range(0, 9);
        if (roll < 3) return roadFiller1;
        if (roll < 6) return roadFiller2;
        if (roll < 7) return roadFiller3;
        if (roll < 8) return roadFiller4;
        return roadFiller5;
    }

    private int AlignToEven(int v)
    {
        return (v / 2) * 2;
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

    /// <summary>
    /// At runtime, scans all children (placed in Scene view) and marks
    /// their grid cells as permanently occupied so the building placer
    /// knows those cells are taken.
    /// </summary>
    private void MarkExistingObstaclesAtRuntime()
    {
        CacheGridSettings();
        Vector3 origin = gridManager.GridOrigin;
        string emptyName = emptySpace != null ? emptySpace.tileName : "";

        foreach (Transform child in transform)
        {
            // Skip empty space filler tiles — they are non-blocking
            if (!string.IsNullOrEmpty(emptyName) && child.name.StartsWith(emptyName))
                continue;

            Renderer[] renderers = child.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) continue;

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                b.Encapsulate(renderers[i].bounds);

            // Convert world bounds to grid cell range
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

        Debug.Log($"[RoadGenerator] Marked existing scene objects as permanent ({transform.childCount} children).");
    }
}
