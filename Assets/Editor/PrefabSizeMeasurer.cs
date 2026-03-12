using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility to measure bounds of SimplePoly prefabs.
/// Tools → Measure SimplePoly Prefabs
/// </summary>
public class PrefabSizeMeasurer
{
    [MenuItem("Tools/Measure SimplePoly Prefabs")]
    static void MeasurePrefabs()
    {
        string[] folders = new string[]
        {
            "Assets/SimplePoly City - Low Poly Assets/Prefab/Buildings",
            "Assets/SimplePoly City - Low Poly Assets/Prefab/Natures",
            "Assets/SimplePoly City - Low Poly Assets/Prefab/Roads"
        };

        foreach (string folder in folders)
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
            Debug.Log($"=== {folder} ({guids.Length} prefabs) ===");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                // Instantiate temporarily to measure renderer bounds
                GameObject instance = Object.Instantiate(prefab);
                Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();

                if (renderers.Length > 0)
                {
                    Bounds bounds = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++)
                        bounds.Encapsulate(renderers[i].bounds);

                    Vector3 size = bounds.size;
                    Debug.Log($"  {prefab.name}: Size=({size.x:F2}, {size.y:F2}, {size.z:F2}) | XZ=({size.x:F2}x{size.z:F2}) | Cells@2.5=({Mathf.CeilToInt(size.x/2.5f)}x{Mathf.CeilToInt(size.z/2.5f)})");
                }

                Object.DestroyImmediate(instance);
            }
        }

        Debug.Log("=== Done measuring ===");
    }
}
