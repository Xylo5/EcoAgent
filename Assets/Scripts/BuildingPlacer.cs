using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Handles building placement with full keyboard controls (CoC-style).
/// Arrow keys move the ghost tile-by-tile, Enter confirms, Escape cancels.
/// Has a 1-frame cooldown after entering placement mode so Enter doesn't
/// instantly confirm (since BuildingUI also uses Enter to start placing).
/// </summary>
public class BuildingPlacer : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;
    public Camera mainCamera;
    public GridCellHighlighter cellHighlighter;
    public PlacementConfirmUI confirmUI;
    public BuildingUI buildingUI;

    [Header("Placement Settings")]
    public LayerMask buildingLayer;
    public float ghostLiftHeight = 0.5f;

    [Header("Tile Movement")]
    public float moveRepeatDelay = 0.3f;
    public float moveRepeatRate = 0.1f;

    // ── State ──
    private enum PlacerState { Idle, PlacingNew, MovingExisting }
    private PlacerState state = PlacerState.Idle;

    private BuildingData currentBuildingData;
    private GameObject ghostObject;
    private PlacedBuilding movingBuilding;
    private bool canPlace = false;

    // Grid cell position of the ghost (bottom-left corner)
    private Vector2Int ghostGridCell;

    // Arrow key repeat timer
    private float nextMoveTime = 0f;

    // Mouse tracking
    private Vector2 lastMousePos;

    // Cooldown: ignore Enter for N frames after entering placement
    private int confirmCooldown = 0;

    void Update()
    {
        // Tick down the cooldown
        if (confirmCooldown > 0)
            confirmCooldown--;

        if (state == PlacerState.Idle)
            return;

        HandleMovement();
        HandleConfirmCancel();
    }

    // ═══════════════════════════════════════════
    //  PUBLIC API
    // ═══════════════════════════════════════════

    public void StartPlacing(BuildingData building)
    {
        CancelPlacement();

        currentBuildingData = building;
        state = PlacerState.PlacingNew;
        movingBuilding = null;

        // Block Enter for 2 frames so the same keypress doesn't confirm
        confirmCooldown = 2;

        // Fallback: auto-find camera if not assigned
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (building.prefab == null)
        {
            Debug.LogError($"[BuildingPlacer] Cannot place '{building.buildingName}' — prefab is not assigned!");
            state = PlacerState.Idle;
            return;
        }

        // Start ghost at grid center
        int centerX = (gridManager.gridWidth - building.sizeInCells) / 2;
        int centerZ = (gridManager.gridHeight - building.sizeInCells) / 2;

        // Try placing at mouse position first, if valid based on raycast, else fallback
        lastMousePos = InputManager.Instance.GetMousePosition();
        Ray ray = mainCamera.ScreenPointToRay(lastMousePos);
        float terrainY = gridManager.terrain != null ? gridManager.terrain.transform.position.y : 0f;
        Plane plane = new Plane(Vector3.up, new Vector3(0, terrainY, 0));
        if (plane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            ghostGridCell = gridManager.GetBuildingGridCell(hitPoint, building.sizeInCells);
        }
        else
        {
            ghostGridCell = new Vector2Int(centerX, centerZ);
        }

        Vector3 worldPos = GridCellToWorldPos(ghostGridCell, building.sizeInCells);
        worldPos.y += ghostLiftHeight;

        ghostObject = Instantiate(building.prefab, worldPos, Quaternion.identity);
        ghostObject.name = "Ghost_" + building.buildingName;

        foreach (Collider col in ghostObject.GetComponentsInChildren<Collider>())
            col.enabled = false;

        SetGhostTransparency(building.validColor);

        if (confirmUI != null)
            confirmUI.Show(ghostObject.transform);

        UpdateValidity();

        Debug.Log("[BuildingPlacer] Placing: " + building.buildingName +
                  " — Arrow keys to move, Enter to place, Escape to cancel");
    }

    public void ConfirmPlacement()
    {
        if (!canPlace) return;

        if (state == PlacerState.PlacingNew)
            ConfirmNewPlacement();
        else if (state == PlacerState.MovingExisting)
            ConfirmMovePlacement();
    }

    public void CancelPlacement()
    {
        if (state == PlacerState.MovingExisting && movingBuilding != null)
        {
            movingBuilding.CancelMove(gridManager);
            movingBuilding = null;
        }

        if (ghostObject != null)
            Destroy(ghostObject);

        ghostObject = null;
        currentBuildingData = null;
        state = PlacerState.Idle;

        if (cellHighlighter != null)
            cellHighlighter.HideHighlight();
        if (confirmUI != null)
            confirmUI.Hide();

        // Re-show the shop
        if (buildingUI != null)
            buildingUI.ShowShop();
    }

    // ═══════════════════════════════════════════
    //  MOVEMENT (Mouse + Arrow Keys)
    // ═══════════════════════════════════════════

    private void HandleMovement()
    {
        if (ghostObject == null || currentBuildingData == null) return;

        int size = currentBuildingData.sizeInCells;
        bool positionChanged = false;

        // --- Mouse Movement (skip if pointer is over UI) ---
        Vector2 currentMousePos = InputManager.Instance.GetMousePosition();
        bool pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (!pointerOverUI && (currentMousePos - lastMousePos).sqrMagnitude > 2f) // If mouse moved
        {
            lastMousePos = currentMousePos;
            Ray ray = mainCamera.ScreenPointToRay(currentMousePos);
            // Math plane at terrain height
            float terrainY = gridManager.terrain != null ? gridManager.terrain.transform.position.y : 0f;
            Plane plane = new Plane(Vector3.up, new Vector3(0, terrainY, 0));
            if (plane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                Vector2Int mouseCell = gridManager.GetBuildingGridCell(hitPoint, size);

                if (mouseCell != ghostGridCell)
                {
                    ghostGridCell = mouseCell;
                    positionChanged = true;
                }
            }
        }

        // --- Keyboard Movement ---
        Vector2Int move = Vector2Int.zero;

        if (InputManager.Instance.GetUpArrowHeld())    move.y += 1;
        if (InputManager.Instance.GetDownArrowHeld())  move.y -= 1;
        if (InputManager.Instance.GetRightArrowHeld()) move.x += 1;
        if (InputManager.Instance.GetLeftArrowHeld())  move.x -= 1;

        if (move != Vector2Int.zero)
        {
            bool shouldMove = false;

            // First press — move immediately
            if (InputManager.Instance.GetUpArrowDown() || InputManager.Instance.GetDownArrowDown() ||
                InputManager.Instance.GetRightArrowDown() || InputManager.Instance.GetLeftArrowDown())
            {
                shouldMove = true;
                nextMoveTime = Time.time + moveRepeatDelay;
            }
            // Held down — auto-repeat
            else if (Time.time >= nextMoveTime)
            {
                shouldMove = true;
                nextMoveTime = Time.time + moveRepeatRate;
            }

            if (shouldMove)
            {
                Vector2Int newCell = ghostGridCell + move;

                // Clamp within grid
                newCell.x = Mathf.Clamp(newCell.x, 0, gridManager.gridWidth - size);
                newCell.y = Mathf.Clamp(newCell.y, 0, gridManager.gridHeight - size);

                if (newCell != ghostGridCell)
                {
                    ghostGridCell = newCell;
                    positionChanged = true;
                    // Keep mouse sync from overriding key movement if mouse isn't moving
                    lastMousePos = currentMousePos; 
                }
            }
        }

        // --- Apply Position ---
        if (positionChanged)
        {
            Vector3 worldPos = GridCellToWorldPos(ghostGridCell, size);
            worldPos.y += ghostLiftHeight;
            ghostObject.transform.position = worldPos;

            UpdateValidity();
        }
    }

    // ═══════════════════════════════════════════
    //  CONFIRM / CANCEL (Enter / Escape)
    // ═══════════════════════════════════════════

    private void HandleConfirmCancel()
    {
        // Enter = confirm (only after cooldown expires, and not when clicking on UI)
        if (confirmCooldown <= 0 && InputManager.Instance.GetEnterDown())
        {
            // Skip if the click landed on a UI element
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (canPlace)
            {
                ConfirmPlacement();
            }
            else
            {
                Debug.Log("[BuildingPlacer] Cannot place here — cells are occupied!");
            }
        }

        // Escape = cancel
        if (InputManager.Instance.GetEscapeDown())
        {
            Debug.Log("[BuildingPlacer] Placement cancelled.");
            CancelPlacement();
        }
    }

    // ═══════════════════════════════════════════
    //  VALIDITY + HIGHLIGHTS
    // ═══════════════════════════════════════════

    private void UpdateValidity()
    {
        if (ghostObject == null || currentBuildingData == null) return;

        int size = currentBuildingData.sizeInCells;

        if (state == PlacerState.MovingExisting && movingBuilding != null)
            canPlace = gridManager.IsAreaAvailable(ghostGridCell, size, movingBuilding);
        else
            canPlace = gridManager.IsAreaAvailable(ghostGridCell, size);

        if (cellHighlighter != null)
            cellHighlighter.ShowHighlight(ghostGridCell, size,
                state == PlacerState.MovingExisting ? movingBuilding : null);

        SetGhostTransparency(canPlace ? currentBuildingData.validColor
                                      : currentBuildingData.invalidColor);

        if (confirmUI != null)
            confirmUI.SetConfirmInteractable(canPlace);
    }

    // ═══════════════════════════════════════════
    //  CONFIRM PLACEMENT
    // ═══════════════════════════════════════════

    private void ConfirmNewPlacement()
    {
        int size = currentBuildingData.sizeInCells;
        Vector3 finalPos = GridCellToWorldPos(ghostGridCell, size);

        GameObject building = Instantiate(currentBuildingData.prefab, finalPos, Quaternion.identity);
        building.name = currentBuildingData.buildingName;
        building.layer = LayerMaskToLayer(buildingLayer);
        SetLayerRecursive(building, building.layer);

        PlacedBuilding pb = building.AddComponent<PlacedBuilding>();
        pb.gridCell = ghostGridCell;
        pb.sizeInCells = size;
        pb.buildingData = currentBuildingData;

        if (building.GetComponent<BoxCollider>() == null)
            building.AddComponent<BoxCollider>();

        gridManager.OccupyCells(ghostGridCell, size);

        if (PollutionManager.Instance != null)
            PollutionManager.Instance.AddPollution(currentBuildingData.pollutionValue);

        Debug.Log($"[BuildingPlacer] Placed {currentBuildingData.buildingName} at ({ghostGridCell.x}, {ghostGridCell.y})");

        CleanupPlacement();
    }

    private void ConfirmMovePlacement()
    {
        int size = currentBuildingData.sizeInCells;
        Vector3 finalPos = GridCellToWorldPos(ghostGridCell, size);

        movingBuilding.PutDown(gridManager, finalPos, ghostGridCell);
        SetBuildingVisible(movingBuilding.gameObject, true);

        Debug.Log($"[BuildingPlacer] Moved {currentBuildingData.buildingName} to ({ghostGridCell.x}, {ghostGridCell.y})");

        movingBuilding = null;
        CleanupPlacement();
    }

    private void CleanupPlacement()
    {
        Destroy(ghostObject);
        ghostObject = null;
        currentBuildingData = null;
        state = PlacerState.Idle;

        if (cellHighlighter != null) cellHighlighter.HideHighlight();
        if (confirmUI != null) confirmUI.Hide();
        if (buildingUI != null) buildingUI.ShowShop();
    }

    // ═══════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════

    private Vector3 GridCellToWorldPos(Vector2Int cell, int size)
    {
        Vector3 terrainPos = gridManager.terrain != null ? gridManager.terrain.transform.position : Vector3.zero;
        float x = terrainPos.x + cell.x * gridManager.cellSize + (size * gridManager.cellSize) / 2f;
        float z = terrainPos.z + cell.y * gridManager.cellSize + (size * gridManager.cellSize) / 2f;
        float y = gridManager.terrain != null ? gridManager.terrain.SampleHeight(new Vector3(x, 0, z)) + terrainPos.y : terrainPos.y;
        return new Vector3(x, y, z);
    }

    private void SetGhostTransparency(Color color)
    {
        if (ghostObject == null) return;
        foreach (Renderer rend in ghostObject.GetComponentsInChildren<Renderer>())
        {
            foreach (Material mat in rend.materials)
            {
                mat.color = color;
                mat.SetFloat("_Mode", 3);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
            }
        }
    }

    private void SetBuildingVisible(GameObject obj, bool visible)
    {
        foreach (Renderer rend in obj.GetComponentsInChildren<Renderer>())
            rend.enabled = visible;
    }

    private int LayerMaskToLayer(LayerMask mask)
    {
        int value = mask.value;
        for (int i = 0; i < 32; i++)
        {
            if ((value & (1 << i)) != 0)
                return i;
        }
        return 0;
    }

    private void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, layer);
    }
}
