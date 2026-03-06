using UnityEngine;

/// <summary>
/// Manages a 40x40 grid on the terrain (CoC-style).
/// Draws grid lines and provides snap-to-grid utilities.
/// Attach this to an empty GameObject named "GridManager".
/// </summary>
public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridWidth = 40;
    public int gridHeight = 40;
    public float cellSize = 2.5f; // Each cell = 2.5 world units (40 * 2.5 = 100)

    [Header("Grid Visual")]
    public Color gridColor = new Color(0f, 1f, 0f, 0.15f);
    public bool showGrid = true;

    [Header("References")]
    public Terrain terrain;

    // 2D array tracking which cells are occupied (true = occupied)
    private bool[,] occupiedCells;

    // Cached material for grid line drawing
    private Material lineMat;

    void Awake()
    {
        occupiedCells = new bool[gridWidth, gridHeight];
        CreateLineMaterial();
    }

    private void CreateLineMaterial()
    {
        Shader shader = Shader.Find("Hidden/Internal-Colored");
        if (shader == null) return;
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
        Vector3 terrainPos = terrain.transform.position;
        float x = terrainPos.x + cell.x * cellSize + cellSize / 2f;
        float z = terrainPos.z + cell.y * cellSize + cellSize / 2f;
        float y = terrain.SampleHeight(new Vector3(x, 0, z)) + terrainPos.y;
        return new Vector3(x, y, z);
    }

    /// <summary>
    /// Snaps a world position to the nearest grid cell center for a building of given size.
    /// For multi-cell buildings, the snap point is the bottom-left cell's center.
    /// </summary>
    public Vector3 SnapToGrid(Vector3 worldPosition, int buildingSize = 1)
    {
        Vector3 terrainPos = terrain.transform.position;

        // Calculate grid-local position
        float localX = worldPosition.x - terrainPos.x;
        float localZ = worldPosition.z - terrainPos.z;

        // Snap to bottom-left cell
        int cellX = Mathf.FloorToInt(localX / cellSize);
        int cellZ = Mathf.FloorToInt(localZ / cellSize);

        // Clamp so the building fits within grid bounds
        cellX = Mathf.Clamp(cellX, 0, gridWidth - buildingSize);
        cellZ = Mathf.Clamp(cellZ, 0, gridHeight - buildingSize);

        // Return world position at the center of the building's footprint
        float snappedX = terrainPos.x + cellX * cellSize + (buildingSize * cellSize) / 2f;
        float snappedZ = terrainPos.z + cellZ * cellSize + (buildingSize * cellSize) / 2f;
        float snappedY = terrain.SampleHeight(new Vector3(snappedX, 0, snappedZ)) + terrainPos.y;

        return new Vector3(snappedX, snappedY, snappedZ);
    }

    /// <summary>
    /// Gets the bottom-left grid cell for a world position.
    /// </summary>
    public Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        Vector3 terrainPos = terrain.transform.position;
        int cellX = Mathf.FloorToInt((worldPosition.x - terrainPos.x) / cellSize);
        int cellZ = Mathf.FloorToInt((worldPosition.z - terrainPos.z) / cellSize);
        cellX = Mathf.Clamp(cellX, 0, gridWidth - 1);
        cellZ = Mathf.Clamp(cellZ, 0, gridHeight - 1);
        return new Vector2Int(cellX, cellZ);
    }

    /// <summary>
    /// Gets the bottom-left grid cell for a snapped building position.
    /// </summary>
    public Vector2Int GetBuildingGridCell(Vector3 buildingCenter, int buildingSize)
    {
        Vector3 terrainPos = terrain.transform.position;
        float bottomLeftX = buildingCenter.x - (buildingSize * cellSize) / 2f;
        float bottomLeftZ = buildingCenter.z - (buildingSize * cellSize) / 2f;
        int cellX = Mathf.RoundToInt((bottomLeftX - terrainPos.x) / cellSize);
        int cellZ = Mathf.RoundToInt((bottomLeftZ - terrainPos.z) / cellSize);
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
    /// Marks cells as free.
    /// </summary>
    public void FreeCells(Vector2Int startCell, int size)
    {
        for (int x = startCell.x; x < startCell.x + size; x++)
        {
            for (int z = startCell.y; z < startCell.y + size; z++)
            {
                if (x >= 0 && x < gridWidth && z >= 0 && z < gridHeight)
                    occupiedCells[x, z] = false;
            }
        }
    }

    /// <summary>
    /// Draws the grid lines using GL (visible in Game view).
    /// </summary>
    void OnRenderObject()
    {
        if (!showGrid || lineMat == null) return;
        lineMat.SetPass(0);

        Vector3 origin = terrain.transform.position;
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
