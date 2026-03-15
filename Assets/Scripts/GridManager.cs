using UnityEngine;

/// <summary>
/// Manages a 100x100 grid on the terrain (CoC-style).
/// Draws grid lines and provides snap-to-grid utilities.
/// Cell size is auto-computed from the terrain dimensions.
/// Attach this to an empty GameObject named "GridManager".
/// </summary>
public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridWidth = 100;
    public int gridHeight = 100;

    [Tooltip("Auto-computed from terrain size. Do not edit manually.")]
    public float cellSize; // Computed in Awake: terrainSize / gridCount

    [Header("Grid Visual")]
    public Color gridColor = new Color(0.2f, 1f, 0.3f, 0.5f);
    public float lineWidth = 0.06f;
    public bool showGrid = true;

    [Header("References")]
    public Terrain terrain;

    // 2D array tracking which cells are occupied (true = occupied)
    private bool[,] occupiedCells;

    // 2D array tracking permanently blocked cells (environment objects)
    private bool[,] permanentCells;

    // Grid line rendering (URP-compatible)
    private GameObject gridLinesParent;

    // Cached terrain origin — terrain is static, so safe to cache
    private Vector3 terrainOrigin;

    /// <summary>
    /// World-space origin of the playable grid (bottom-left corner).
    /// Centered on the terrain when terrain is larger than the grid.
    /// Computed on access so it works in both edit and play mode.
    /// </summary>
    public Vector3 GridOrigin
    {
        get
        {
            Terrain t = terrain;
            // In edit mode terrain may not be auto-found yet; try to locate it.
            if (t == null)
                t = FindAnyObjectByType<Terrain>();
            if (t == null)
                return Vector3.zero;

            Vector3 tPos = t.transform.position;
            if (t.terrainData == null)
                return tPos;

            float cs = cellSize > 0f ? cellSize : 2.5f;
            float offsetX = (t.terrainData.size.x - gridWidth * cs) * 0.5f;
            float offsetZ = (t.terrainData.size.z - gridHeight * cs) * 0.5f;
            return tPos + new Vector3(offsetX, 0f, offsetZ);
        }
    }

    void Awake()
    {
        // Auto-find terrain if not assigned in Inspector
        if (terrain == null)
        {
            terrain = FindAnyObjectByType<Terrain>();
            if (terrain == null)
                Debug.LogWarning("[GridManager] No Terrain found in scene!");
        }

        // Force cell size to 2.5 as requested
        cellSize = 2.5f;
        
        if (terrain != null && terrain.terrainData != null)
        {
            Vector3 terrainSize = terrain.terrainData.size;
            Debug.Log($"[GridManager] Terrain size: {terrainSize.x}x{terrainSize.z}, Cell size fixed to: {cellSize}");
        }
        else
        {
            Debug.LogWarning("[GridManager] No terrain assigned! Using fixed cellSize=2.5.");
        }

        occupiedCells = new bool[gridWidth, gridHeight];
        permanentCells = new bool[gridWidth, gridHeight];

        terrainOrigin = (terrain != null) ? terrain.transform.position : Vector3.zero;

        if (showGrid)
            CreateGridLines();
    }

    /// <summary>
    /// Creates grid lines using LineRenderers (URP-compatible).
    /// </summary>
    private void CreateGridLines()
    {
        // Clean up any previous grid
        if (gridLinesParent != null)
            Destroy(gridLinesParent);

        gridLinesParent = new GameObject("GridLines");
        gridLinesParent.transform.SetParent(transform);

        Material lineMat = new Material(Shader.Find("Sprites/Default"));
        lineMat.hideFlags = HideFlags.HideAndDontSave;

        float yOffset = 0.05f;

        // Vertical lines (along Z axis)
        for (int x = 0; x <= gridWidth; x++)
        {
            Vector3 start = GridOrigin + new Vector3(x * cellSize, yOffset, 0);
            Vector3 end = GridOrigin + new Vector3(x * cellSize, yOffset, gridHeight * cellSize);
            CreateLine($"GridLine_V_{x}", start, end, lineMat, lineWidth);
        }

        // Horizontal lines (along X axis)
        for (int z = 0; z <= gridHeight; z++)
        {
            Vector3 start = GridOrigin + new Vector3(0, yOffset, z * cellSize);
            Vector3 end = GridOrigin + new Vector3(gridWidth * cellSize, yOffset, z * cellSize);
            CreateLine($"GridLine_H_{z}", start, end, lineMat, lineWidth);
        }

        Debug.Log($"[GridManager] Created {gridWidth + gridHeight + 2} grid lines.");
    }

    private void CreateLine(string name, Vector3 start, Vector3 end, Material mat, float width)
    {
        GameObject lineObj = new GameObject(name);
        lineObj.transform.SetParent(gridLinesParent.transform);

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.material = mat;
        lr.startColor = gridColor;
        lr.endColor = gridColor;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.useWorldSpace = true;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
    }

    /// <summary>
    /// Gets the world-space center position of a grid cell.
    /// </summary>
    public Vector3 GetCellWorldCenter(Vector2Int cell)
    {
        float x = GridOrigin.x + cell.x * cellSize + cellSize / 2f;
        float z = GridOrigin.z + cell.y * cellSize + cellSize / 2f;
        float y = terrain != null ? terrain.SampleHeight(new Vector3(x, 0, z)) + terrainOrigin.y : terrainOrigin.y;
        return new Vector3(x, y, z);
    }

    /// <summary>
    /// Snaps a world position to the nearest grid cell center for a building of given size.
    /// For multi-cell buildings, the snap point is the bottom-left cell's center.
    /// </summary>
    public Vector3 SnapToGrid(Vector3 worldPosition, int buildingSize = 1)
    {
        float localX = worldPosition.x - GridOrigin.x;
        float localZ = worldPosition.z - GridOrigin.z;

        int cellX = Mathf.FloorToInt(localX / cellSize);
        int cellZ = Mathf.FloorToInt(localZ / cellSize);

        cellX = Mathf.Clamp(cellX, 0, gridWidth - buildingSize);
        cellZ = Mathf.Clamp(cellZ, 0, gridHeight - buildingSize);

        float snappedX = GridOrigin.x + cellX * cellSize + (buildingSize * cellSize) / 2f;
        float snappedZ = GridOrigin.z + cellZ * cellSize + (buildingSize * cellSize) / 2f;
        float snappedY = terrain != null ? terrain.SampleHeight(new Vector3(snappedX, 0, snappedZ)) + terrainOrigin.y : terrainOrigin.y;

        return new Vector3(snappedX, snappedY, snappedZ);
    }

    /// <summary>
    /// Gets the bottom-left grid cell for a world position.
    /// </summary>
    public Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        int cellX = Mathf.FloorToInt((worldPosition.x - GridOrigin.x) / cellSize);
        int cellZ = Mathf.FloorToInt((worldPosition.z - GridOrigin.z) / cellSize);
        cellX = Mathf.Clamp(cellX, 0, gridWidth - 1);
        cellZ = Mathf.Clamp(cellZ, 0, gridHeight - 1);
        return new Vector2Int(cellX, cellZ);
    }

    /// <summary>
    /// Gets the bottom-left grid cell for a snapped building position.
    /// </summary>
    public Vector2Int GetBuildingGridCell(Vector3 buildingCenter, int buildingSize)
    {
        float bottomLeftX = buildingCenter.x - (buildingSize * cellSize) / 2f;
        float bottomLeftZ = buildingCenter.z - (buildingSize * cellSize) / 2f;
        int cellX = Mathf.RoundToInt((bottomLeftX - GridOrigin.x) / cellSize);
        int cellZ = Mathf.RoundToInt((bottomLeftZ - GridOrigin.z) / cellSize);
        cellX = Mathf.Clamp(cellX, 0, gridWidth - buildingSize);
        cellZ = Mathf.Clamp(cellZ, 0, gridHeight - buildingSize);
        return new Vector2Int(cellX, cellZ);
    }

    /// <summary>
    /// Checks if a cell is available for building.
    /// </summary>
    public bool IsCellAvailable(Vector2Int cell)
    {
        if (cell.x < 0 || cell.x >= gridWidth || cell.y < 0 || cell.y >= gridHeight)
            return false;
        return !occupiedCells[cell.x, cell.y];
    }

    /// <summary>
    /// Checks if a square area of cells is available.
    /// </summary>
    public bool IsAreaAvailable(Vector2Int startCell, int size)
    {
        for (int x = startCell.x; x < startCell.x + size; x++)
        {
            for (int z = startCell.y; z < startCell.y + size; z++)
            {
                if (!IsCellAvailable(new Vector2Int(x, z)))
                    return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Checks if a square area is available, ignoring cells occupied by a specific building.
    /// Used when relocating a building so it can be placed back on its own cells.
    /// </summary>
    public bool IsAreaAvailable(Vector2Int startCell, int size, PlacedBuilding ignore)
    {
        for (int x = startCell.x; x < startCell.x + size; x++)
        {
            for (int z = startCell.y; z < startCell.y + size; z++)
            {
                Vector2Int cell = new Vector2Int(x, z);
                if (cell.x < 0 || cell.x >= gridWidth || cell.y < 0 || cell.y >= gridHeight)
                    return false;

                if (occupiedCells[cell.x, cell.y])
                {
                    // Check if this cell belongs to the building we're ignoring
                    if (ignore != null && IsCellOwnedBy(cell, ignore))
                        continue;
                    return false;
                }
            }
        }
        return true;
    }

    /// <summary>
    /// Checks if a cell is within the footprint of a given building.
    /// </summary>
    private bool IsCellOwnedBy(Vector2Int cell, PlacedBuilding building)
    {
        return cell.x >= building.gridCell.x &&
               cell.x < building.gridCell.x + building.sizeInCells &&
               cell.y >= building.gridCell.y &&
               cell.y < building.gridCell.y + building.sizeInCells;
    }

    /// <summary>
    /// Marks cells as occupied.
    /// </summary>
    public void OccupyCells(Vector2Int startCell, int size)
    {
        for (int x = startCell.x; x < startCell.x + size; x++)
        {
            for (int z = startCell.y; z < startCell.y + size; z++)
            {
                if (x >= 0 && x < gridWidth && z >= 0 && z < gridHeight)
                    occupiedCells[x, z] = true;
            }
        }
    }

    /// <summary>
    /// Marks cells as free. Skips permanently blocked cells (environment objects).
    /// </summary>
    public void FreeCells(Vector2Int startCell, int size)
    {
        for (int x = startCell.x; x < startCell.x + size; x++)
        {
            for (int z = startCell.y; z < startCell.y + size; z++)
            {
                if (x >= 0 && x < gridWidth && z >= 0 && z < gridHeight && !permanentCells[x, z])
                    occupiedCells[x, z] = false;
            }
        }
    }

    /// <summary>
    /// Marks cells as permanently occupied (for environment objects like mountains, rivers, etc.).
    /// These cells cannot be freed by FreeCells.
    /// </summary>
    public void OccupyCellsPermanent(Vector2Int startCell, int size)
    {
        for (int x = startCell.x; x < startCell.x + size; x++)
        {
            for (int z = startCell.y; z < startCell.y + size; z++)
            {
                if (x >= 0 && x < gridWidth && z >= 0 && z < gridHeight)
                {
                    occupiedCells[x, z] = true;
                    permanentCells[x, z] = true;
                }
            }
        }
    }

    /// <summary>
    /// Checks if a cell is permanently blocked by an environment object.
    /// </summary>
    public bool IsCellPermanent(Vector2Int cell)
    {
        if (cell.x < 0 || cell.x >= gridWidth || cell.y < 0 || cell.y >= gridHeight)
            return false;
        return permanentCells[cell.x, cell.y];
    }

    /// <summary>
    /// Toggles grid line visibility at runtime.
    /// </summary>
    public void SetGridVisible(bool visible)
    {
        showGrid = visible;
        if (gridLinesParent != null)
            gridLinesParent.SetActive(visible);
    }
}
