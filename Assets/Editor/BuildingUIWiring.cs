using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Editor utility to auto-populate the BuildingUI.buildings array
/// with all BuildingData assets found in the project.
/// Use via menu: Tools > Wire All Buildings to BuildingUI
/// </summary>
public class BuildingUIWiring
{
    [MenuItem("Tools/Wire All Buildings to BuildingUI")]
    public static void WireAllBuildings()
    {
        // Find all BuildingData assets in the project
        string[] guids = AssetDatabase.FindAssets("t:BuildingData");
        if (guids.Length == 0)
        {
            Debug.LogWarning("No BuildingData assets found in the project.");
            return;
        }

        List<BuildingData> allBuildings = new List<BuildingData>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            BuildingData bd = AssetDatabase.LoadAssetAtPath<BuildingData>(path);
            if (bd != null)
                allBuildings.Add(bd);
        }

        // Sort by sizeInCells for a nice ordering in the UI
        allBuildings = allBuildings.OrderBy(b => b.sizeInCells).ThenBy(b => b.buildingName).ToList();

        // Find the BuildingUI component in the current scene
        BuildingUI buildingUI = Object.FindFirstObjectByType<BuildingUI>();
        if (buildingUI == null)
        {
            // Fallback for older Unity versions
            buildingUI = Object.FindObjectOfType<BuildingUI>();
        }

        if (buildingUI == null)
        {
            Debug.LogError("No BuildingUI component found in the current scene. Open Level_0 scene first.");
            return;
        }

        Undo.RecordObject(buildingUI, "Wire All Buildings to BuildingUI");
        buildingUI.buildings = allBuildings.ToArray();
        EditorUtility.SetDirty(buildingUI);

        Debug.Log($"BuildingUI.buildings wired with {allBuildings.Count} buildings: " +
                  string.Join(", ", allBuildings.Select(b => b.buildingName)));
    }
}
