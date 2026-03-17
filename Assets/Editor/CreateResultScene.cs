using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// One-time editor utility to create the Result scene with the correct setup.
/// Use via menu: Tools > Create Result Scene.
/// </summary>
public static class CreateResultScene
{
    [MenuItem("Tools/Create Result Scene")]
    public static void Create()
    {
        // Save current scene first
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        // Create a new empty scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera
        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        Camera cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.08f, 0.12f, 1f);
        camObj.transform.position = new Vector3(0, 0, -10);

        // ResultManager with ResultUI script
        GameObject resultObj = new GameObject("ResultManager");
        resultObj.AddComponent<ResultUI>();

        // Save the scene
        string path = "Assets/Scenes/Result.unity";
        EditorSceneManager.SaveScene(scene, path);

        // Add to Build Settings if not already there
        var buildScenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool found = false;
        foreach (var s in buildScenes)
        {
            if (s.path == path) { found = true; break; }
        }
        if (!found)
        {
            buildScenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();
            Debug.Log("[CreateResultScene] Added 'Result' to Build Settings.");
        }

        Debug.Log("[CreateResultScene] Result scene created and saved at " + path);
    }
}
