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
    private int maxQuads = 25; // max 5x5 building
    private Material quadMaterial;
    private int activeQuads = 0;

    void Awake()
    {
        CreateQuadMaterial();
        CreateQuadPool();
    }

    private void CreateQuadMaterial()
    {
        quadMaterial = new Material(Shader.Find("Sprites/Default"));
        quadMaterial.hideFlags = HideFlags.HideAndDontSave;
    }

    private void CreateQuadPool()
    {
        quadPool = new GameObject[maxQuads];
        for (int i = 0; i < maxQuads; i++)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "CellHighlight_" + i;
            quad.transform.SetParent(transform);
            quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // Lay flat

            // Remove collider
            Collider col = quad.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // Set material
            Renderer rend = quad.GetComponent<Renderer>();
            rend.material = quadMaterial;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;

            quad.SetActive(false);
            quadPool[i] = quad;
        }
    }

    /// <summary>
    /// Shows cell highlights for a building footprint.
    /// Call this every frame during placement.
    /// </summary>
    public void ShowHighlight(Vector2Int startCell, int size, PlacedBuilding ignoredBuilding = null)
    {
        activeQuads = 0;
        float margin = 0.05f; // Slight inset so highlight doesn't cover grid lines
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
                quad.transform.position = worldCenter + Vector3.up * 0.08f; // Slight Y offset
                quad.transform.localScale = new Vector3(quadSize, quadSize, 1f);

                // Color based on availability
                bool available;
                if (ignoredBuilding != null)
                    available = gridManager.IsAreaAvailable(cell, 1, ignoredBuilding);
                else
                    available = gridManager.IsCellAvailable(cell);

                Renderer rend = quad.GetComponent<Renderer>();
                rend.material.color = available ? validCellColor : invalidCellColor;

                activeQuads++;
            }
        }

        // Disable unused quads
        for (int i = activeQuads; i < maxQuads; i++)
        {
            quadPool[i].SetActive(false);
        }
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
