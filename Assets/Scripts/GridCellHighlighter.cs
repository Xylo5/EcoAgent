using UnityEngine;

/// <summary>
/// Renders colored quads under the building ghost to show cell validity.
/// Green = cell is free, Red = cell is occupied.
/// Attach this to the GridManager GameObject.
/// </summary>
public class GridCellHighlighter : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;

    [Header("Colors")]
    public Color validCellColor = new Color(0f, 0.8f, 0f, 0.45f);
    public Color invalidCellColor = new Color(0.9f, 0f, 0f, 0.45f);

    // Pool of quad GameObjects
    private GameObject[] quadPool;
    private Renderer[] quadRenderers; // Cached renderers — no per-frame GetComponent
    private int maxQuads = 25; // max 5x5 building
    private int activeQuads = 0;

    // MaterialPropertyBlock avoids creating per-quad material instances
    private MaterialPropertyBlock propBlock;
    private static readonly int ColorProp = Shader.PropertyToID("_Color");

    void Awake()
    {
        propBlock = new MaterialPropertyBlock();
        CreateQuadPool();
    }

    private void CreateQuadPool()
    {
        Material sharedMat = new Material(Shader.Find("Sprites/Default"));
        sharedMat.hideFlags = HideFlags.HideAndDontSave;

        quadPool = new GameObject[maxQuads];
        quadRenderers = new Renderer[maxQuads];

        for (int i = 0; i < maxQuads; i++)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "CellHighlight_" + i;
            quad.transform.SetParent(transform);
            quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            // Remove collider
            Collider col = quad.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // Set shared material & cache renderer
            Renderer rend = quad.GetComponent<Renderer>();
            rend.sharedMaterial = sharedMat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;

            quad.SetActive(false);
            quadPool[i] = quad;
            quadRenderers[i] = rend;
        }
    }

    /// <summary>
    /// Shows cell highlights for a building footprint.
    /// </summary>
    public void ShowHighlight(Vector2Int startCell, int size, PlacedBuilding ignoredBuilding = null)
    {
        activeQuads = 0;
        float margin = 0.05f;
        float quadSize = gridManager.cellSize - margin * 2f;

        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                int idx = x * size + z;
                if (idx >= maxQuads) break;

                Vector2Int cell = new Vector2Int(startCell.x + x, startCell.y + z);
                Vector3 worldCenter = gridManager.GetCellWorldCenter(cell);

                GameObject quad = quadPool[idx];
                quad.SetActive(true);
                quad.transform.position = worldCenter + Vector3.up * 0.08f;
                quad.transform.localScale = new Vector3(quadSize, quadSize, 1f);

                // Color via PropertyBlock — no material instance created
                bool available = (ignoredBuilding != null)
                    ? gridManager.IsAreaAvailable(cell, 1, ignoredBuilding)
                    : gridManager.IsCellAvailable(cell);

                propBlock.SetColor(ColorProp, available ? validCellColor : invalidCellColor);
                quadRenderers[idx].SetPropertyBlock(propBlock);

                activeQuads++;
            }
        }

        // Disable unused quads
        for (int i = activeQuads; i < maxQuads; i++)
            quadPool[i].SetActive(false);
    }

    /// <summary>
    /// Hides all cell highlights.
    /// </summary>
    public void HideHighlight()
    {
        for (int i = 0; i < maxQuads; i++)
        {
            if (quadPool[i] != null)
                quadPool[i].SetActive(false);
        }
        activeQuads = 0;
    }
}
