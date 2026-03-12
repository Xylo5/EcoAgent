using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor utility to auto-create CityTileData and CityTileAdjacencyRule assets
/// using prefabs from the SimplePoly City asset pack.
/// Access via menu: Tools > EcoAgent > Create City Tile Assets
/// </summary>
public class CityPlaceholderCreator : EditorWindow
{
    // Base path to SimplePoly City prefabs.
    private const string PrefabRoot = "Assets/SimplePoly City - Low Poly Assets/Prefab";

    [MenuItem("Tools/EcoAgent/Create City Tile Assets")]
    public static void CreateCityTileAssets()
    {
        // Ensure output directories exist.
        EnsureDirectory("Assets/CityData");

        // ── Create tile data assets ──
        CityTileData roadTile = CreateTileData("Road", new string[]
        {
            PrefabRoot + "/Roads/Road Lane_01.prefab",
            PrefabRoot + "/Roads/Road Lane_02.prefab",
            PrefabRoot + "/Roads/Road Lane_03.prefab",
            PrefabRoot + "/Roads/Road Lane_04.prefab"
        });

        CityTileData residentialTile = CreateTileData("Residential", new string[]
        {
            PrefabRoot + "/Buildings/Building_House_01_color01.prefab",
            PrefabRoot + "/Buildings/Building_House_01_color02.prefab",
            PrefabRoot + "/Buildings/Building_House_01_color03.prefab",
            PrefabRoot + "/Buildings/Building_House_02_color01.prefab",
            PrefabRoot + "/Buildings/Building_House_02_color02.prefab",
            PrefabRoot + "/Buildings/Building_House_02_color03.prefab"
        });

        CityTileData industrialTile = CreateTileData("Industrial", new string[]
        {
            PrefabRoot + "/Buildings/Building_Factory.prefab",
            PrefabRoot + "/Buildings/Building_Auto Service.prefab",
            PrefabRoot + "/Buildings/Building_Gas Station.prefab"
        });

        CityTileData parkTile = CreateTileData("Park", new string[]
        {
            PrefabRoot + "/Natures/Natures_Grass Tile.prefab",
            PrefabRoot + "/Natures/Natures_Big Tree.prefab",
            PrefabRoot + "/Natures/Natures_Fir Tree.prefab"
        });

        CityTileData emptyLotTile = CreateTileData("EmptyLot", new string[]
        {
            PrefabRoot + "/Roads/Road Concrete Tile.prefab",
            PrefabRoot + "/Roads/Road Concrete Tile Small.prefab"
        });

        // ── Create adjacency rule asset ──
        CityTileAdjacencyRule ruleSet = ScriptableObject.CreateInstance<CityTileAdjacencyRule>();

        ruleSet.availableTiles = new CityTileData[]
        {
            roadTile, residentialTile, industrialTile, parkTile, emptyLotTile
        };

        ruleSet.adjacencyRules = new AdjacencyEntry[]
        {
            // Road → Road, Residential, Industrial, EmptyLot
            new AdjacencyEntry
            {
                tile = roadTile,
                allowedNeighbours = new CityTileData[] { roadTile, residentialTile, industrialTile, emptyLotTile }
            },
            // Residential → Road, Residential, Park, EmptyLot
            new AdjacencyEntry
            {
                tile = residentialTile,
                allowedNeighbours = new CityTileData[] { roadTile, residentialTile, parkTile, emptyLotTile }
            },
            // Industrial → Road, Industrial, EmptyLot
            new AdjacencyEntry
            {
                tile = industrialTile,
                allowedNeighbours = new CityTileData[] { roadTile, industrialTile, emptyLotTile }
            },
            // Park → Residential, Park, EmptyLot
            new AdjacencyEntry
            {
                tile = parkTile,
                allowedNeighbours = new CityTileData[] { residentialTile, parkTile, emptyLotTile }
            },
            // EmptyLot → all types
            new AdjacencyEntry
            {
                tile = emptyLotTile,
                allowedNeighbours = new CityTileData[] { roadTile, residentialTile, industrialTile, parkTile, emptyLotTile }
            }
        };

        AssetDatabase.CreateAsset(ruleSet, "Assets/CityData/DefaultAdjacencyRules.asset");

        // ── Finalise ──
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[CityPlaceholderCreator] Created 5 CityTileData assets and 1 AdjacencyRule asset in Assets/CityData/.");
        EditorUtility.DisplayDialog("Done",
            "City tile assets created!\n\n" +
            "Tile Data: Assets/CityData/\n" +
            "Adjacency Rules: Assets/CityData/DefaultAdjacencyRules.asset\n\n" +
            "Next: Create an empty GameObject, add WFCCityGenerator, assign the rule set, and click Generate City.",
            "OK");
    }

    // ═══════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════

    /// <summary>
    /// Creates a CityTileData ScriptableObject asset with the given prefab paths.
    /// </summary>
    private static CityTileData CreateTileData(string tileName, string[] prefabPaths)
    {
        CityTileData data = ScriptableObject.CreateInstance<CityTileData>();
        data.tileName = tileName;

        // Load prefabs from asset paths.
        var prefabList = new System.Collections.Generic.List<GameObject>();
        for (int i = 0; i < prefabPaths.Length; i++)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPaths[i]);
            if (prefab != null)
            {
                prefabList.Add(prefab);
            }
            else
            {
                Debug.LogWarning($"[CityPlaceholderCreator] Prefab not found: {prefabPaths[i]}");
            }
        }
        data.prefabs = prefabList.ToArray();

        string assetPath = $"Assets/CityData/{tileName}.asset";
        AssetDatabase.CreateAsset(data, assetPath);

        return data;
    }

    /// <summary>
    /// Creates a folder if it doesn't exist.
    /// </summary>
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
