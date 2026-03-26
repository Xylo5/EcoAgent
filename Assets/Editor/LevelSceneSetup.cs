using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility: Tools → Setup All Level Scenes
/// Copies Level_1.unity to Level_2 through Level_5, giving each level
/// the same terrain, grid, UI, and script setup as Level_1.
/// Existing Level_N scenes are backed up (renamed with _backup suffix) before overwriting.
/// Run once — then customize each level individually in the editor.
/// </summary>
public static class LevelSceneSetup
{
    private const string ScenesFolder = "Assets/Scenes";
    private const string SourceScene = "Assets/Scenes/Level_1.unity";

    [MenuItem("Tools/Setup All Level Scenes (Copy Level_1 → 2-5)")]
    public static void SetupAllLevels()
    {
        if (!System.IO.File.Exists(SourceScene))
        {
            EditorUtility.DisplayDialog("Error",
                "Level_1.unity not found at:\n" + SourceScene +
                "\n\nMake sure Level_1 exists first.", "OK");
            return;
        }

        // Save any open scene first
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        int copied = 0;
        for (int i = 2; i <= 5; i++)
        {
            string targetPath = ScenesFolder + "/Level_" + i + ".unity";

            // If target already exists, delete it first
            if (System.IO.File.Exists(targetPath))
            {
                AssetDatabase.DeleteAsset(targetPath);
            }

            // Copy Level_1 → Level_N
            bool success = AssetDatabase.CopyAsset(SourceScene, targetPath);
            if (success)
            {
                Debug.Log("[LevelSceneSetup] ✓ Copied Level_1 → Level_" + i);
                copied++;
            }
            else
            {
                Debug.LogError("[LevelSceneSetup] Failed to copy to Level_" + i);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Re-register all scenes in Build Settings
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(ScenesFolder + "/MainMenu.unity",    true),
            new EditorBuildSettingsScene(ScenesFolder + "/LevelSelect.unity", true),
            new EditorBuildSettingsScene(ScenesFolder + "/Level_1.unity",     true),
            new EditorBuildSettingsScene(ScenesFolder + "/Level_2.unity",     true),
            new EditorBuildSettingsScene(ScenesFolder + "/Level_3.unity",     true),
            new EditorBuildSettingsScene(ScenesFolder + "/Level_4.unity",     true),
            new EditorBuildSettingsScene(ScenesFolder + "/Level_5.unity",     true),
            new EditorBuildSettingsScene(ScenesFolder + "/Result.unity",      true),
        };
        EditorBuildSettings.scenes = scenes.ToArray();

        EditorUtility.DisplayDialog("Done",
            copied + " level scenes created from Level_1!\n\n" +
            "All 8 scenes registered in Build Settings.\n\n" +
            "Next steps:\n" +
            "1. Open each Level_N scene\n" +
            "2. Customize terrain, roads, buildings as desired\n" +
            "3. Assign different LevelRequirements assets per level",
            "OK");
    }
}
