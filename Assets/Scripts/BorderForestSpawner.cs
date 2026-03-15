using UnityEngine;

/// <summary>
/// Spawns a dense forest of trees in the terrain border area around the playable grid.
/// The border is defined by GridManager.borderCells — the ring of cells between
/// the terrain edge and the playable grid. Trees are non-interactable (decorative only).
/// </summary>
public class BorderForestSpawner : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;

    [Header("Tree Prefabs")]
    [Tooltip("Drag tree prefabs here. A random one is picked per placement.")]
    public GameObject[] treePrefabs;

    [Header("Density")]
    [Tooltip("Trees per cell in the border area. 1 = one tree per cell, 2 = denser.")]
    [Range(0.5f, 4f)]
    public float treesPerCell = 1.5f;

    [Header("Variation")]
    [Tooltip("Random scale range for trees (min, max).")]
    public Vector2 scaleRange = new Vector2(0.8f, 1.4f);
    [Tooltip("Random position jitter within each cell (0 = centered, 1 = full cell).")]
    [Range(0f, 1f)]
    public float positionJitter = 0.9f;

    [Header("General")]
    public bool spawnOnStart = true;
    public int randomSeed = 0;

    void Start()
    {
        if (spawnOnStart)
            SpawnForest();
    }

    public void SpawnForest()
    {
        if (gridManager == null)
        {
            Debug.LogError("[BorderForestSpawner] No GridManager assigned!");
            return;
        }

        if (treePrefabs == null || treePrefabs.Length == 0)
        {
            Debug.LogError("[BorderForestSpawner] No tree prefabs assigned!");
            return;
        }

        int border = gridManager.borderCells;
        if (border <= 0)
        {
            Debug.LogWarning("[BorderForestSpawner] borderCells is 0 — nothing to fill.");
            return;
        }

        ClearForest();

        int seed = (randomSeed != 0) ? randomSeed : System.Environment.TickCount;
        Random.InitState(seed);

        float cellSize = gridManager.cellSize;
        int gridW = gridManager.gridWidth;
        int gridH = gridManager.gridHeight;

        // Total terrain size in cells (grid + border on each side)
        int totalW = gridW + border * 2;
        int totalH = gridH + border * 2;

        Vector3 terrainOrigin = gridManager.terrain != null
            ? gridManager.terrain.transform.position
            : Vector3.zero;

        int treeCount = 0;
        GameObject parent = new GameObject("BorderForest");
        parent.transform.SetParent(transform);

        for (int tx = 0; tx < totalW; tx++)
        {
            for (int tz = 0; tz < totalH; tz++)
            {
                // Skip cells inside the playable grid
                bool insideGrid = tx >= border && tx < border + gridW &&
                                  tz >= border && tz < border + gridH;
                if (insideGrid) continue;

                // Spawn multiple trees per cell for density
                int count = Mathf.Max(1, Mathf.RoundToInt(treesPerCell));
                for (int t = 0; t < count; t++)
                {
                    float jitterX = Random.Range(-0.5f, 0.5f) * positionJitter * cellSize;
                    float jitterZ = Random.Range(-0.5f, 0.5f) * positionJitter * cellSize;

                    // Extra fractional chance for non-integer treesPerCell
                    if (t == count - 1 && treesPerCell % 1f > 0f)
                    {
                        if (Random.value > (treesPerCell % 1f))
                            continue;
                    }

                    float worldX = terrainOrigin.x + (tx + 0.5f) * cellSize + jitterX;
                    float worldZ = terrainOrigin.z + (tz + 0.5f) * cellSize + jitterZ;
                    float worldY = terrainOrigin.y;
                    if (gridManager.terrain != null)
                        worldY = gridManager.terrain.SampleHeight(new Vector3(worldX, 0, worldZ)) + terrainOrigin.y;

                    Vector3 pos = new Vector3(worldX, worldY, worldZ);

                    GameObject prefab = treePrefabs[Random.Range(0, treePrefabs.Length)];
                    if (prefab == null) continue;

                    float randomYRotation = Random.Range(0f, 360f);
                    Quaternion rot = Quaternion.Euler(0f, randomYRotation, 0f);

                    GameObject tree;
                    #if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        tree = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent.transform);
                        tree.transform.position = pos;
                        tree.transform.rotation = rot;
                    }
                    else
                    #endif
                    {
                        tree = Instantiate(prefab, pos, rot, parent.transform);
                    }

                    float scale = Random.Range(scaleRange.x, scaleRange.y);
                    tree.transform.localScale = Vector3.one * scale;

                    // Make non-interactable
                    SetLayerRecursive(tree, LayerMask.NameToLayer("Ignore Raycast"));
                    foreach (Collider col in tree.GetComponentsInChildren<Collider>())
                        col.enabled = false;

                    treeCount++;
                }
            }
        }

        Debug.Log($"[BorderForestSpawner] Spawned {treeCount} trees in border area (seed {seed}).");
    }

    public void ClearForest()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            if (child.name == "BorderForest")
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
