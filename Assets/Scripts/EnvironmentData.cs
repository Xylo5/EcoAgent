using UnityEngine;

/// <summary>
/// Defines an environment object type (mountain, tree, river segment, pond, road, etc.).
/// These objects permanently occupy grid cells and block building placement.
/// Create instances via Assets > Create > Environment > EnvironmentData.
/// </summary>
[CreateAssetMenu(fileName = "NewEnvironment", menuName = "Environment/EnvironmentData")]
public class EnvironmentData : ScriptableObject
{
    [Header("Environment Info")]
    public string objectName = "New Environment";

    [Header("Grid Size (square, in cells)")]
    [Range(1, 5)]
    public int sizeInCells = 1; // 1 = 1x1, 2 = 2x2, 3 = 3x3, etc.

    [Header("Prefab")]
    public GameObject prefab; // The 3D model/placeholder to instantiate
}
