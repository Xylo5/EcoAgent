using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility to update all game data with SimplePoly prefabs and auto-calculate their grid sizes.
/// </summary>
public class SimplePolySetup : EditorWindow
{
    private const float CellSize = 2.5f;

    [MenuItem("Tools/Apply SimplePoly Asset Prefabs")]
    public static void ApplySettings()
    {
        Debug.Log("--- Starting SimplePoly Asset Setup ---");

        // Buildings
        UpdateBuilding("Assets/Hut.asset", "Assets/SimplePoly City - Low Poly Assets/Prefab/Buildings/Building_Fast Food.prefab");
        UpdateBuilding("Assets/House.asset", "Assets/SimplePoly City - Low Poly Assets/Prefab/Buildings/Building_House_01_color01.prefab");
        UpdateBuilding("Assets/Office.asset", "Assets/SimplePoly City - Low Poly Assets/Prefab/Buildings/Building_Super Market.prefab");
        UpdateBuilding("Assets/Fact.asset", "Assets/SimplePoly City - Low Poly Assets/Prefab/Buildings/Building_Factory.prefab");

        // Environment
        UpdateEnvironment("Assets/EnvironmentData/Mountain.asset", "Assets/SimplePoly City - Low Poly Assets/Prefab/Natures/Natures_Rock_Big.prefab");
        UpdateEnvironment("Assets/EnvironmentData/Pond.asset", "Assets/SimplePoly City - Low Poly Assets/Prefab/Natures/Natures_Bush_big.prefab"); // Using bush as pond placeholder
        UpdateEnvironment("Assets/EnvironmentData/Tree.asset", "Assets/SimplePoly City - Low Poly Assets/Prefab/Natures/Natures_Big Tree.prefab");
        // For Road and RiverSegment in EnvironmentData, use specific pieces
        UpdateEnvironment("Assets/EnvironmentData/Road.asset", "Assets/SimplePoly City - Low Poly Assets/Prefab/Roads/Road Tile.prefab");
        UpdateEnvironment("Assets/EnvironmentData/RiverSegment.asset", "Assets/SimplePoly City - Low Poly Assets/Prefab/Roads/Road Split Line.prefab"); // Using split line as river placeholder

        // City Tiles (WFC)
        UpdateCityTile("Assets/CityData/Residential.asset", "Assets/SimplePoly City - Low Poly Assets/Prefab/Buildings/Building_House_02_color01.prefab");
        UpdateCityTile("Assets/CityData/Industrial.asset", "Assets/SimplePoly City - Low Poly Assets/Prefab/Buildings/Building_Factory.prefab");
        UpdateCityTile("Assets/CityData/Park.asset", "Assets/SimplePoly City - Low Poly Assets/Prefab/Natures/Natures_Fir Tree.prefab");
        UpdateCityTile("Assets/CityData/Road.asset", "Assets/SimplePoly City - Low Poly Assets/Prefab/Roads/Road Tile.prefab");
        UpdateCityTile("Assets/CityData/EmptyLot.asset", "Assets/SimplePoly City - Low Poly Assets/Prefab/Natures/Natures_Grass Tile.prefab");

        AssetDatabase.SaveAssets();
        Debug.Log("--- SimplePoly Asset Setup Complete! ---");
    }

    private static int CalculateSizeInCells(GameObject prefab)
    {
        if (prefab == null) return 1;

        GameObject instance = Instantiate(prefab);
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
        
        int size = 1;

        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            // SimplePoly roads are 5x5. 5 / 2.5 = 2 cells.
            // Small buildings might be 5x5, large 10x10.
            float maxSize = Mathf.Max(bounds.size.x, bounds.size.z);
            
            // Avoid 0 size, round cleanly to nearest cell to ignore minor inaccuracies
            size = Mathf.Max(1, Mathf.RoundToInt(maxSize / CellSize));
        }

        DestroyImmediate(instance);
        return size;
    }

    private static void UpdateBuilding(string assetPath, string prefabPath)
    {
        BuildingData data = AssetDatabase.LoadAssetAtPath<BuildingData>(assetPath);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (data != null && prefab != null)
        {
            data.prefab = prefab;
            data.sizeInCells = CalculateSizeInCells(prefab);
            EditorUtility.SetDirty(data);
            Debug.Log($"Updated Building: {data.buildingName} -> Size {data.sizeInCells} ({prefab.name})");
        }
        else Debug.LogWarning($"Failed to update {assetPath} or {prefabPath}");
    }

    private static void UpdateEnvironment(string assetPath, string prefabPath)
    {
        EnvironmentData data = AssetDatabase.LoadAssetAtPath<EnvironmentData>(assetPath);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (data != null && prefab != null)
        {
            data.prefab = prefab;
            data.sizeInCells = CalculateSizeInCells(prefab);
            EditorUtility.SetDirty(data);
            Debug.Log($"Updated Env: {data.objectName} -> Size {data.sizeInCells} ({prefab.name})");
        }
        else Debug.LogWarning($"Failed to update {assetPath} or {prefabPath}");
    }

    private static void UpdateCityTile(string assetPath, string prefabPath)
    {
        CityTileData data = AssetDatabase.LoadAssetAtPath<CityTileData>(assetPath);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (data != null && prefab != null)
        {
            data.prefabs = new GameObject[] { prefab };
            data.sizeInCells = CalculateSizeInCells(prefab);
            EditorUtility.SetDirty(data);
            Debug.Log($"Updated CityTile: {data.tileName} -> Size {data.sizeInCells} ({prefab.name})");
        }
        else Debug.LogWarning($"Failed to update {assetPath} or {prefabPath}");
    }
}
