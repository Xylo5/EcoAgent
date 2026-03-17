using UnityEngine;

/// <summary>
/// Per-level configuration of required buildings.
/// Create one asset per level via Assets > Create > Building > LevelRequirements.
/// Assign to RequiredBuildingsUI in the scene.
/// </summary>
[CreateAssetMenu(fileName = "NewLevelRequirements", menuName = "Building/LevelRequirements")]
public class LevelRequirements : ScriptableObject
{
    [Header("Required Buildings")]
    [Tooltip("Buildings the player must place to win this level.")]
    public BuildingData[] requiredBuildings;
}
