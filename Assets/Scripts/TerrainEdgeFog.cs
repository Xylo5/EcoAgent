using UnityEngine;

/// <summary>
/// Generates 4 fog quads around the terrain edges at runtime.
/// Each quad fades from opaque at the outer edge to transparent at the terrain edge.
/// Attach to any GameObject in the scene and assign the terrain reference.
/// </summary>
public class TerrainEdgeFog : MonoBehaviour
{
    [Header("References")]
    [Tooltip("If null, will auto-find the active terrain.")]
    public Terrain terrain;

    [Header("Fog Settings")]
    [Tooltip("Color of the fog.")]
    public Color fogColor = new Color(0.75f, 0.85f, 0.75f, 1f);

    [Range(10f, 200f)]
    [Tooltip("How far the fog extends outward from the terrain edge.")]
    public float fogWidth = 60f;

    [Range(0f, 1f)]
    [Tooltip("Maximum fog opacity at the outer edge.")]
    public float fogDensity = 0.85f;

    [Tooltip("Y offset above terrain surface for the fog plane.")]
    public float yOffset = 1f;

    [Tooltip("Extra height for the fog plane (makes it visible from angles).")]
    public float fogHeight = 15f;

    private GameObject fogParent;
    private Material fogMaterial;

    void Start()
    {
        if (terrain == null)
            terrain = Terrain.activeTerrain;

        if (terrain == null)
        {
            Debug.LogWarning("[TerrainEdgeFog] No terrain found!");
            return;
        }

        CreateFog();
    }

    void CreateFog()
    {
        // Clean up previous
        if (fogParent != null)
            Destroy(fogParent);

        fogParent = new GameObject("TerrainEdgeFog");
        fogParent.transform.SetParent(transform);

        // Create material
        Shader fogShader = Shader.Find("Custom/EdgeFog");
        if (fogShader == null)
        {
            // Fallback to Sprites/Default if custom shader not found
            Debug.LogWarning("[TerrainEdgeFog] Custom/EdgeFog shader not found, using fallback.");
            fogShader = Shader.Find("Sprites/Default");
        }

        fogMaterial = new Material(fogShader);
        fogMaterial.SetColor("_FogColor", fogColor);
        fogMaterial.SetFloat("_FogDensity", fogDensity);
        fogMaterial.hideFlags = HideFlags.HideAndDontSave;

        Vector3 tPos = terrain.transform.position;
        Vector3 tSize = terrain.terrainData.size;

        float terrainMinX = tPos.x;
        float terrainMaxX = tPos.x + tSize.x;
        float terrainMinZ = tPos.z;
        float terrainMaxZ = tPos.z + tSize.z;
        float baseY = tPos.y + yOffset;

        // Create 4 fog quads: North, South, East, West
        // North (positive Z edge)
        CreateFogQuad("Fog_North",
            new Vector3(terrainMinX - fogWidth, baseY, terrainMaxZ),
            new Vector3(terrainMaxX + fogWidth, baseY, terrainMaxZ),
            new Vector3(terrainMaxX + fogWidth, baseY + fogHeight, terrainMaxZ + fogWidth),
            new Vector3(terrainMinX - fogWidth, baseY + fogHeight, terrainMaxZ + fogWidth));

        // South (negative Z edge)
        CreateFogQuad("Fog_South",
            new Vector3(terrainMaxX + fogWidth, baseY, terrainMinZ),
            new Vector3(terrainMinX - fogWidth, baseY, terrainMinZ),
            new Vector3(terrainMinX - fogWidth, baseY + fogHeight, terrainMinZ - fogWidth),
            new Vector3(terrainMaxX + fogWidth, baseY + fogHeight, terrainMinZ - fogWidth));

        // East (positive X edge)
        CreateFogQuad("Fog_East",
            new Vector3(terrainMaxX, baseY, terrainMaxZ + fogWidth),
            new Vector3(terrainMaxX, baseY, terrainMinZ - fogWidth),
            new Vector3(terrainMaxX + fogWidth, baseY + fogHeight, terrainMinZ - fogWidth),
            new Vector3(terrainMaxX + fogWidth, baseY + fogHeight, terrainMaxZ + fogWidth));

        // West (negative X edge)
        CreateFogQuad("Fog_West",
            new Vector3(terrainMinX, baseY, terrainMinZ - fogWidth),
            new Vector3(terrainMinX, baseY, terrainMaxZ + fogWidth),
            new Vector3(terrainMinX - fogWidth, baseY + fogHeight, terrainMaxZ + fogWidth),
            new Vector3(terrainMinX - fogWidth, baseY + fogHeight, terrainMinZ - fogWidth));

        Debug.Log("[TerrainEdgeFog] Created 4 fog planes around terrain edges.");
    }

    void CreateFogQuad(string name, Vector3 bl, Vector3 br, Vector3 tr, Vector3 tl)
    {
        GameObject quad = new GameObject(name);
        quad.transform.SetParent(fogParent.transform, false);
        quad.layer = LayerMask.NameToLayer("Ignore Raycast");

        MeshFilter mf = quad.AddComponent<MeshFilter>();
        MeshRenderer mr = quad.AddComponent<MeshRenderer>();
        mr.material = fogMaterial;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        Mesh mesh = new Mesh();
        mesh.name = name;

        mesh.vertices = new Vector3[] { bl, br, tr, tl };
        mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
        mesh.uv = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        };

        // Vertex colors: bottom edge (near terrain) = transparent, top/outer = opaque
        Color transparent = new Color(fogColor.r, fogColor.g, fogColor.b, 0f);
        Color opaque = new Color(fogColor.r, fogColor.g, fogColor.b, 1f);
        mesh.colors = new Color[] { transparent, transparent, opaque, opaque };

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        mf.mesh = mesh;
    }
}
