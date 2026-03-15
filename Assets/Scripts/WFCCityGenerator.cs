using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Procedural city generator using the Wave Function Collapse algorithm.
/// Attach to an empty GameObject, assign a GridManager and CityTileAdjacencyRule,
/// and click "Generate City" in the Inspector.
/// </summary>
public class WFCCityGenerator : MonoBehaviour
{
    [Header("Grid Reference")]
    [Tooltip("Reference to the GridManager that defines grid dimensions and cell size.")]
    public GridManager gridManager;

    [Header("WFC Settings")]
    [Tooltip("Adjacency rule set that defines tile types and neighbour constraints.")]
    public CityTileAdjacencyRule adjacencyRuleSet;

    [Tooltip("Random seed. Set to 0 for a random seed each run.")]
    public int randomSeed = 0;

    [Tooltip("Maximum retries when the algorithm hits a contradiction.")]
    public int maxRetries = 10;

    // ═══════════════════════════════════════════
    //  INTERNAL STATE
    // ═══════════════════════════════════════════

    // Tracks which cells are filled during generation
    private bool[,] placedGrid;

    // Cached grid dimensions (read from GridManager at generation time).
    private int gridWidth;
    private int gridHeight;
    private float cellSize;
    private Vector3 cachedTerrainOrigin;

    // ═══════════════════════════════════════════
    //  PUBLIC API
    // ═══════════════════════════════════════════

    /// <summary>
    /// Runs the generation algorithm and spawns prefabs in the scene.
    /// Safe to call from edit mode via custom editor.
    /// </summary>
    public void GenerateCity()
    {
        if (gridManager == null)
        {
            Debug.LogError("[WFCCityGenerator] No GridManager assigned!");
            return;
        }

        if (adjacencyRuleSet == null || adjacencyRuleSet.availableTiles == null ||
            adjacencyRuleSet.availableTiles.Length == 0)
        {
            Debug.LogError("[WFCCityGenerator] No adjacency rule set assigned or it has no tiles!");
            return;
        }

        // Cache grid dimensions from GridManager.
        gridWidth  = gridManager.gridWidth;
        gridHeight = gridManager.gridHeight;
        // cellSize may be 0 in edit mode (Awake hasn't run), fall back to 2.5.
        cellSize   = gridManager.cellSize > 0f ? gridManager.cellSize : 2.5f;

        // Cache terrain origin so tiles align to the terrain, not this transform.
        cachedTerrainOrigin = (gridManager.terrain != null)
            ? gridManager.terrain.transform.position
            : Vector3.zero;

        ClearCity();

        int seed = (randomSeed != 0) ? randomSeed : System.Environment.TickCount;
        Random.InitState(seed);
        
        placedGrid = new bool[gridWidth, gridHeight];

        // Randomly place tiles from the available rule set, respecting their sizeInCells
        CityTileData[] tiles = adjacencyRuleSet.availableTiles;
        int maxAttempts = gridWidth * gridHeight * maxRetries;
        int placedCount = 0;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            int x = Random.Range(0, gridWidth);
            int z = Random.Range(0, gridHeight);
            CityTileData tile = tiles[Random.Range(0, tiles.Length)];
            int size = tile.sizeInCells;

            // Check boundaries
            if (x + size > gridWidth || z + size > gridHeight)
                continue;

            // Check availability
            if (IsAreaAvailable(x, z, size))
            {
                // Mark occupied
                OccupyArea(x, z, size);

                // Instantiate Prefab
                GameObject prefab = tile.GetRandomPrefab();
                if (prefab != null)
                {
                    Vector3 position = GetWorldPosition(new Vector2Int(x, z), size);
                    GameObject instance;

                    #if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        instance = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, transform);
                        instance.transform.position = position;
                    }
                    else
                    #endif
                    {
                        instance = Instantiate(prefab, position, Quaternion.identity, transform);
                    }

                    instance.name = $"{tile.tileName}_{x}_{z}";

                    // Put on Ignore Raycast so tiles don't block BuildingPlacer mouse input.
                    SetLayerRecursive(instance, LayerMask.NameToLayer("Ignore Raycast"));

                    placedCount++;
                }
            }
        }

        // Mark all placed cells as permanently occupied in GridManager
        // so BuildingPlacer won't allow buildings on top of generated tiles.
        if (Application.isPlaying)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int z = 0; z < gridHeight; z++)
                {
                    if (placedGrid[x, z])
                        gridManager.OccupyCellsPermanent(new Vector2Int(x, z), 1);
                }
            }
        }

        Debug.Log($"[WFCCityGenerator] City generated with {placedCount} variable-sized tiles (seed {seed}).");
    }

    /// <summary>
    /// Destroys all child objects (the generated city).
    /// </summary>
    public void ClearCity()
    {
        // Work in reverse to safely destroy children.
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
    //  HELPERS
    // ═══════════════════════════════════════════

    private bool IsAreaAvailable(int startX, int startZ, int size)
    {
        for (int x = startX; x < startX + size; x++)
        {
            for (int z = startZ; z < startZ + size; z++)
            {
                if (placedGrid[x, z]) return false;
            }
        }
        return true;
    }

    private void OccupyArea(int startX, int startZ, int size)
    {
        for (int x = startX; x < startX + size; x++)
        {
            for (int z = startZ; z < startZ + size; z++)
            {
                placedGrid[x, z] = true;
            }
        }
    }

    private Vector3 GetWorldPosition(Vector2Int cell, int size)
    {
        // Use terrain origin so tiles align to the GridManager's grid.
        return cachedTerrainOrigin + new Vector3(
            cell.x * cellSize + (size * cellSize) * 0.5f,
            0f,
            cell.y * cellSize + (size * cellSize) * 0.5f
        );
    }

    private void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, layer);
    }
}
