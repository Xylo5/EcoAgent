using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor utility to generate placeholder environment prefabs and ScriptableObject assets.
/// Access via menu: Tools > EcoAgent > Create Environment Placeholders
/// </summary>
public class EnvironmentPlaceholderCreator : EditorWindow
{
    [MenuItem("Tools/EcoAgent/Create Environment Placeholders")]
    public static void CreatePlaceholders()
    {
        // Ensure directories exist
        EnsureDirectory("Assets/Prefabs/Environment");
        EnsureDirectory("Assets/EnvironmentData");

        // --- Create placeholder prefabs ---
        CreateMountainPrefab();
        CreateTreePrefab();
        CreateRiverSegmentPrefab();
        CreatePondPrefab();
        CreateRoadPrefab();

        // --- Create EnvironmentData ScriptableObject assets ---
        CreateEnvironmentDataAsset("Mountain", 3, "Assets/Prefabs/Environment/PH-Mountain.prefab");
        CreateEnvironmentDataAsset("Tree", 1, "Assets/Prefabs/Environment/PH-Tree.prefab");
        CreateEnvironmentDataAsset("RiverSegment", 1, "Assets/Prefabs/Environment/PH-RiverSegment.prefab");
        CreateEnvironmentDataAsset("Pond", 2, "Assets/Prefabs/Environment/PH-Pond.prefab");
        CreateEnvironmentDataAsset("Road", 1, "Assets/Prefabs/Environment/PH-Road.prefab");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[EnvironmentPlaceholderCreator] All placeholder prefabs and data assets created!");
        EditorUtility.DisplayDialog("Done", "Environment placeholders created!\n\nPrefabs: Assets/Prefabs/Environment/\nData: Assets/EnvironmentData/", "OK");
    }

    // ═══════════════════════════════════════════
    //  PREFAB CREATORS
    // ═══════════════════════════════════════════

    private static void CreateMountainPrefab()
    {
        // Brown cone-like shape (scaled cube as placeholder)
        GameObject mountain = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mountain.name = "PH-Mountain";
        mountain.transform.localScale = new Vector3(7f, 5f, 7f);
        mountain.transform.position = new Vector3(0, 2.5f, 0);

        SetColor(mountain, new Color(0.45f, 0.3f, 0.15f)); // Brown

        SavePrefab(mountain, "Assets/Prefabs/Environment/PH-Mountain.prefab");
        Object.DestroyImmediate(mountain);
    }

    private static void CreateTreePrefab()
    {
        // Green cylinder (trunk) + green sphere (canopy)
        GameObject tree = new GameObject("PH-Tree");

        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk";
        trunk.transform.SetParent(tree.transform);
        trunk.transform.localScale = new Vector3(0.3f, 1f, 0.3f);
        trunk.transform.localPosition = new Vector3(0, 1f, 0);
        SetColor(trunk, new Color(0.4f, 0.25f, 0.1f)); // Brown trunk

        GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        canopy.name = "Canopy";
        canopy.transform.SetParent(tree.transform);
        canopy.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
        canopy.transform.localPosition = new Vector3(0, 2.5f, 0);
        SetColor(canopy, new Color(0.1f, 0.55f, 0.1f)); // Green

        SavePrefab(tree, "Assets/Prefabs/Environment/PH-Tree.prefab");
        Object.DestroyImmediate(tree);
    }

    private static void CreateRiverSegmentPrefab()
    {
        // Flat blue cube (1x1 cell water segment)
        GameObject river = GameObject.CreatePrimitive(PrimitiveType.Cube);
        river.name = "PH-RiverSegment";
        river.transform.localScale = new Vector3(2.4f, 0.15f, 2.4f);
        river.transform.position = new Vector3(0, 0.075f, 0);

        SetColor(river, new Color(0.15f, 0.45f, 0.85f)); // Blue

        SavePrefab(river, "Assets/Prefabs/Environment/PH-RiverSegment.prefab");
        Object.DestroyImmediate(river);
    }

    private static void CreatePondPrefab()
    {
        // Flat cyan cube (2x2 cell water body)
        GameObject pond = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pond.name = "PH-Pond";
        pond.transform.localScale = new Vector3(4.8f, 0.15f, 4.8f);
        pond.transform.position = new Vector3(0, 0.075f, 0);

        SetColor(pond, new Color(0.1f, 0.6f, 0.75f)); // Cyan-blue

        SavePrefab(pond, "Assets/Prefabs/Environment/PH-Pond.prefab");
        Object.DestroyImmediate(pond);
    }

    private static void CreateRoadPrefab()
    {
        // Flat grey cube (1x1 cell road segment)
        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = "PH-Road";
        road.transform.localScale = new Vector3(2.4f, 0.1f, 2.4f);
        road.transform.position = new Vector3(0, 0.05f, 0);

        SetColor(road, new Color(0.4f, 0.4f, 0.4f)); // Grey

        SavePrefab(road, "Assets/Prefabs/Environment/PH-Road.prefab");
        Object.DestroyImmediate(road);
    }

    // ═══════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════

    private static void SetColor(GameObject obj, Color color)
    {
        Renderer rend = obj.GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = color;
            rend.sharedMaterial = mat;
        }
    }

    private static void SavePrefab(GameObject obj, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(obj, path);
    }

    private static void CreateEnvironmentDataAsset(string name, int size, string prefabPath)
    {
        EnvironmentData data = ScriptableObject.CreateInstance<EnvironmentData>();
        data.objectName = name;
        data.sizeInCells = size;
        data.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        string assetPath = $"Assets/EnvironmentData/{name}.asset";
        AssetDatabase.CreateAsset(data, assetPath);
    }

    private static void EnsureDirectory(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = Path.GetDirectoryName(path).Replace("\\", "/");
            string folder = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
