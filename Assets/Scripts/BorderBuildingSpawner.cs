using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns buildings in the terrain border area around the playable grid.
/// Auto-detects the border by comparing terrain size to grid size.
/// Buildings face only cardinal directions (N/S/E/W) and are spaced to avoid overlap.
/// Buildings are non-interactable (decorative only).
/// </summary>
public class BorderBuildingSpawner : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;

    [Header("Building Prefabs")]
    [Tooltip("Drag building prefabs here. A random one is picked per placement.")]
    public GameObject[] buildingPrefabs;

    [Header("Density")]
    [Tooltip("Spacing multiplier between buildings. Higher = more spread out. " +
             "1 = one building every cell, 2 = one building every 2 cells, etc.")]
    [Range(1f, 6f)]
    public float spacingMultiplier = 2f;

    [Header("Variation")]
    [Tooltip("Random scale range for buildings (min, max).")]
    public Vector2 scaleRange = new Vector2(0.8f, 1.2f);
    [Tooltip("Random position jitter within each placement slot (0 = centered, 1 = full cell). " +
             "Keep low to prevent overlap.")]
    [Range(0f, 0.4f)]
    public float positionJitter = 0.15f;

    [Header("Spacing Guard")]
    [Tooltip("Minimum distance between any two buildings in world units. " +
             "Buildings that would spawn too close to an existing one are skipped.")]
    public float minimumBuildingDistance = 3f;

    [Header("Placement Chance")]
    [Tooltip("Probability (0-1) that a valid slot actually gets a building. " +
             "Lower values produce a sparser skyline.")]
    [Range(0f, 1f)]
    public float placementChance = 0.7f;

    [Header("Size Normalization")]
    [Tooltip("Target footprint size in world units that all prefabs will be normalized to. " +
             "Set to 0 to auto-calculate from the median prefab size.")]
    public float targetBuildingSize = 0f;
    [Tooltip("If true, also normalizes building height to keep proportions uniform. " +
             "If false, only the XZ footprint is normalized (height varies naturally).")]
    public bool normalizeHeight = false;

    [Header("General")]
    public bool spawnOnStart = true;
    public int randomSeed = 0;

    // Cached normalization data
    private float[] prefabNormScales;
    private float[] prefabFootprints;

    // Cardinal rotations: North (0°), East (90°), South (180°), West (270°)
    private static readonly float[] CardinalAngles = { 0f, 90f, 180f, 270f };

    void Start()
    {
        if (spawnOnStart)
            SpawnBuildings();
    }

    public void SpawnBuildings()
    {
        if (gridManager == null)
        {
            Debug.LogError("[BorderBuildingSpawner] No GridManager assigned!");
            return;
        }

        if (buildingPrefabs == null || buildingPrefabs.Length == 0)
        {
            Debug.LogError("[BorderBuildingSpawner] No building prefabs assigned!");
            return;
        }

        // Pre-compute normalization scales for all prefabs
        ComputeNormalizationFactors();

        Terrain terrain = gridManager.terrain;
        if (terrain == null || terrain.terrainData == null)
        {
            Debug.LogError("[BorderBuildingSpawner] No terrain found on GridManager!");
            return;
        }

        float cellSize = gridManager.cellSize;
        int gridW = gridManager.gridWidth;
        int gridH = gridManager.gridHeight;

        // Auto-detect border from terrain vs grid size
        float terrainSizeX = terrain.terrainData.size.x;
        float terrainSizeZ = terrain.terrainData.size.z;
        float gridSizeX = gridW * cellSize;
        float gridSizeZ = gridH * cellSize;

        int borderX = Mathf.Max(0, Mathf.FloorToInt((terrainSizeX - gridSizeX) / (2f * cellSize)));
        int borderZ = Mathf.Max(0, Mathf.FloorToInt((terrainSizeZ - gridSizeZ) / (2f * cellSize)));

        if (borderX <= 0 && borderZ <= 0)
        {
            Debug.LogWarning("[BorderBuildingSpawner] Terrain is same size as grid — no border to fill. Resize terrain to be larger than the grid.");
            return;
        }

        ClearBuildings();

        int seed = (randomSeed != 0) ? randomSeed : System.Environment.TickCount;
        Random.InitState(seed);

        Vector3 terrainOrigin = terrain.transform.position;
        Vector3 gridOrigin = gridManager.GridOrigin;

        int totalW = gridW + borderX * 2;
        int totalH = gridH + borderZ * 2;

        // Step size in cells — controls spacing between placement slots
        int step = Mathf.Max(1, Mathf.RoundToInt(spacingMultiplier));

        int buildingCount = 0;
        GameObject parent = new GameObject("BorderBuildings");
        parent.transform.SetParent(transform);

        // Track placed positions and their effective radii to enforce minimum distance
        var placedPositions = new List<Vector3>();
        var placedRadii = new List<float>();

        // Compute the playable grid bounds in world space (with a 1-cell safety margin)
        float gridMargin = cellSize * 1.5f; // extra buffer so building footprints don't bleed in
        float gridMinX = gridOrigin.x - gridMargin;
        float gridMaxX = gridOrigin.x + gridSizeX + gridMargin;
        float gridMinZ = gridOrigin.z - gridMargin;
        float gridMaxZ = gridOrigin.z + gridSizeZ + gridMargin;

        for (int tx = 0; tx < totalW; tx += step)
        {
            for (int tz = 0; tz < totalH; tz += step)
            {
                // Skip cells inside the playable grid (with 1-cell margin to prevent edge encroachment)
                bool insideGrid = tx >= (borderX - 1) && tx < (borderX + gridW + 1) &&
                                  tz >= (borderZ - 1) && tz < (borderZ + gridH + 1);
                if (insideGrid) continue;

                // Random chance to skip this slot for variety
                if (Random.value > placementChance)
                    continue;

                float jitterX = Random.Range(-0.5f, 0.5f) * positionJitter * cellSize;
                float jitterZ = Random.Range(-0.5f, 0.5f) * positionJitter * cellSize;

                // Position relative to terrain origin (not grid origin) since border covers the whole terrain
                float worldX = terrainOrigin.x + (tx + 0.5f) * cellSize + jitterX;
                float worldZ = terrainOrigin.z + (tz + 0.5f) * cellSize + jitterZ;
                float worldY = terrain.SampleHeight(new Vector3(worldX, 0, worldZ)) + terrainOrigin.y;

                Vector3 pos = new Vector3(worldX, worldY, worldZ);

                // Pick prefab and compute its normalized scale
                int prefabIndex = Random.Range(0, buildingPrefabs.Length);
                GameObject prefab = buildingPrefabs[prefabIndex];
                if (prefab == null) continue;

                float normScale = (prefabNormScales != null && prefabIndex < prefabNormScales.Length)
                    ? prefabNormScales[prefabIndex] : 1f;
                float randomVariation = Random.Range(scaleRange.x, scaleRange.y);
                float finalScale = normScale * randomVariation;

                // Effective radius of this building after scaling
                float effectiveFootprint = (prefabFootprints != null && prefabIndex < prefabFootprints.Length)
                    ? prefabFootprints[prefabIndex] * finalScale : 0f;
                float effectiveRadius = effectiveFootprint * 0.5f;

                // Final world-space check: skip if the building's footprint would encroach on the grid
                bool encroachesGrid = (pos.x + effectiveRadius > gridMinX && pos.x - effectiveRadius < gridMaxX &&
                                       pos.z + effectiveRadius > gridMinZ && pos.z - effectiveRadius < gridMaxZ);
                if (encroachesGrid) continue;

                // Enforce minimum distance between buildings to avoid overlap
                bool tooClose = false;
                for (int i = 0; i < placedPositions.Count; i++)
                {
                    float requiredDist = Mathf.Max(minimumBuildingDistance, effectiveRadius + placedRadii[i]);
                    if (Vector3.Distance(pos, placedPositions[i]) < requiredDist)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;

                // Only use cardinal directions (N/S/E/W)
                float cardinalYRotation = CardinalAngles[Random.Range(0, CardinalAngles.Length)];
                Quaternion rot = Quaternion.Euler(0f, cardinalYRotation, 0f);

                GameObject building;
                #if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    building = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent.transform);
                    building.transform.position = pos;
                    building.transform.rotation = rot;
                }
                else
                #endif
                {
                    building = Instantiate(prefab, pos, rot, parent.transform);
                }

                // Apply normalized + randomized scale
                building.transform.localScale = Vector3.one * finalScale;

                // Make non-interactable
                SetLayerRecursive(building, LayerMask.NameToLayer("Ignore Raycast"));
                foreach (Collider col in building.GetComponentsInChildren<Collider>())
                    col.enabled = false;

                placedPositions.Add(pos);
                placedRadii.Add(effectiveRadius);
                buildingCount++;
            }
        }

        Debug.Log($"[BorderBuildingSpawner] Spawned {buildingCount} buildings in border (borderX={borderX}, borderZ={borderZ}, seed={seed}).");
    }

    public void ClearBuildings()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            if (child.name == "BorderBuildings")
            {
                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }
        }
    }

    /// <summary>
    /// Measures each prefab's renderer bounds and computes a per-prefab normalization
    /// scale factor so all buildings end up with a similar footprint size.
    /// </summary>
    private void ComputeNormalizationFactors()
    {
        int count = buildingPrefabs.Length;
        prefabNormScales = new float[count];
        prefabFootprints = new float[count];
        float[] rawFootprints = new float[count];

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = buildingPrefabs[i];
            if (prefab == null)
            {
                rawFootprints[i] = 1f;
                continue;
            }

            Bounds bounds = GetPrefabBounds(prefab);
            // Footprint = max of X and Z extents (the dominant ground-plane dimension)
            float footprint = Mathf.Max(bounds.size.x, bounds.size.z);
            rawFootprints[i] = Mathf.Max(footprint, 0.01f); // avoid zero
        }

        // Determine the target size: user-specified or auto (median of all prefab footprints)
        float target = targetBuildingSize;
        if (target <= 0f)
        {
            target = ComputeMedian(rawFootprints);
        }

        for (int i = 0; i < count; i++)
        {
            prefabNormScales[i] = target / rawFootprints[i];
            // Store the original footprint so we can compute effective radius after scaling
            prefabFootprints[i] = rawFootprints[i];
        }

        Debug.Log($"[BorderBuildingSpawner] Normalization — target footprint: {target:F2}. " +
                  $"Scale factors: [{string.Join(", ", System.Array.ConvertAll(prefabNormScales, s => s.ToString("F2")))}]");
    }

    /// <summary>
    /// Calculates the combined renderer bounds of a prefab (without instantiating it at runtime).
    /// </summary>
    private Bounds GetPrefabBounds(GameObject prefab)
    {
        Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            // Fallback: try colliders
            Collider[] colliders = prefab.GetComponentsInChildren<Collider>(true);
            if (colliders.Length > 0)
            {
                Bounds b = colliders[0].bounds;
                for (int i = 1; i < colliders.Length; i++)
                    b.Encapsulate(colliders[i].bounds);
                return b;
            }
            return new Bounds(Vector3.zero, Vector3.one);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds;
    }

    /// <summary>
    /// Returns the median value of an array of floats.
    /// </summary>
    private float ComputeMedian(float[] values)
    {
        float[] sorted = (float[])values.Clone();
        System.Array.Sort(sorted);
        int mid = sorted.Length / 2;
        if (sorted.Length % 2 == 0)
            return (sorted[mid - 1] + sorted[mid]) / 2f;
        return sorted[mid];
    }

    private void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, layer);
    }
}
