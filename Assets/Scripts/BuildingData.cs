using UnityEngine;

/// <summary>
/// Defines a building type with its size and prefab (CoC-style).
/// Buildings are always square (1x1, 2x2, 3x3, etc.) and don't rotate.
/// Create instances via Assets > Create > Building > BuildingData.
/// </summary>
[CreateAssetMenu(fileName = "NewBuilding", menuName = "Building/BuildingData")]
public class BuildingData : ScriptableObject
{
    [Header("Building Info")]
    public string buildingName = "New Building";
    public Sprite icon; // UI icon for the building shop panel

    [Header("Grid Size (square, in cells)")]
    [Range(1, 15)]
    public int sizeInCells = 1; // 1 = 1x1, 2 = 2x2, 3 = 3x3, etc.

    [Header("Prefab")]
    public GameObject prefab; // The 3D model prefab to instantiate

    [Header("Placement Colors")]
    public Color validColor = new Color(0f, 1f, 0f, 0.5f);   // Green
    public Color invalidColor = new Color(1f, 0f, 0f, 0.5f);  // Red

    [Header("Environment Impact")]
    [Tooltip("Pollution value. Positive = pollutes, Negative = cleans/absorbs pollution.")]
    public int pollutionValue = 0;
}
