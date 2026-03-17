using UnityEngine;

/// <summary>
/// Defines a building type with its size and prefab (CoC-style).
/// Buildings can be rectangular (e.g., 7x9, 3x5) or square.
/// Create instances via Assets > Create > Building > BuildingData.
/// </summary>
[CreateAssetMenu(fileName = "NewBuilding", menuName = "Building/BuildingData")]
public class BuildingData : ScriptableObject
{
    [Header("Building Info")]
    public string buildingName = "New Building";
    [TextArea(2, 5)]
    [Tooltip("Description shown in the building shop tooltip on hover.")]
    public string description = "A useful building for your city.";
    public Sprite icon; // UI icon for the building shop panel

    [Header("Grid Size (in cells)")]
    [Range(1, 15)]
    public int sizeX = 1; // Width in cells (along X axis)
    [Range(1, 15)]
    public int sizeZ = 1; // Depth in cells (along Z axis)

    [Header("Prefab")]
    public GameObject prefab; // The 3D model prefab to instantiate

    [Header("Scale")]
    [Tooltip("Extra scale multiplier after auto-fitting to grid. >1 = cartoon overfill, 1 = exact fit.")]
    [Range(0.5f, 2f)]
    public float scaleMultiplier = 1.1f;

    [Header("Placement Colors")]
    public Color validColor = new Color(0f, 1f, 0f, 0.5f);   // Green
    public Color invalidColor = new Color(1f, 0f, 0f, 0.5f);  // Red

    [Header("Environment Impact")]
    [Tooltip("Pollution value. Positive = pollutes, Negative = cleans/absorbs pollution.")]
    public int pollutionValue = 0;

    [Header("City Generation")]
    [Range(1, 10)]
    [Tooltip("Spawn frequency weight for CityGenerator. Higher = more copies generated. Each type spawns at least once.")]
    public int frequency = 1;
}
