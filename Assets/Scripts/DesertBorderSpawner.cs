using UnityEngine;

/// <summary>
/// Spawns a desert border around the playable grid.
/// Covers the border floor with sandy/yellow tiles and scatters
/// very sparse desert elements (rocks, dried trees, cacti, etc.).
/// Auto-detects the border by comparing terrain size to grid size.
/// All spawned objects are non-interactable (decorative only).
/// </summary>
public class DesertBorderSpawner : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;

    [Header("Desert Element Prefabs")]
    [Tooltip("Drag rock, dried tree, cactus, or other desert prefabs here. " +
             "A random one is picked per placement.")]
    public GameObject[] desertPrefabs;

    [Header("Sand Tile Settings")]
    [Tooltip("Color of the desert sand tiles.")]
    public Color sandColor = new Color(0.88f, 0.80f, 0.52f, 1f);
    [Tooltip("Slight color variation per tile for a natural look.")]
    public float sandColorVariation = 0.06f;
    [Tooltip("Height/thickness of sand tiles.")]
    public float sandTileHeight = 0.12f;

    [Header("Element Density")]
    [Tooltip("Chance (0-1) that any given cell spawns a desert element. " +
             "Keep very low for a barren desert look.")]
    [Range(0f, 0.3f)]
    public float elementChance = 0.06f;

    [Header("Element Spacing")]
    [Tooltip("Minimum spacing multiplier in cells between desert elements. " +
             "Higher = more spread apart.")]
    [Range(2f, 10f)]
    public float elementSpacing = 4f;
    [Tooltip("Minimum world-space distance between any two desert elements.")]
    public float minimumElementDistance = 6f;

    [Header("Element Variation")]
    [Tooltip("Random scale range for desert elements (min, max).")]
    public Vector2 elementScaleRange = new Vector2(0.6f, 1.5f);
    [Tooltip("Random position jitter within each cell (0 = centered, 1 = full cell).")]
    [Range(0f, 0.5f)]
    public float positionJitter = 0.3f;

    [Header("General")]
    public bool spawnOnStart = true;
    public int randomSeed = 0;

    void Start()
    {
        if (spawnOnStart)
            SpawnDesert();
    }

    public void SpawnDesert()
    {
        if (gridManager == null)
        {
            Debug.LogError("[DesertBorderSpawner] No GridManager assigned!");
            return;
        }

        Terrain terrain = gridManager.terrain;
        if (terrain == null || terrain.terrainData == null)
        {
            Debug.LogError("[DesertBorderSpawner] No terrain found on GridManager!");
            return;
        }

        float cellSize = gridManager.cellSize;
        int gridW = gridManager.gridWidth;
        int gridH = gridManager.gridHeight;

        float terrainSizeX = terrain.terrainData.size.x;
        float terrainSizeZ = terrain.terrainData.size.z;
        float gridSizeX = gridW * cellSize;
        float gridSizeZ = gridH * cellSize;

        int borderX = Mathf.Max(0, Mathf.FloorToInt((terrainSizeX - gridSizeX) / (2f * cellSize)));
        int borderZ = Mathf.Max(0, Mathf.FloorToInt((terrainSizeZ - gridSizeZ) / (2f * cellSize)));

        if (borderX <= 0 && borderZ <= 0)
        {
            Debug.LogWarning("[DesertBorderSpawner] Terrain is same size as grid — no border to fill.");
            return;
        }

        ClearDesert();

        int seed = (randomSeed != 0) ? randomSeed : System.Environment.TickCount;
        Random.InitState(seed);

        Vector3 terrainOrigin = terrain.transform.position;

        int totalW = gridW + borderX * 2;
        int totalH = gridH + borderZ * 2;

        GameObject parent = new GameObject("DesertBorder");
        parent.transform.SetParent(transform);

        int sandCount = 0;
        int elementCount = 0;

        // Track placed element positions for minimum distance enforcement
        var placedPositions = new System.Collections.Generic.List<Vector3>();

        // Step size for element placement (sand tiles cover every cell regardless)
        int elementStep = Mathf.Max(2, Mathf.RoundToInt(elementSpacing));

        // ── Pass 1: Lay sand tiles on every border cell ──
        for (int tx = 0; tx < totalW; tx++)
        {
            for (int tz = 0; tz < totalH; tz++)
            {
                bool insideGrid = tx >= borderX && tx < borderX + gridW &&
                                  tz >= borderZ && tz < borderZ + gridH;
                if (insideGrid) continue;

                Vector3 cellPos = GetCellWorldPos(terrain, terrainOrigin, cellSize, tx, tz);
                SpawnSandTile(cellPos, cellSize, parent.transform);
                sandCount++;
            }
        }

        // ── Pass 2: Scatter sparse desert elements ──
        if (desertPrefabs != null && desertPrefabs.Length > 0)
        {
            for (int tx = 0; tx < totalW; tx += elementStep)
            {
                for (int tz = 0; tz < totalH; tz += elementStep)
                {
                    bool insideGrid = tx >= borderX && tx < borderX + gridW &&
                                      tz >= borderZ && tz < borderZ + gridH;
                    if (insideGrid) continue;

                    // Random chance — most cells stay empty for barren look
                    if (Random.value > elementChance)
                        continue;

                    float jitterX = Random.Range(-0.5f, 0.5f) * positionJitter * cellSize;
                    float jitterZ = Random.Range(-0.5f, 0.5f) * positionJitter * cellSize;

                    float worldX = terrainOrigin.x + (tx + 0.5f) * cellSize + jitterX;
                    float worldZ = terrainOrigin.z + (tz + 0.5f) * cellSize + jitterZ;
                    float worldY = terrain.SampleHeight(new Vector3(worldX, 0, worldZ)) + terrainOrigin.y;

                    Vector3 pos = new Vector3(worldX, worldY, worldZ);

                    // Enforce minimum distance between elements
                    bool tooClose = false;
                    for (int i = 0; i < placedPositions.Count; i++)
                    {
                        if (Vector3.Distance(pos, placedPositions[i]) < minimumElementDistance)
                        {
                            tooClose = true;
                            break;
                        }
                    }
                    if (tooClose) continue;

                    GameObject prefab = desertPrefabs[Random.Range(0, desertPrefabs.Length)];
                    if (prefab == null) continue;

                    float scale = Random.Range(elementScaleRange.x, elementScaleRange.y);
                    SpawnDecor(prefab, pos, parent.transform, scale);

                    placedPositions.Add(pos);
                    elementCount++;
                }
            }
        }

        Debug.Log($"[DesertBorderSpawner] Spawned desert: {sandCount} sand tiles, {elementCount} elements (seed={seed}).");
    }

    // ═══════════════════════════════════════════
    //  SAND TILES
    // ═══════════════════════════════════════════

    private void SpawnSandTile(Vector3 position, float cellSize, Transform parent)
    {
        GameObject sandObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sandObj.name = "DesertSand";
        sandObj.transform.SetParent(parent);
        sandObj.transform.position = position + new Vector3(0, sandTileHeight * 0.5f, 0);
        sandObj.transform.localScale = new Vector3(cellSize, sandTileHeight, cellSize);

        // Apply sand color with subtle per-tile variation
        Renderer renderer = sandObj.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material sandMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));

            float variation = Random.Range(-sandColorVariation, sandColorVariation);
            Color tileColor = new Color(
                Mathf.Clamp01(sandColor.r + variation),
                Mathf.Clamp01(sandColor.g + variation * 0.8f),
                Mathf.Clamp01(sandColor.b + variation * 0.5f),
                1f
            );

            sandMat.color = tileColor;
            sandMat.SetFloat("_Smoothness", 0.05f);
            renderer.material = sandMat;
        }

        // Remove collider, set non-interactable
        Collider col = sandObj.GetComponent<Collider>();
        if (col != null)
        {
            if (Application.isPlaying) Destroy(col);
            else DestroyImmediate(col);
        }
        sandObj.layer = LayerMask.NameToLayer("Ignore Raycast");
    }

    // ═══════════════════════════════════════════
    //  DESERT ELEMENTS (rocks, dried trees, etc.)
    // ═══════════════════════════════════════════

    private void SpawnDecor(GameObject prefab, Vector3 position, Transform parent, float scale)
    {
        float yRot = Random.Range(0f, 360f);
        Quaternion rot = Quaternion.Euler(0f, yRot, 0f);

        GameObject obj;
        #if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            obj = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent);
            obj.transform.position = position;
            obj.transform.rotation = rot;
        }
        else
        #endif
        {
            obj = Instantiate(prefab, position, rot, parent);
        }

        obj.transform.localScale = Vector3.one * scale;

        // Make non-interactable
        SetLayerRecursive(obj, LayerMask.NameToLayer("Ignore Raycast"));
        foreach (Collider c in obj.GetComponentsInChildren<Collider>())
            c.enabled = false;
    }

    // ═══════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════

    private Vector3 GetCellWorldPos(Terrain terrain, Vector3 terrainOrigin, float cellSize, int tx, int tz)
    {
        float worldX = terrainOrigin.x + (tx + 0.5f) * cellSize;
        float worldZ = terrainOrigin.z + (tz + 0.5f) * cellSize;
        float worldY = terrain.SampleHeight(new Vector3(worldX, 0, worldZ)) + terrainOrigin.y;
        return new Vector3(worldX, worldY, worldZ);
    }

    public void ClearDesert()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            if (child.name == "DesertBorder")
            {
                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }
        }
    }

    private void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, layer);
    }
}
