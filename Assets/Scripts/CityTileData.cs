using UnityEngine;

/// <summary>
/// Defines a city tile type with a name and one or more prefab variants.
/// The WFC generator picks a random variant when placing a tile for visual variety.
/// Create instances via Assets > Create > City > CityTileData.
/// </summary>
[CreateAssetMenu(fileName = "NewCityTile", menuName = "City/CityTileData")]
public class CityTileData : ScriptableObject
{
    [Header("Tile Info")]
    public string tileName = "New Tile";

    [Header("Grid Size (square, in cells)")]
    [Range(1, 10)]
    public int sizeInCells = 1;

    [Header("Prefab Variants")]
    [Tooltip("One or more prefab variants. A random one is chosen per placement.")]
    public GameObject[] prefabs;

    /// <summary>
    /// Returns a random prefab from the variants array.
    /// </summary>
    public GameObject GetRandomPrefab()
    {
        if (prefabs == null || prefabs.Length == 0) return null;
        return prefabs[Random.Range(0, prefabs.Length)];
    }
}
