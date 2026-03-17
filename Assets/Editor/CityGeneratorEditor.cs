using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CityGenerator))]
public class CityGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CityGenerator generator = (CityGenerator)target;

        if (generator.gridManager != null)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                $"Grid: {generator.gridManager.gridWidth}\u00d7{generator.gridManager.gridHeight}  |  " +
                $"Cell Size: {generator.gridManager.cellSize}  |  " +
                $"Buildings assigned: {(generator.buildings != null ? generator.buildings.Length : 0)}",
                MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("Assign a GridManager to define grid dimensions.", MessageType.Warning);
        }

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("Generation", EditorStyles.boldLabel);

        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
        if (GUILayout.Button("Generate Buildings", GUILayout.Height(36)))
        {
            Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "Generate Buildings");
            generator.Generate();
        }

        GUI.backgroundColor = new Color(0.9f, 0.35f, 0.3f);
        if (GUILayout.Button("Clear Buildings", GUILayout.Height(28)))
        {
            Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "Clear Buildings");
            generator.ClearMap();
        }

        GUI.backgroundColor = Color.white;
    }
}
