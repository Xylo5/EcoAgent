using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor utility to auto-assign textures to SimplePoly City materials.
/// Each material has a matching .png in the Textures folder (same base name).
/// Run via menu: Tools > SimplePoly > Assign All Textures
/// </summary>
public class AssignSimplePolyTextures : EditorWindow
{
    private const string MATERIALS_PATH = "Assets/SimplePoly City - Low Poly Assets/Materials";
    private const string TEXTURES_PATH = "Assets/SimplePoly City - Low Poly Assets/Textures";

    [MenuItem("Tools/SimplePoly/Assign All Textures")]
    public static void AssignTextures()
    {
        string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { MATERIALS_PATH });
        int assignedCount = 0;
        int skippedCount = 0;

        foreach (string guid in matGuids)
        {
            string matPath = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

            if (mat == null) continue;

            // Get the base name without extension
            string matName = Path.GetFileNameWithoutExtension(matPath);

            // Try to find matching texture
            string texPath = TEXTURES_PATH + "/" + matName + ".png";
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);

            if (tex == null)
            {
                // Some textures have slightly different names (e.g. Vehicle_Bus_color01 -> Vehicle_Bus_1)
                // Try common patterns
                string altName = matName.Replace("_color01", "_1")
                                        .Replace("_color02", "_2")
                                        .Replace("_color03", "_3");
                texPath = TEXTURES_PATH + "/" + altName + ".png";
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            }

            if (tex == null)
            {
                Debug.LogWarning($"[SimplePoly] No texture found for material: {matName}");
                skippedCount++;
                continue;
            }

            // Check if already assigned
            Texture currentTex = mat.HasProperty("_BaseMap") ? mat.GetTexture("_BaseMap") : null;
            if (currentTex != null)
            {
                skippedCount++;
                continue;
            }

            // Assign texture to _BaseMap (URP) and _MainTex (fallback)
            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex"))
                mat.SetTexture("_MainTex", tex);

            EditorUtility.SetDirty(mat);
            assignedCount++;
            Debug.Log($"[SimplePoly] Assigned {tex.name} to {matName}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[SimplePoly] Done! Assigned: {assignedCount}, Skipped: {skippedCount}");
        EditorUtility.DisplayDialog("SimplePoly Texture Assignment",
            $"Assigned textures to {assignedCount} materials.\nSkipped: {skippedCount}",
            "OK");
    }
}
