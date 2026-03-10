using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns environment objects at scene start based on a configurable list.
/// Attach to an empty GameObject in the Level 0 scene.
/// Configure placements in the Inspector — each entry specifies what to place and where.
/// </summary>
public class EnvironmentSpawner : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;

    [Header("Environment Placements")]
    public List<EnvironmentPlacement> placements = new List<EnvironmentPlacement>();

    void Start()
    {
        SpawnAll();
    }

    /// <summary>
    /// Instantiates all configured environment objects and marks their cells as permanently occupied.
    /// </summary>
    private void SpawnAll()
    {
        foreach (var placement in placements)
        {
            if (placement.data == null)
            {
                Debug.LogWarning("[EnvironmentSpawner] Skipping null EnvironmentData entry.");
                continue;
            }

            if (placement.data.prefab == null)
            {
                Debug.LogWarning($"[EnvironmentSpawner] '{placement.data.objectName}' has no prefab assigned. Skipping.");
                continue;
            }

            int size = placement.data.sizeInCells;

            // Check if the area is available
            if (!gridManager.IsAreaAvailable(placement.gridCell, size))
            {
                Debug.LogWarning($"[EnvironmentSpawner] Cannot place '{placement.data.objectName}' at ({placement.gridCell.x}, {placement.gridCell.y}) — cells already occupied. Skipping.");
                continue;
            }

            // Calculate world position (same logic as BuildingPlacer)
            Vector3 worldPos = GetWorldPosition(placement.gridCell, size);

            // Instantiate the environment object
            GameObject obj = Instantiate(placement.data.prefab, worldPos, Quaternion.identity);
            obj.name = "Env_" + placement.data.objectName;

            // Attach the EnvironmentObject component
            EnvironmentObject envObj = obj.AddComponent<EnvironmentObject>();
            envObj.gridCell = placement.gridCell;
            envObj.sizeInCells = size;
            envObj.environmentData = placement.data;

            // Permanently occupy the cells
            gridManager.OccupyCellsPermanent(placement.gridCell, size);

            Debug.Log($"[EnvironmentSpawner] Placed '{placement.data.objectName}' at cell ({placement.gridCell.x}, {placement.gridCell.y})");
        }
    }

    /// <summary>
    /// Converts a grid cell + size to world position (center of footprint).
    /// </summary>
    private Vector3 GetWorldPosition(Vector2Int cell, int size)
    {
        Vector3 terrainPos = gridManager.terrain.transform.position;
        float x = terrainPos.x + cell.x * gridManager.cellSize + (size * gridManager.cellSize) / 2f;
        float z = terrainPos.z + cell.y * gridManager.cellSize + (size * gridManager.cellSize) / 2f;
        float y = gridManager.terrain.SampleHeight(new Vector3(x, 0, z)) + terrainPos.y;
        return new Vector3(x, y, z);
    }
}

/// <summary>
/// Serializable struct for configuring an environment placement in the Inspector.
/// </summary>
[System.Serializable]
public struct EnvironmentPlacement
{
    public EnvironmentData data;
    public Vector2Int gridCell; // Bottom-left cell
}
