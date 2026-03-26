using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// One-click utility to register all game scenes in Build Settings.
/// Use via menu: Tools > Register All Scenes In Build Settings
/// </summary>
public static class RegisterAllScenes
{
    [MenuItem("Tools/Register All Scenes In Build Settings")]
    public static void Register()
    {
        string folder = "Assets/Scenes";
        var scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(folder + "/MainMenu.unity",    true),
            new EditorBuildSettingsScene(folder + "/LevelSelect.unity", true),
            new EditorBuildSettingsScene(folder + "/Level_1.unity",     true),
            new EditorBuildSettingsScene(folder + "/Level_2.unity",     true),
            new EditorBuildSettingsScene(folder + "/Level_3.unity",     true),
            new EditorBuildSettingsScene(folder + "/Level_4.unity",     true),
            new EditorBuildSettingsScene(folder + "/Level_5.unity",     true),
            new EditorBuildSettingsScene(folder + "/Result.unity",      true),
        };
        EditorBuildSettings.scenes = scenes.ToArray();
        UnityEngine.Debug.Log("[RegisterAllScenes] ✓ 8 scenes registered in Build Settings.");
    }
}
