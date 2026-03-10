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
    public Color gridColor = new Color(0f, 1f, 0f, 0.15f);
    public bool showGrid = true;

    [Header("References")]
    public Terrain terrain;

    // 2D array tracking which cells are occupied (true = occupied)
    private bool[,] occupiedCells;

    // 2D array tracking permanently blocked cells (environment objects)
    private bool[,] permanentCells;

    // Cached material for grid line drawing
    private Material lineMat;

    // Cached terrain origin — terrain is static, so safe to cache
    private Vector3 terrainOrigin;

    void Awake()
    {
        // Auto-compute cell size from terrain dimensions
        if (terrain != null && terrain.terrainData != null)
        {
            Vector3 terrainSize = terrain.terrainData.size;
            cellSize = Mathf.Min(terrainSize.x / gridWidth, terrainSize.z / gridHeight);
            Debug.Log($"[GridManager] Terrain size: {terrainSize.x}x{terrainSize.z}, Cell size: {cellSize}");
        }
        else
        {
            cellSize = 10f; // Fallback
            Debug.LogWarning("[GridManager] No terrain assigned! Using fallback cellSize=10.");
        }

        occupiedCells = new bool[gridWidth, gridHeight];
        permanentCells = new bool[gridWidth, gridHeight];
        CreateLineMaterial();

        terrainOrigin = (terrain != null) ? terrain.transform.position : Vector3.zero;
    }

    private void CreateLineMaterial()
    {
        // Try built-in shader first, then fall back to shaders available in URP
        Shader shader = Shader.Find("Hidden/Internal-Colored");
        if (shader == null)
        {
            Debug.LogWarning("[GridManager] 'Hidden/Internal-Colored' shader not found (URP?). Trying fallback.");
            shader = Shader.Find("Sprites/Default");
        }
        if (shader == null)
        {
            shader = Shader.Find("UI/Default");
        }
        if (shader == null)
        {
            Debug.LogError("[GridManager] No suitable shader found for grid lines! Grid will not render.");
            return;
        }

        lineMat = new Material(shader);
        lineMat.hideFlags = HideFlags.HideAndDontSave;
        lineMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        lineMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        lineMat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        lineMat.SetInt("_ZWrite", 0);
    }

    /// <summary>
    /// Gets the world-space center position of a grid cell.
    /// </summary>
    public Vector3 GetCellWorldCenter(Vector2Int cell)
    {
        float x = terrainOrigin.x + cell.x * cellSize + cellSize / 2f;
        float z = terrainOrigin.z + cell.y * cellSize + cellSize / 2f;
        float y = terrain.SampleHeight(new Vector3(x, 0, z)) + terrainOrigin.y;
        return new Vector3(x, y, z);
    }

    /// <summary>
    /// Snaps a world position to the nearest grid cell center for a building of given size.
    /// For multi-cell buildings, the snap point is the bottom-left cell's center.
    /// </summary>
    public Vector3 SnapToGrid(Vector3 worldPosition, int buildingSize = 1)
    {
        float localX = worldPosition.x - terrainOrigin.x;
        float localZ = worldPosition.z - terrainOrigin.z;

        int cellX = Mathf.FloorToInt(localX / cellSize);
        int cellZ = Mathf.FloorToInt(localZ / cellSize);

        cellX = Mathf.Clamp(cellX, 0, gridWidth - buildingSize);
        cellZ = Mathf.Clamp(cellZ, 0, gridHeight - buildingSize);

        float snappedX = terrainOrigin.x + cellX * cellSize + (buildingSize * cellSize) / 2f;
        float snappedZ = terrainOrigin.z + cellZ * cellSize + (buildingSize * cellSize) / 2f;
        float snappedY = terrain.SampleHeight(new Vector3(snappedX, 0, snappedZ)) + terrainOrigin.y;

        return new Vector3(snappedX, snappedY, snappedZ);
    }

    /// <summary>
    /// Gets the bottom-left grid cell for a world position.
    /// </summary>
    public Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        int cellX = Mathf.FloorToInt((worldPosition.x - terrainOrigin.x) / cellSize);
        int cellZ = Mathf.FloorToInt((worldPosition.z - terrainOrigin.z) / cellSize);
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
        int cellX = Mathf.RoundToInt((bottomLeftX - terrainOrigin.x) / cellSize);
        int cellZ = Mathf.RoundToInt((bottomLeftZ - terrainOrigin.z) / cellSize);
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
    /// Draws the grid lines using GL (visible in Game view).
    /// </summary>
    void OnRenderObject()
    {
        if (!showGrid || lineMat == null) return;
        lineMat.SetPass(0);

        Vector3 origin = terrainOrigin;
        float yOffset = 0.05f;

        GL.PushMatrix();
        GL.Begin(GL.LINES);
        GL.Color(gridColor);

        for (int x = 0; x <= gridWidth; x++)
        {
            Vector3 start = origin + new Vector3(x * cellSize, yOffset, 0);
            Vector3 end = origin + new Vector3(x * cellSize, yOffset, gridHeight * cellSize);
            GL.Vertex(start);
            GL.Vertex(end);
        }

        for (int z = 0; z <= gridHeight; z++)
        {
            Vector3 start = origin + new Vector3(0, yOffset, z * cellSize);
            Vector3 end = origin + new Vector3(gridWidth * cellSize, yOffset, z * cellSize);
            GL.Vertex(start);
            GL.Vertex(end);
        }

        GL.End();
        GL.PopMatrix();
    }
}
