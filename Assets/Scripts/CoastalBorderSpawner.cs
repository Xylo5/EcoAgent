using UnityEngine;

/// <summary>
/// Spawns a coastal/beach border for Level_2.
/// South side (high Z, near camera start): ocean water plane + sandy beach + rocks.
/// Left/right sides: gradual transition from sand/rocks (south) to forest (north).
/// North side (low Z, far from camera): full forest (trees only).
///
/// Attach to a GameObject in the scene. Requires GridManager reference.
/// Assign tree prefabs and rock prefabs in Inspector.
/// </summary>
public class CoastalBorderSpawner : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;

    [Header("Tree Prefabs")]
    [Tooltip("Drag tree prefabs here. Used for forest areas.")]
    public GameObject[] treePrefabs;

    [Header("Rock Prefabs")]
    [Tooltip("Drag rock prefabs here. Scattered along the coastline.")]
    public GameObject[] rockPrefabs;

    [Header("Water Settings")]
    [Tooltip("Color of the ocean water plane.")]
    public Color waterColor = new Color(0.15f, 0.45f, 0.75f, 1f);
    [Tooltip("Y offset for water surface relative to terrain base.")]
    public float waterYOffset = -1.5f;

    [Header("Sand Settings")]
    [Tooltip("Color of the sandy beach cubes.")]
    public Color sandColor = new Color(0.86f, 0.78f, 0.58f, 1f);
    [Tooltip("Height of sand cubes.")]
    public float sandHeight = 0.15f;

    [Header("Transition")]
    [Tooltip("How far up the side borders the beach extends before fully becoming forest (0-1). 0.5 = halfway up.")]
    [Range(0.1f, 0.8f)]
    public float beachToForestBlend = 0.45f;

    [Header("Density")]
    [Tooltip("Trees per cell in forest areas.")]
    [Range(0.5f, 4f)]
    public float treesPerCell = 1.5f;
    [Tooltip("Rocks per cell along the coastline.")]
    [Range(0.1f, 2f)]
    public float rocksPerCell = 0.5f;

    [Header("Variation")]
    public Vector2 treeScaleRange = new Vector2(0.8f, 1.4f);
    public Vector2 rockScaleRange = new Vector2(0.8f, 1.8f);
    [Range(0f, 1f)]
    public float positionJitter = 0.9f;

    [Header("General")]
    public bool spawnOnStart = true;
    public int randomSeed = 0;

    void Start()
    {
        if (spawnOnStart)
            SpawnCoast();
    }

    public void SpawnCoast()
    {
        if (gridManager == null)
        {
            Debug.LogError("[CoastalBorderSpawner] No GridManager assigned!");
            return;
        }

        Terrain terrain = gridManager.terrain;
        if (terrain == null || terrain.terrainData == null)
        {
            Debug.LogError("[CoastalBorderSpawner] No terrain found on GridManager!");
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
            Debug.LogWarning("[CoastalBorderSpawner] No border area. Terrain must be larger than the grid.");
            return;
        }

        ClearCoast();

        int seed = (randomSeed != 0) ? randomSeed : System.Environment.TickCount;
        Random.InitState(seed);

        Vector3 terrainOrigin = terrain.transform.position;

        int totalW = gridW + borderX * 2;
        int totalH = gridH + borderZ * 2;

        GameObject parent = new GameObject("CoastalBorder");
        parent.transform.SetParent(transform);

        // ── 1. Spawn water plane on the TOP (high Z) side ──
        SpawnWaterPlane(terrain, terrainOrigin, cellSize, totalW, totalH, borderZ, gridH, parent.transform);

        // ── 2. Iterate border cells ──
        int treeCount = 0, sandCount = 0, rockCount = 0;

        for (int tx = 0; tx < totalW; tx++)
        {
            for (int tz = 0; tz < totalH; tz++)
            {
                // Skip cells inside the playable grid
                bool insideGrid = tx >= borderX && tx < borderX + gridW &&
                                  tz >= borderZ && tz < borderZ + gridH;
                if (insideGrid) continue;

                // Determine which zone this cell is in
                // Beach side = TOP (high Z, near camera start)
                // Forest side = BOTTOM (low Z, far from camera)
                bool isTop = (tz >= borderZ + gridH);    // Beach / coast side
                bool isBottom = (tz < borderZ);            // Forest side
                bool isLeftRight = !isBottom && !isTop && (tx < borderX || tx >= borderX + gridW);

                // Calculate blend factor: 0 = full beach, 1 = full forest
                float blendToForest = 1f;

                if (isTop)
                {
                    blendToForest = 0f; // Full beach (near camera)
                }
                else if (isBottom)
                {
                    blendToForest = 1f; // Full forest (far from camera)
                }
                else if (isLeftRight)
                {
                    // Gradual transition along the sides
                    // progressZ: 0 at top of grid (beach end) → 1 at bottom of grid (forest end)
                    float progressFromBeach = (float)(borderZ + gridH - 1 - tz) / gridH;
                    blendToForest = Mathf.Clamp01(progressFromBeach / beachToForestBlend);
                }

                // Decide what to spawn based on blend
                float roll = Random.value;

                if (roll < blendToForest)
                {
                    // Spawn tree
                    if (treePrefabs != null && treePrefabs.Length > 0)
                    {
                        int count = Mathf.Max(1, Mathf.RoundToInt(treesPerCell));
                        for (int t = 0; t < count; t++)
                        {
                            if (t == count - 1 && treesPerCell % 1f > 0f)
                            {
                                if (Random.value > (treesPerCell % 1f))
                                    continue;
                            }

                            Vector3 pos = GetCellWorldPos(terrain, terrainOrigin, cellSize, tx, tz);
                            pos.x += Random.Range(-0.5f, 0.5f) * positionJitter * cellSize;
                            pos.z += Random.Range(-0.5f, 0.5f) * positionJitter * cellSize;
                            pos.y = terrain.SampleHeight(pos) + terrainOrigin.y;

                            GameObject prefab = treePrefabs[Random.Range(0, treePrefabs.Length)];
                            if (prefab == null) continue;

                            SpawnDecor(prefab, pos, parent.transform,
                                Random.Range(treeScaleRange.x, treeScaleRange.y));
                            treeCount++;
                        }
                    }
                }
                else
                {
                    // Spawn sand
                    Vector3 sandPos = GetCellWorldPos(terrain, terrainOrigin, cellSize, tx, tz);
                    sandPos.y = terrain.SampleHeight(sandPos) + terrainOrigin.y;
                    SpawnSandCube(sandPos, cellSize, parent.transform);
                    sandCount++;

                    // Scatter rocks only on inner beach cells (not outermost rows near water)
                    bool innerBeach = true;
                    if (isTop) innerBeach = (tz < totalH - 2); // skip last 2 rows

                    if (innerBeach && rockPrefabs != null && rockPrefabs.Length > 0)
                    {
                        if (Random.value < rocksPerCell)
                        {
                            Vector3 rockPos = sandPos;
                            rockPos.x += Random.Range(-0.3f, 0.3f) * cellSize;
                            rockPos.z += Random.Range(-0.3f, 0.3f) * cellSize;
                            rockPos.y = terrain.SampleHeight(rockPos) + terrainOrigin.y + 0.05f;

                            GameObject rockPrefab = rockPrefabs[Random.Range(0, rockPrefabs.Length)];
                            if (rockPrefab != null)
                            {
                                SpawnDecor(rockPrefab, rockPos, parent.transform,
                                    Random.Range(rockScaleRange.x, rockScaleRange.y));
                                rockCount++;
                            }
                        }
                    }
                }
            }
        }

        Debug.Log($"[CoastalBorderSpawner] Spawned coast: {sandCount} sand, {treeCount} trees, {rockCount} rocks (seed={seed}).");
    }

    private void SpawnWaterPlane(Terrain terrain, Vector3 terrainOrigin, float cellSize,
        int totalW, int totalH, int borderZ, int gridH, Transform parent)
    {
        // Water extends from the middle of the beach outward as ocean
        float beachDepth = borderZ * cellSize;
        float terrainEndZ = terrainOrigin.z + totalH * cellSize;
        float waterStartZ = terrainEndZ - beachDepth * 0.5f; // starts halfway through beach
        float waterWidth = totalW * cellSize; // match terrain width exactly
        float oceanExtent = beachDepth * 0.5f + beachDepth * 2f; // half beach + ocean beyond

        if (oceanExtent <= 0) return;

        // Use a Plane primitive (faces up by default, 10x10 units at scale 1)
        GameObject waterObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
        waterObj.name = "OceanWater";
        waterObj.transform.SetParent(parent);

        // Position: starts at terrain edge, extends outward
        float centerX = terrainOrigin.x + totalW * cellSize * 0.5f;
        float centerZ = waterStartZ + oceanExtent * 0.5f;
        float waterY = terrainOrigin.y + waterYOffset;

        waterObj.transform.position = new Vector3(centerX, waterY, centerZ);

        // Plane is 10x10 at scale 1, so scale = desiredSize / 10
        waterObj.transform.localScale = new Vector3(
            waterWidth / 10f,
            1f,
            oceanExtent / 10f
        );

        // Set water material — opaque blue
        Renderer renderer = waterObj.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material waterMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            waterMat.color = waterColor;
            waterMat.SetFloat("_Smoothness", 0.85f);
            waterMat.SetFloat("_Metallic", 0.05f);
            renderer.material = waterMat;
        }

        // Remove collider, set non-interactable
        Collider col = waterObj.GetComponent<Collider>();
        if (col != null)
        {
            if (Application.isPlaying) Destroy(col);
            else DestroyImmediate(col);
        }
        waterObj.layer = LayerMask.NameToLayer("Ignore Raycast");
    }

    private void SpawnSandCube(Vector3 position, float cellSize, Transform parent)
    {
        GameObject sandObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sandObj.name = "Sand";
        sandObj.transform.SetParent(parent);
        sandObj.transform.position = position + new Vector3(0, sandHeight * 0.5f, 0);
        sandObj.transform.localScale = new Vector3(cellSize, sandHeight, cellSize);

        Renderer renderer = sandObj.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material sandMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            sandMat.color = sandColor;
            sandMat.SetFloat("_Smoothness", 0.05f);
            renderer.material = sandMat;
        }

        Collider col = sandObj.GetComponent<Collider>();
        if (col != null)
        {
            if (Application.isPlaying) Destroy(col);
            else DestroyImmediate(col);
        }
        sandObj.layer = LayerMask.NameToLayer("Ignore Raycast");
    }

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

        SetLayerRecursive(obj, LayerMask.NameToLayer("Ignore Raycast"));
        foreach (Collider c in obj.GetComponentsInChildren<Collider>())
            c.enabled = false;
    }

    private Vector3 GetCellWorldPos(Terrain terrain, Vector3 terrainOrigin, float cellSize, int tx, int tz)
    {
        float worldX = terrainOrigin.x + (tx + 0.5f) * cellSize;
        float worldZ = terrainOrigin.z + (tz + 0.5f) * cellSize;
        float worldY = terrain.SampleHeight(new Vector3(worldX, 0, worldZ)) + terrainOrigin.y;
        return new Vector3(worldX, worldY, worldZ);
    }

    public void ClearCoast()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            if (child.name == "CoastalBorder")
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
