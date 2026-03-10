using UnityEngine;

/// <summary>
/// Attached to each placed environment object at runtime.
/// Tracks grid position. Unlike PlacedBuilding, these are permanent
/// and cannot be picked up, moved, or demolished.
/// </summary>
public class EnvironmentObject : MonoBehaviour
{
    [HideInInspector] public Vector2Int gridCell;       // Bottom-left cell
    [HideInInspector] public int sizeInCells;
    [HideInInspector] public EnvironmentData environmentData;
}
