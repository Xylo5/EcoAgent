using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RoadGenerator))]
public class RoadGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        RoadGenerator generator = (RoadGenerator)target;

        if (generator.gridManager != null)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                $"Grid: {generator.gridManager.gridWidth}\u00d7{generator.gridManager.gridHeight}  |  " +
                $"Cell Size: {generator.gridManager.cellSize}",
                MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("Assign a GridManager to define grid dimensions.", MessageType.Warning);
        }

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("Road Generation", EditorStyles.boldLabel);

        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
        if (GUILayout.Button("Generate Roads", GUILayout.Height(36)))
        {
            Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "Generate Roads");
            generator.Generate();
        }

        GUI.backgroundColor = new Color(0.9f, 0.35f, 0.3f);
        if (GUILayout.Button("Clear Roads", GUILayout.Height(28)))
        {
            Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "Clear Roads");
            generator.ClearMap();
        }

        EditorGUILayout.Space(8);
        GUI.backgroundColor = new Color(0.4f, 0.7f, 0.9f);
        if (GUILayout.Button("Log Prefab Scales", GUILayout.Height(24)))
        {
            generator.LogPrefabScales();
        }

        GUI.backgroundColor = Color.white;
    }
}
