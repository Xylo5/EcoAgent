using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

/// <summary>
/// Editor utility: Tools → EcoAgent → Setup City Generator in Level_0
/// Opens Level_0, finds or creates a WFCCityGenerator GameObject,
/// wires it to the existing GridManager and terrain, assigns the
/// DefaultAdjacencyRules asset, and saves the scene.
/// </summary>
public class AddCityGeneratorToLevel : EditorWindow
{
    private const string LevelScenePath = "Assets/Scenes/Level_0.unity";
    private const string AdjacencyRulePath = "Assets/CityData/DefaultAdjacencyRules.asset";

    [MenuItem("Tools/EcoAgent/Setup City Generator in Level_0")]
    public static void SetupCityGenerator()
    {
        // ── Validate scene file exists ──
        if (!File.Exists(LevelScenePath))
        {
            EditorUtility.DisplayDialog("Error",
                "Level_0.unity not found at:\n" + LevelScenePath +
                "\n\nRun Tools > Build All Scenes first.", "OK");
            return;
        }

        // ── Save current scene and open Level_0 ──
        EditorSceneManager.SaveOpenScenes();
        var scene = EditorSceneManager.OpenScene(LevelScenePath, OpenSceneMode.Single);

        // ── Find existing GridManager in scene ──
        GridManager gridManager = Object.FindFirstObjectByType<GridManager>();
        if (gridManager == null)
        {
            // Create one if missing
            GameObject gmGO = new GameObject("GridManager");
            gridManager = gmGO.AddComponent<GridManager>();

            // Auto-assign terrain
            Terrain terrain = Object.FindFirstObjectByType<Terrain>();
            if (terrain != null)
                gridManager.terrain = terrain;

            Debug.Log("[AddCityGenerator] Created new GridManager in scene.");
        }

        // Ensure terrain is assigned
        if (gridManager.terrain == null)
        {
            Terrain terrain = Object.FindFirstObjectByType<Terrain>();
            if (terrain != null)
            {
                gridManager.terrain = terrain;
                EditorUtility.SetDirty(gridManager);
            }
        }

        // ── Check if WFCCityGenerator already exists ──
        WFCCityGenerator existingGenerator = Object.FindFirstObjectByType<WFCCityGenerator>();
        if (existingGenerator != null)
        {
            Debug.Log("[AddCityGenerator] WFCCityGenerator already exists in scene. Updating references...");
            WireGenerator(existingGenerator, gridManager);
            EditorSceneManager.SaveScene(scene);
            EditorUtility.DisplayDialog("Updated",
                "WFCCityGenerator already existed.\nReferences updated and scene saved.\n\n" +
                "Select the CityGenerator object and click 'Generate City' in the Inspector.",
                "OK");
            return;
        }

        // ── Create the generator GameObject ──
        GameObject generatorGO = new GameObject("CityGenerator");
        WFCCityGenerator generator = generatorGO.AddComponent<WFCCityGenerator>();

        // Position at terrain origin so it's easy to find in hierarchy
        if (gridManager.terrain != null)
            generatorGO.transform.position = gridManager.terrain.transform.position;

        WireGenerator(generator, gridManager);

        // ── Save scene ──
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[AddCityGenerator] ✓ CityGenerator added to Level_0 and wired to GridManager.");
        EditorUtility.DisplayDialog("Done",
            "CityGenerator added to Level_0!\n\n" +
            "1. Select 'CityGenerator' in the Hierarchy\n" +
            "2. Click 'Generate City' in the Inspector\n" +
            "3. Click 'Clear City' to remove generated tiles",
            "OK");

        // Select the new object so user can immediately see it
        Selection.activeGameObject = generatorGO;
    }

    /// <summary>
    /// Wires the generator's references to GridManager and adjacency rules.
    /// </summary>
    private static void WireGenerator(WFCCityGenerator generator, GridManager gridManager)
    {
        generator.gridManager = gridManager;

        // Load adjacency rule asset
        CityTileAdjacencyRule ruleSet = AssetDatabase.LoadAssetAtPath<CityTileAdjacencyRule>(AdjacencyRulePath);
        if (ruleSet != null)
        {
            generator.adjacencyRuleSet = ruleSet;
        }
        else
        {
            Debug.LogWarning("[AddCityGenerator] DefaultAdjacencyRules.asset not found at " + AdjacencyRulePath +
                ". Run Tools > EcoAgent > Create City Tile Assets first.");
        }

        EditorUtility.SetDirty(generator);
    }
}
