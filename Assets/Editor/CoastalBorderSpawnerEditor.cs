using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom Inspector for CoastalBorderSpawner.
/// Adds Spawn/Clear buttons for scene-view generation.
/// </summary>
[CustomEditor(typeof(CoastalBorderSpawner))]
public class CoastalBorderSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CoastalBorderSpawner spawner = (CoastalBorderSpawner)target;

        EditorGUILayout.Space(10);

        // Info box
        if (spawner.gridManager != null)
        {
            EditorGUILayout.HelpBox(
                $"Grid: {spawner.gridManager.gridWidth}x{spawner.gridManager.gridHeight}" +
                $" | Cell: {spawner.gridManager.cellSize}" +
                $"\nBlend: {spawner.beachToForestBlend:P0} up the sides",
                MessageType.Info);
        }

        EditorGUILayout.Space(5);

        // Spawn button (green)
        GUI.backgroundColor = new Color(0.3f, 0.85f, 0.5f);
        if (GUILayout.Button("Spawn Coast & Beach", GUILayout.Height(32)))
        {
            Undo.RegisterFullObjectHierarchyUndo(spawner.gameObject, "Spawn Coast");
            spawner.SpawnCoast();
        }

        // Clear button (red)
        GUI.backgroundColor = new Color(0.9f, 0.35f, 0.3f);
        if (GUILayout.Button("Clear Coast", GUILayout.Height(28)))
        {
            Undo.RegisterFullObjectHierarchyUndo(spawner.gameObject, "Clear Coast");
            spawner.ClearCoast();
        }

        GUI.backgroundColor = Color.white;
    }
}
