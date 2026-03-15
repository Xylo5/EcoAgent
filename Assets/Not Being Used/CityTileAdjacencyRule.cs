using UnityEngine;
using System;

/// <summary>
/// Holds a complete set of adjacency rules for the WFC city generator.
/// Each entry maps a tile type to the tile types allowed next to it.
/// Create via Assets > Create > City > CityTileAdjacencyRule.
/// </summary>
[CreateAssetMenu(fileName = "NewAdjacencyRule", menuName = "City/CityTileAdjacencyRule")]
public class CityTileAdjacencyRule : ScriptableObject
{
    [Header("Available Tile Types")]
    [Tooltip("All tile types the generator can use.")]
    public CityTileData[] availableTiles;

    [Header("Adjacency Rules")]
    [Tooltip("For each tile, list which tiles are allowed as cardinal neighbours.")]
    public AdjacencyEntry[] adjacencyRules;

    /// <summary>
    /// Returns the allowed neighbours for the given tile, or null if not found.
    /// </summary>
    public CityTileData[] GetAllowedNeighbours(CityTileData tile)
    {
        if (adjacencyRules == null) return null;

        for (int i = 0; i < adjacencyRules.Length; i++)
        {
            if (adjacencyRules[i].tile == tile)
                return adjacencyRules[i].allowedNeighbours;
        }
        return null;
    }
}

/// <summary>
/// Maps a single tile to its allowed neighbours.
/// </summary>
[Serializable]
public class AdjacencyEntry
{
    [Tooltip("The tile this rule applies to.")]
    public CityTileData tile;

    [Tooltip("Tiles allowed next to this tile (N/S/E/W).")]
    public CityTileData[] allowedNeighbours;
}
