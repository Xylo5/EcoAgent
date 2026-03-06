using UnityEngine;

/// <summary>
/// Attached to each placed building in the scene (CoC-style).
/// Tracks grid position, supports pick-up/put-down for relocation.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class PlacedBuilding : MonoBehaviour
{
    [HideInInspector] public Vector2Int gridCell;     // Bottom-left cell
    [HideInInspector] public int sizeInCells;
    [HideInInspector] public BuildingData buildingData;

    // For relocation — store original position in case cancel
    [HideInInspector] public Vector3 originalPosition;
    [HideInInspector] public Vector2Int originalGridCell;

    private float liftHeight = 1.5f; // How high the building lifts when picked up
    private bool isPickedUp = false;

    /// <summary>
    /// Pick up this building for relocation. Lifts it slightly.
    /// </summary>
    public void PickUp(GridManager gridManager)
    {
        if (isPickedUp) return;

        isPickedUp = true;
        originalPosition = transform.position;
        originalGridCell = gridCell;

        // Free the cells so they don't block the new placement check
        gridManager.FreeCells(gridCell, sizeInCells);

        // Lift the building visually
        transform.position += Vector3.up * liftHeight;
    }

    /// <summary>
    /// Confirm placement at the new position.
    /// </summary>
    public void PutDown(GridManager gridManager, Vector3 newPosition, Vector2Int newCell)
    {
        isPickedUp = false;
        transform.position = newPosition;
        gridCell = newCell;

        // Occupy the new cells
        gridManager.OccupyCells(gridCell, sizeInCells);
    }

    /// <summary>
    /// Cancel relocation — return to original position.
    /// </summary>
    public void CancelMove(GridManager gridManager)
    {
        isPickedUp = false;
        transform.position = originalPosition;
        gridCell = originalGridCell;

        // Re-occupy original cells
        gridManager.OccupyCells(gridCell, sizeInCells);
    }

    /// <summary>
    /// Demolish this building and free its cells.
    /// </summary>
    public void Demolish(GridManager gridManager)
    {
        gridManager.FreeCells(gridCell, sizeInCells);
        Debug.Log($"Demolished {buildingData.buildingName} at ({gridCell.x}, {gridCell.y})");
        Destroy(gameObject);
    }
}
