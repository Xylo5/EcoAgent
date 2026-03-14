using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MapGenerator generator = (MapGenerator)target;

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
        EditorGUILayout.LabelField("Map Generation", EditorStyles.boldLabel);

        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
        if (GUILayout.Button("Generate Map", GUILayout.Height(36)))
        {
            Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "Generate Map");
            generator.Generate();
        }

        GUI.backgroundColor = new Color(0.9f, 0.35f, 0.3f);
        if (GUILayout.Button("Clear Map", GUILayout.Height(28)))
        {
            Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "Clear Map");
            generator.ClearMap();
        }

        GUI.backgroundColor = Color.white;
    }
}
