using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class PinkMaterialFixer
{
    private const string LiteFarmPackRoot = "Assets/Gridness Studios/Lite Farm Pack";

    [MenuItem("Tools/Rendering/Fix Pink Materials In Selection")]
    public static void FixPinkMaterialsInSelection()
    {
        List<Material> materials = CollectMaterialsFromSelection();
        RunFix(materials, "Selection");
    }

    [MenuItem("Tools/Rendering/Fix Pink Materials In Project")]
    public static void FixPinkMaterialsInProject()
    {
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
        var materials = new List<Material>(guids.Length);

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null)
                materials.Add(mat);
        }

        RunFix(materials, "Project");
    }

    [MenuItem("Tools/Rendering/Fix Lite Farm Pack (Gridness Studios)")]
    public static void FixLiteFarmPack()
    {
        if (!AssetDatabase.IsValidFolder(LiteFarmPackRoot))
        {
            EditorUtility.DisplayDialog(
                "Fix Lite Farm Pack",
                "Lite Farm Pack folder was not found at:\n" + LiteFarmPackRoot,
                "OK");
            return;
        }

        RemapModelMaterialsInFolder(LiteFarmPackRoot);

        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { LiteFarmPackRoot });
        var materials = new List<Material>(guids.Length);
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null)
                materials.Add(mat);
        }

        RunFix(materials, "Lite Farm Pack");
    }

    private static void RunFix(List<Material> materials, string scope)
    {
        if (materials == null || materials.Count == 0)
        {
            EditorUtility.DisplayDialog("Fix Pink Materials", $"No materials found in {scope}.", "OK");
            return;
        }

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            EditorUtility.DisplayDialog(
                "Fix Pink Materials",
                "URP/Lit shader was not found. Ensure Universal RP is installed and active.",
                "OK");
            return;
        }

        int fixedCount = 0;
        int skippedCount = 0;

        foreach (Material mat in materials)
        {
            if (mat == null)
                continue;

            if (!NeedsRepair(mat))
            {
                skippedCount++;
                continue;
            }

            ConvertToUrpLit(mat, urpLit);
            EditorUtility.SetDirty(mat);
            fixedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[PinkMaterialFixer] Scope={scope} Fixed={fixedCount} Skipped={skippedCount}");
        EditorUtility.DisplayDialog(
            "Fix Pink Materials",
            $"Scope: {scope}\nFixed: {fixedCount}\nSkipped: {skippedCount}",
            "OK");
    }

    private static void RemapModelMaterialsInFolder(string folder)
    {
        string[] modelGuids = AssetDatabase.FindAssets("t:Model", new[] { folder });

        foreach (string guid in modelGuids)
        {
            string modelPath = AssetDatabase.GUIDToAssetPath(guid);
            ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (importer == null)
                continue;

            bool changed = false;

            if (importer.materialImportMode == ModelImporterMaterialImportMode.None)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                changed = true;
            }

            if (importer.materialLocation != ModelImporterMaterialLocation.External)
            {
                importer.materialLocation = ModelImporterMaterialLocation.External;
                changed = true;
            }

            importer.SearchAndRemapMaterials(
                ModelImporterMaterialName.BasedOnMaterialName,
                ModelImporterMaterialSearch.Everywhere);

            if (changed)
                importer.SaveAndReimport();
            else
                AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceUpdate);
        }
    }

    private static bool NeedsRepair(Material mat)
    {
        Shader shader = mat.shader;

        if (shader == null)
            return true;

        string shaderName = shader.name;

        if (shaderName == "Hidden/InternalErrorShader")
            return true;

        if (shaderName == "Standard")
            return true;

        if (shaderName.StartsWith("Legacy Shaders/"))
            return true;

        if (shaderName.Contains("Autodesk Interactive"))
            return true;

        return false;
    }

    private static void ConvertToUrpLit(Material mat, Shader urpLit)
    {
        Texture baseMap = GetFirstTexture(mat, "_BaseMap", "_MainTex");
        Color baseColor = GetFirstColor(mat, "_BaseColor", "_Color", Color.white);

        Texture normalMap = GetFirstTexture(mat, "_BumpMap");
        Texture metallicMap = GetFirstTexture(mat, "_MetallicGlossMap");
        Texture occlusionMap = GetFirstTexture(mat, "_OcclusionMap");
        Texture emissionMap = GetFirstTexture(mat, "_EmissionMap");

        float metallic = GetFirstFloat(mat, "_Metallic", 0f);
        float smoothness = GetFirstFloat(mat, "_Smoothness", "_Glossiness", 0.5f);

        Color emissionColor = GetFirstColor(mat, "_EmissionColor", Color.black);

        mat.shader = urpLit;

        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", baseMap);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);

        if (mat.HasProperty("_BumpMap")) mat.SetTexture("_BumpMap", normalMap);
        if (mat.HasProperty("_MetallicGlossMap")) mat.SetTexture("_MetallicGlossMap", metallicMap);
        if (mat.HasProperty("_OcclusionMap")) mat.SetTexture("_OcclusionMap", occlusionMap);
        if (mat.HasProperty("_EmissionMap")) mat.SetTexture("_EmissionMap", emissionMap);

        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emissionColor);

        if (normalMap != null)
            mat.EnableKeyword("_NORMALMAP");

        if (emissionMap != null || emissionColor.maxColorComponent > 0f)
            mat.EnableKeyword("_EMISSION");
    }

    private static List<Material> CollectMaterialsFromSelection()
    {
        var unique = new HashSet<Material>();

        foreach (Object obj in Selection.objects)
        {
            if (obj == null)
                continue;

            if (obj is Material directMat)
            {
                unique.Add(directMat);
                continue;
            }

            string path = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(path) && path.EndsWith(".prefab"))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                    CollectFromGameObject(prefab, unique);

                continue;
            }

            if (obj is GameObject go)
            {
                CollectFromGameObject(go, unique);
            }
        }

        return new List<Material>(unique);
    }

    private static void CollectFromGameObject(GameObject go, HashSet<Material> target)
    {
        if (go == null)
            return;

        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            Material[] mats = renderer.sharedMaterials;
            foreach (Material mat in mats)
            {
                if (mat != null)
                    target.Add(mat);
            }
        }
    }

    private static Texture GetFirstTexture(Material mat, params string[] names)
    {
        foreach (string n in names)
        {
            if (mat.HasProperty(n))
                return mat.GetTexture(n);
        }

        return null;
    }

    private static Color GetFirstColor(Material mat, string primary, Color fallback)
    {
        if (mat.HasProperty(primary))
            return mat.GetColor(primary);

        return fallback;
    }

    private static Color GetFirstColor(Material mat, string primary, string secondary, Color fallback)
    {
        if (mat.HasProperty(primary))
            return mat.GetColor(primary);

        if (mat.HasProperty(secondary))
            return mat.GetColor(secondary);

        return fallback;
    }

    private static float GetFirstFloat(Material mat, string primary, float fallback)
    {
        if (mat.HasProperty(primary))
            return mat.GetFloat(primary);

        return fallback;
    }

    private static float GetFirstFloat(Material mat, string primary, string secondary, float fallback)
    {
        if (mat.HasProperty(primary))
            return mat.GetFloat(primary);

        if (mat.HasProperty(secondary))
            return mat.GetFloat(secondary);

        return fallback;
    }
}
