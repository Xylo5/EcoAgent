using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public static class CombinedPrefabCreator
{
    [MenuItem("Tools/Prefabs/Create Combined Prefab From Selection")]
    private static void CreateCombinedPrefabFromSelectionTools()
    {
        CreateCombinedPrefabFromSelection();
    }

    [MenuItem("GameObject/Prefabs/Create Combined Prefab From Selection", false, 49)]
    private static void CreateCombinedPrefabFromSelectionGameObject()
    {
        CreateCombinedPrefabFromSelection();
    }

    [MenuItem("Tools/Prefabs/Create Combined Prefab From Scene View (Selection Root)")]
    private static void CreateCombinedPrefabFromSceneViewSelectionRootTools()
    {
        CreateCombinedPrefabFromSceneViewSelectionRoot();
    }

    [MenuItem("GameObject/Prefabs/Create Combined Prefab From Scene View (Selection Root)", false, 50)]
    private static void CreateCombinedPrefabFromSceneViewSelectionRootGameObject()
    {
        CreateCombinedPrefabFromSceneViewSelectionRoot();
    }

    [MenuItem("Tools/Prefabs/Merge Selected Prefab Assets Into New Prefab")]
    private static void MergeSelectedPrefabAssetsTools()
    {
        MergeSelectedPrefabAssetsIntoNewPrefab();
    }

    [MenuItem("Assets/Prefabs/Merge Selected Prefab Assets Into New Prefab", false, 2200)]
    private static void MergeSelectedPrefabAssetsAssetsMenu()
    {
        MergeSelectedPrefabAssetsIntoNewPrefab();
    }

    [MenuItem("Tools/Prefabs/Create Combined Prefab From Selection", true)]
    [MenuItem("GameObject/Prefabs/Create Combined Prefab From Selection", true)]
    private static bool ValidateCreateCombinedPrefabFromSelection()
    {
        return Selection.activeGameObject != null;
    }

    [MenuItem("Tools/Prefabs/Create Combined Prefab From Scene View (Selection Root)", true)]
    [MenuItem("GameObject/Prefabs/Create Combined Prefab From Scene View (Selection Root)", true)]
    private static bool ValidateCreateCombinedPrefabFromSceneViewSelectionRoot()
    {
        return Selection.activeGameObject != null && SceneView.lastActiveSceneView != null;
    }

    [MenuItem("Tools/Prefabs/Merge Selected Prefab Assets Into New Prefab", true)]
    [MenuItem("Assets/Prefabs/Merge Selected Prefab Assets Into New Prefab", true)]
    private static bool ValidateMergeSelectedPrefabAssets()
    {
        int count = Selection.objects
            .OfType<GameObject>()
            .Count(go => PrefabUtility.GetPrefabAssetType(go) != PrefabAssetType.NotAPrefab);

        return count >= 2;
    }

    private static void CreateCombinedPrefabFromSelection()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("Create Combined Prefab", "Select a root GameObject in the Hierarchy first.", "OK");
            return;
        }

        EnsureFolderExists("Assets/Prefabs");

        string defaultName = selected.name + "_Combined.prefab";
        string savePath = EditorUtility.SaveFilePanelInProject(
            "Save Combined Prefab",
            defaultName,
            "prefab",
            "Choose where to save the new combined prefab.",
            "Assets/Prefabs");

        if (string.IsNullOrEmpty(savePath))
            return;

        savePath = AssetDatabase.GenerateUniqueAssetPath(savePath);

        GameObject workingCopy = Object.Instantiate(selected);
        workingCopy.name = selected.name;

        try
        {
            // Unpack all nested prefab instances so the saved asset is fully self-contained.
            UnpackAllPrefabInstances(workingCopy);

            bool success;
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(workingCopy, savePath, out success);
            if (!success || savedPrefab == null)
            {
                EditorUtility.DisplayDialog("Create Combined Prefab", "Failed to save prefab.", "OK");
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(savedPrefab);

            Debug.Log($"Created combined prefab at: {savePath}");
            EditorUtility.DisplayDialog("Create Combined Prefab", $"Created:\n{savePath}", "OK");
        }
        finally
        {
            Object.DestroyImmediate(workingCopy);
        }
    }

    private static void MergeSelectedPrefabAssetsIntoNewPrefab()
    {
        List<GameObject> selectedPrefabs = Selection.objects
            .OfType<GameObject>()
            .Where(go => PrefabUtility.GetPrefabAssetType(go) != PrefabAssetType.NotAPrefab)
            .ToList();

        if (selectedPrefabs.Count < 2)
        {
            EditorUtility.DisplayDialog(
                "Merge Prefabs",
                "Select at least two prefab assets in the Project window first.",
                "OK");
            return;
        }

        EnsureFolderExists("Assets/Prefabs");

        string defaultName = string.Join("_", selectedPrefabs.Select(p => p.name)) + "_Merged.prefab";
        string savePath = EditorUtility.SaveFilePanelInProject(
            "Save Merged Prefab",
            defaultName,
            "prefab",
            "Choose where to save the merged prefab.",
            "Assets/Prefabs");

        if (string.IsNullOrEmpty(savePath))
            return;

        savePath = AssetDatabase.GenerateUniqueAssetPath(savePath);

        GameObject root = new GameObject("MergedPrefabRoot");

        try
        {
            foreach (GameObject sourcePrefab in selectedPrefabs)
            {
                GameObject child = Object.Instantiate(sourcePrefab);
                child.name = sourcePrefab.name;
                child.transform.SetParent(root.transform, false);
            }

            UnpackAllPrefabInstances(root);

            bool success;
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, savePath, out success);
            if (!success || savedPrefab == null)
            {
                EditorUtility.DisplayDialog("Merge Prefabs", "Failed to save merged prefab.", "OK");
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(savedPrefab);

            string msg = $"Created merged prefab:\n{savePath}\n\nMerged count: {selectedPrefabs.Count}";
            Debug.Log(msg);
            EditorUtility.DisplayDialog("Merge Prefabs", msg, "OK");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void CreateCombinedPrefabFromSceneViewSelectionRoot()
    {
        GameObject selectedRoot = Selection.activeGameObject;
        if (selectedRoot == null)
        {
            EditorUtility.DisplayDialog("Create Visible Portion Prefab", "Select a root GameObject in the Hierarchy first.", "OK");
            return;
        }

        SceneView sceneView = SceneView.lastActiveSceneView;
        Camera sceneCamera = sceneView != null ? sceneView.camera : null;
        if (sceneCamera == null)
        {
            EditorUtility.DisplayDialog("Create Visible Portion Prefab", "No active Scene view camera found.", "OK");
            return;
        }

        EnsureFolderExists("Assets/Prefabs");

        string defaultName = selectedRoot.name + "_ScreenPortion.prefab";
        string savePath = EditorUtility.SaveFilePanelInProject(
            "Save Visible Portion Prefab",
            defaultName,
            "prefab",
            "Saves only the part of selected root currently visible in Scene view.",
            "Assets/Prefabs");

        if (string.IsNullOrEmpty(savePath))
            return;

        savePath = AssetDatabase.GenerateUniqueAssetPath(savePath);

        GameObject workingCopy = Object.Instantiate(selectedRoot);
        workingCopy.name = selectedRoot.name;

        try
        {
            int keptBranches;
            int visibleRenderers;
            bool hadVisibleContent = KeepOnlyVisibleBranches(workingCopy, sceneCamera, out keptBranches, out visibleRenderers);
            if (!hadVisibleContent)
            {
                EditorUtility.DisplayDialog(
                    "Create Visible Portion Prefab",
                    "No visible renderers found under the selected root in current Scene view.",
                    "OK");
                return;
            }

            UnpackAllPrefabInstances(workingCopy);

            bool success;
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(workingCopy, savePath, out success);
            if (!success || savedPrefab == null)
            {
                EditorUtility.DisplayDialog("Create Visible Portion Prefab", "Failed to save prefab.", "OK");
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(savedPrefab);

            string msg = $"Created:\n{savePath}\n\nVisible renderers: {visibleRenderers}\nKept root branches: {keptBranches}";
            Debug.Log(msg);
            EditorUtility.DisplayDialog("Create Visible Portion Prefab", msg, "OK");
        }
        finally
        {
            Object.DestroyImmediate(workingCopy);
        }
    }

    private static bool KeepOnlyVisibleBranches(GameObject root, Camera camera, out int keptBranches, out int visibleRendererCount)
    {
        keptBranches = 0;
        visibleRendererCount = 0;

        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        var branchRootsToKeep = new HashSet<Transform>();
        bool rootVisible = false;

        foreach (Renderer r in renderers)
        {
            if (r == null || !r.enabled)
                continue;

            if (!GeometryUtility.TestPlanesAABB(planes, r.bounds))
                continue;

            visibleRendererCount++;

            Transform t = r.transform;
            if (t == root.transform)
            {
                rootVisible = true;
                continue;
            }

            while (t.parent != null && t.parent != root.transform)
            {
                t = t.parent;
            }

            if (t.parent == root.transform)
                branchRootsToKeep.Add(t);
        }

        if (visibleRendererCount == 0)
            return false;

        if (rootVisible)
        {
            keptBranches = root.transform.childCount;
            return true;
        }

        List<Transform> toDelete = new List<Transform>();
        foreach (Transform child in root.transform)
        {
            if (!branchRootsToKeep.Contains(child))
                toDelete.Add(child);
        }

        foreach (Transform child in toDelete)
            Object.DestroyImmediate(child.gameObject);

        keptBranches = branchRootsToKeep.Count;
        return keptBranches > 0;
    }

    private static void UnpackAllPrefabInstances(GameObject root)
    {
        bool unpackedAny;
        do
        {
            unpackedAny = false;
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in transforms)
            {
                GameObject go = t.gameObject;
                if (!PrefabUtility.IsAnyPrefabInstanceRoot(go))
                    continue;

                PrefabUtility.UnpackPrefabInstance(
                    go,
                    PrefabUnpackMode.Completely,
                    InteractionMode.AutomatedAction);

                unpackedAny = true;
            }
        }
        while (unpackedAny);
    }

    private static void EnsureFolderExists(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        string[] parts = folderPath.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }
}
