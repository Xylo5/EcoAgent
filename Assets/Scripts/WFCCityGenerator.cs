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

    // Each cell stores a list of still-possible tile indices (into adjacencyRuleSet.availableTiles).
    private List<int>[,] possibilityGrid;

    // The collapsed result: tileIndex per cell (-1 = not yet collapsed).
    private int[,] collapsedGrid;

    // Cardinal neighbour offsets (N, S, E, W).
    private static readonly Vector2Int[] Directions =
    {
        new Vector2Int( 0,  1),
        new Vector2Int( 0, -1),
        new Vector2Int( 1,  0),
        new Vector2Int(-1,  0)
    };

    // Pre-computed allowed-neighbour sets per tile index for fast lookup.
    private HashSet<int>[] allowedNeighbourSets;

    // Cached grid dimensions (read from GridManager at generation time).
    private int gridWidth;
    private int gridHeight;
    private float cellSize;

    // ═══════════════════════════════════════════
    //  PUBLIC API
    // ═══════════════════════════════════════════

    /// <summary>
    /// Runs the WFC algorithm and spawns prefabs in the scene.
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
        cellSize   = gridManager.cellSize > 0f ? gridManager.cellSize : 2.5f;

        ClearCity();
        BuildAllowedNeighbourLookup();

        int seed = (randomSeed != 0) ? randomSeed : System.Environment.TickCount;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            Random.InitState(seed + attempt);

            if (RunWFC())
            {
                SpawnPrefabs();
                Debug.Log($"[WFCCityGenerator] City generated successfully (attempt {attempt + 1}, seed {seed + attempt}).");
                return;
            }

            Debug.LogWarning($"[WFCCityGenerator] Contradiction on attempt {attempt + 1}, retrying...");
        }

        Debug.LogError($"[WFCCityGenerator] Failed to generate after {maxRetries} attempts.");
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
    //  WFC CORE
    // ═══════════════════════════════════════════

    /// <summary>
    /// Runs the WFC loop. Returns true on success, false on contradiction.
    /// </summary>
    private bool RunWFC()
    {
        int tileCount = adjacencyRuleSet.availableTiles.Length;

        // Initialise: every cell can be any tile.
        possibilityGrid = new List<int>[gridWidth, gridHeight];
        collapsedGrid = new int[gridWidth, gridHeight];

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                possibilityGrid[x, z] = new List<int>(tileCount);
                for (int t = 0; t < tileCount; t++)
                    possibilityGrid[x, z].Add(t);

                collapsedGrid[x, z] = -1;
            }
        }

        int totalCells = gridWidth * gridHeight;
        int collapsedCount = 0;

        while (collapsedCount < totalCells)
        {
            // 1. OBSERVE — find the uncollapsed cell with lowest entropy.
            Vector2Int cell = FindLowestEntropyCell();
            if (cell.x == -1)
                return false; // No valid cell found (shouldn't happen if collapsedCount < total).

            List<int> options = possibilityGrid[cell.x, cell.y];
            if (options.Count == 0)
                return false; // Contradiction.

            // 2. COLLAPSE — pick a random tile from the remaining options.
            int chosenIndex = options[Random.Range(0, options.Count)];
            collapsedGrid[cell.x, cell.y] = chosenIndex;
            options.Clear();
            options.Add(chosenIndex);
            collapsedCount++;

            // 3. PROPAGATE — constrain neighbours.
            if (!Propagate(cell))
                return false; // Contradiction during propagation.
        }

        return true;
    }

    /// <summary>
    /// Finds the uncollapsed cell with the fewest remaining possibilities.
    /// Ties are broken randomly for variety.
    /// </summary>
    private Vector2Int FindLowestEntropyCell()
    {
        int minEntropy = int.MaxValue;
        List<Vector2Int> candidates = new List<Vector2Int>();

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                if (collapsedGrid[x, z] != -1) continue; // Already collapsed.

                int entropy = possibilityGrid[x, z].Count;
                if (entropy == 0) return new Vector2Int(-1, -1); // Contradiction.

                if (entropy < minEntropy)
                {
                    minEntropy = entropy;
                    candidates.Clear();
                    candidates.Add(new Vector2Int(x, z));
                }
                else if (entropy == minEntropy)
                {
                    candidates.Add(new Vector2Int(x, z));
                }
            }
        }

        if (candidates.Count == 0) return new Vector2Int(-1, -1);
        return candidates[Random.Range(0, candidates.Count)];
    }

    /// <summary>
    /// Propagates constraints outward from the given cell using a BFS queue.
    /// Returns false if a contradiction is found.
    /// </summary>
    private bool Propagate(Vector2Int startCell)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(startCell);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            List<int> currentOptions = possibilityGrid[current.x, current.y];

            // Compute the union of allowed neighbours for all remaining options in this cell.
            HashSet<int> allowedForNeighbours = new HashSet<int>();
            for (int i = 0; i < currentOptions.Count; i++)
            {
                int tileIdx = currentOptions[i];
                if (allowedNeighbourSets[tileIdx] != null)
                    allowedForNeighbours.UnionWith(allowedNeighbourSets[tileIdx]);
            }

            for (int d = 0; d < Directions.Length; d++)
            {
                int nx = current.x + Directions[d].x;
                int nz = current.y + Directions[d].y;

                if (nx < 0 || nx >= gridWidth || nz < 0 || nz >= gridHeight) continue;
                if (collapsedGrid[nx, nz] != -1) continue; // Already collapsed, skip.

                List<int> neighbourOptions = possibilityGrid[nx, nz];
                bool changed = false;

                for (int i = neighbourOptions.Count - 1; i >= 0; i--)
                {
                    if (!allowedForNeighbours.Contains(neighbourOptions[i]))
                    {
                        neighbourOptions.RemoveAt(i);
                        changed = true;
                    }
                }

                if (neighbourOptions.Count == 0)
                    return false; // Contradiction.

                if (changed)
                    queue.Enqueue(new Vector2Int(nx, nz));
            }
        }

        return true;
    }

    // ═══════════════════════════════════════════
    //  PREFAB SPAWNING
    // ═══════════════════════════════════════════

    /// <summary>
    /// Instantiates prefabs for every collapsed cell.
    /// </summary>
    private void SpawnPrefabs()
    {
        CityTileData[] tiles = adjacencyRuleSet.availableTiles;
        Vector3 origin = transform.position;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                int tileIdx = collapsedGrid[x, z];
                if (tileIdx < 0 || tileIdx >= tiles.Length) continue;

                CityTileData tileData = tiles[tileIdx];
                GameObject prefab = tileData.GetRandomPrefab();
                if (prefab == null) continue;

                Vector3 position = origin + new Vector3(
                    x * cellSize + cellSize * 0.5f,
                    0f,
                    z * cellSize + cellSize * 0.5f
                );

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

                instance.name = $"{tileData.tileName}_{x}_{z}";
            }
        }
    }

    // ═══════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════

    /// <summary>
    /// Pre-computes a HashSet of allowed neighbour indices for each tile
    /// so propagation is fast O(1) lookups instead of linear scans.
    /// </summary>
    private void BuildAllowedNeighbourLookup()
    {
        CityTileData[] tiles = adjacencyRuleSet.availableTiles;
        int count = tiles.Length;
        allowedNeighbourSets = new HashSet<int>[count];

        for (int i = 0; i < count; i++)
        {
            allowedNeighbourSets[i] = new HashSet<int>();
            CityTileData[] allowed = adjacencyRuleSet.GetAllowedNeighbours(tiles[i]);

            if (allowed == null) continue;

            for (int a = 0; a < allowed.Length; a++)
            {
                int idx = System.Array.IndexOf(tiles, allowed[a]);
                if (idx >= 0)
                    allowedNeighbourSets[i].Add(idx);
            }
        }
    }
}
