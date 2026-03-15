using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom Inspector for WFCCityGenerator.
/// Adds "Generate City" and "Clear City" buttons.
/// </summary>
[CustomEditor(typeof(WFCCityGenerator))]
public class WFCCityGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        WFCCityGenerator generator = (WFCCityGenerator)target;

        // Show grid info from GridManager if assigned.
        if (generator.gridManager != null)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                $"Grid: {generator.gridManager.gridWidth}×{generator.gridManager.gridHeight}  |  " +
                $"Cell Size: {generator.gridManager.cellSize}",
                MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("Assign a GridManager to define grid dimensions.", MessageType.Warning);
        }

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("City Generation", EditorStyles.boldLabel);

        // ── Generate Button ──
        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
        if (GUILayout.Button("Generate City", GUILayout.Height(36)))
        {
            Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "Generate City");
            generator.GenerateCity();
        }

        // ── Clear Button ──
        GUI.backgroundColor = new Color(0.9f, 0.35f, 0.3f);
        if (GUILayout.Button("Clear City", GUILayout.Height(28)))
        {
            Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "Clear City");
            generator.ClearCity();
        }

        GUI.backgroundColor = Color.white;
    }
}
