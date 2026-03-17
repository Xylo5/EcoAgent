using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Two-level building shop with grid layout and hover tooltips.
/// A fixed "Buildings" button at the top-right of the screen (always visible).
/// Clicking it toggles a centered grid panel with individual building options.
/// Hovering a building for 2 seconds shows a description tooltip.
/// After placing a building, the submenu closes and user must click "Buildings" again.
/// </summary>
public class BuildingUI : MonoBehaviour
{
    [Header("References")]
    public BuildingPlacer buildingPlacer;

    [Header("Building List")]
    public BuildingData[] buildings;

    [Header("UI")]
    public GameObject shopPanel;
    public Transform buttonContainer;
    public GameObject buttonPrefab;

    [Header("Grid Settings")]
    [Tooltip("Number of columns in the building grid.")]
    public int gridColumns = 4;

    [Header("Selection Colors")]
    public Color normalColor = new Color(0.3f, 0.3f, 0.35f, 1f);
    public Color selectedColor = new Color(0.1f, 0.6f, 0.1f, 1f);

    [Header("Tooltip Settings")]
    [Tooltip("Seconds the mouse must hover before the tooltip appears.")]
    public float tooltipDelay = 2f;

    private int selectedIndex = 0;
    private bool submenuOpen = false;
    private bool initialized = false;

    // Fixed "Buildings" button (created programmatically at top-right)
    private GameObject buildingsButtonObj;

    // Building submenu buttons
    private GameObject[] buttonObjects;
    private Image[] buttonImages;

    // Tooltip
    private GameObject tooltipObj;
    private TextMeshProUGUI tooltipText;
    private RectTransform tooltipRect;
    private int hoveredIndex = -1;
    private float hoverTimer = 0f;
    private bool tooltipVisible = false;

    void Start()
    {
        CreateFixedBuildingsButton();
        SetupGridLayout();
        CreateBuildingButtons();
        CreateTooltip();

        // Start with submenu closed
        HideSubmenu();
    }

    void Update()
    {
        // Skip first frame
        if (!initialized)
        {
            initialized = true;
            return;
        }

        if (!submenuOpen) return;

        // Tooltip hover timer
        UpdateTooltipTimer();

        // Tab cycles through buildings in submenu
        if (InputManager.Instance.GetTabDown())
        {
            if (InputManager.Instance.GetShiftHeld())
                selectedIndex = (selectedIndex - 1 + buildings.Length) % buildings.Length;
            else
                selectedIndex = (selectedIndex + 1) % buildings.Length;

            UpdateButtonHighlight();
        }

        // Enter = select the highlighted building (keyboard only — mouse clicks handled by EventTrigger)
        if (InputManager.Instance.GetEnterKeyDown())
        {
            if (buildings.Length > 0)
            {
                SelectBuilding(selectedIndex);
            }
        }

        // Escape = close submenu
        if (InputManager.Instance.GetEscapeDown())
        {
            HideSubmenu();
        }
    }

    // ═══════════════════════════════════════════
    //  FIXED "BUILDINGS" BUTTON (top-right)
    // ═══════════════════════════════════════════

    void CreateFixedBuildingsButton()
    {
        // Find the Canvas that the shopPanel lives on
        Canvas canvas = shopPanel.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        // Create button using the same prefab
        buildingsButtonObj = Instantiate(buttonPrefab, canvas.transform);
        buildingsButtonObj.name = "Btn_Buildings_Fixed";

        // Set text
        TextMeshProUGUI btnText = buildingsButtonObj.GetComponentInChildren<TextMeshProUGUI>();
        if (btnText != null)
            btnText.text = "Buildings";

        // Style the button
        Image btnImage = buildingsButtonObj.GetComponent<Image>();
        if (btnImage != null)
            btnImage.color = new Color(0.15f, 0.55f, 0.30f, 1f); // Green like menu button

        // Position at top-right, anchored to top-right corner
        RectTransform rect = buildingsButtonObj.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-20f, -10f); // 20px from right, 10px from top
            rect.sizeDelta = new Vector2(140f, 45f);
        }

        // Mouse click → toggle submenu
        EventTrigger trigger = buildingsButtonObj.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = buildingsButtonObj.AddComponent<EventTrigger>();

        EventTrigger.Entry clickEntry = new EventTrigger.Entry();
        clickEntry.eventID = EventTriggerType.PointerClick;
        clickEntry.callback.AddListener((_) => ToggleSubmenu());
        trigger.triggers.Add(clickEntry);
    }

    void ToggleSubmenu()
    {
        if (submenuOpen)
            HideSubmenu();
        else
            ShowSubmenu();
    }

    // ═══════════════════════════════════════════
    //  GRID LAYOUT SETUP
    // ═══════════════════════════════════════════

    void SetupGridLayout()
    {
        if (shopPanel == null || buttonContainer == null) return;

        // --- Configure the shop panel to be centered on screen ---
        RectTransform panelRt = shopPanel.GetComponent<RectTransform>();
        if (panelRt != null)
        {
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.anchoredPosition = Vector2.zero;
        }

        // --- Remove any existing layout group on buttonContainer ---
        LayoutGroup existingLayout = buttonContainer.GetComponent<LayoutGroup>();
        if (existingLayout != null)
            DestroyImmediate(existingLayout);

        // --- Add GridLayoutGroup ---
        GridLayoutGroup grid = buttonContainer.gameObject.GetComponent<GridLayoutGroup>();
        if (grid == null)
            grid = buttonContainer.gameObject.AddComponent<GridLayoutGroup>();

        grid.constraintCount = gridColumns;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.cellSize = new Vector2(145f, 75f);
        grid.spacing = new Vector2(14f, 14f);
        grid.padding = new RectOffset(15, 15, 15, 15);
        grid.childAlignment = TextAnchor.MiddleCenter;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;

        // --- Size the panel to fit the grid ---
        int rows = Mathf.CeilToInt((float)buildings.Length / gridColumns);
        // Add extra row of space for future buildings
        int displayRows = Mathf.Max(rows, 3);

        float panelWidth = gridColumns * 145f + (gridColumns - 1) * 14f + 30f;
        float panelHeight = displayRows * 75f + (displayRows - 1) * 14f + 30f;

        if (panelRt != null)
            panelRt.sizeDelta = new Vector2(panelWidth, panelHeight);

        // --- Add an opaque background to the panel if not already there ---
        Image panelImg = shopPanel.GetComponent<Image>();
        if (panelImg == null)
            panelImg = shopPanel.AddComponent<Image>();
        panelImg.color = new Color(0.12f, 0.12f, 0.16f, 0.95f);

        // --- Size the buttonContainer to fill the panel ---
        RectTransform containerRt = buttonContainer.GetComponent<RectTransform>();
        if (containerRt != null)
        {
            containerRt.anchorMin = Vector2.zero;
            containerRt.anchorMax = Vector2.one;
            containerRt.sizeDelta = Vector2.zero;
            containerRt.anchoredPosition = Vector2.zero;
        }
    }

    // ═══════════════════════════════════════════
    //  BUILDING BUTTONS
    // ═══════════════════════════════════════════

    void CreateBuildingButtons()
    {
        buttonObjects = new GameObject[buildings.Length];
        buttonImages = new Image[buildings.Length];

        for (int i = 0; i < buildings.Length; i++)
        {
            BuildingData building = buildings[i];
            GameObject btnObj = Instantiate(buttonPrefab, buttonContainer);
            btnObj.name = "Btn_" + building.buildingName;

            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
                btnText.text = building.buildingName + "\n(" + building.sizeX + "x" + building.sizeZ + ")";

            buttonObjects[i] = btnObj;
            buttonImages[i] = btnObj.GetComponent<Image>();

            int idx = i;
            EventTrigger trigger = btnObj.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = btnObj.AddComponent<EventTrigger>();

            // Hover enter
            EventTrigger.Entry hoverEntry = new EventTrigger.Entry();
            hoverEntry.eventID = EventTriggerType.PointerEnter;
            hoverEntry.callback.AddListener((_) => OnButtonHover(idx));
            trigger.triggers.Add(hoverEntry);

            // Hover exit
            EventTrigger.Entry exitEntry = new EventTrigger.Entry();
            exitEntry.eventID = EventTriggerType.PointerExit;
            exitEntry.callback.AddListener((_) => OnButtonExit(idx));
            trigger.triggers.Add(exitEntry);

            // Click
            EventTrigger.Entry clickEntry = new EventTrigger.Entry();
            clickEntry.eventID = EventTriggerType.PointerClick;
            clickEntry.callback.AddListener((_) => OnButtonClick(idx));
            trigger.triggers.Add(clickEntry);
        }
    }

    // ═══════════════════════════════════════════
    //  TOOLTIP
    // ═══════════════════════════════════════════

    void CreateTooltip()
    {
        Canvas canvas = shopPanel.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        // Tooltip container (lives on the canvas so it can overlap everything)
        tooltipObj = new GameObject("BuildingTooltip");
        tooltipObj.transform.SetParent(canvas.transform, false);

        // Opaque background
        Image bg = tooltipObj.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.12f, 0.98f);

        tooltipRect = tooltipObj.GetComponent<RectTransform>();
        tooltipRect.sizeDelta = new Vector2(250f, 100f);
        tooltipRect.pivot = new Vector2(0f, 0.5f); // Anchor left-center so it appears to the right

        // Text
        GameObject textObj = new GameObject("TooltipText");
        textObj.transform.SetParent(tooltipObj.transform, false);
        tooltipText = textObj.AddComponent<TextMeshProUGUI>();
        tooltipText.fontSize = 16;
        tooltipText.color = Color.white;
        tooltipText.alignment = TextAlignmentOptions.TopLeft;
        tooltipText.enableWordWrapping = true;
        tooltipText.overflowMode = TextOverflowModes.Overflow;

        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = new Vector2(-20f, -16f); // 10px padding on each side
        textRt.anchoredPosition = Vector2.zero;

        tooltipObj.SetActive(false);
    }

    void UpdateTooltipTimer()
    {
        if (hoveredIndex < 0 || hoveredIndex >= buildings.Length)
        {
            HideTooltip();
            return;
        }

        if (!tooltipVisible)
        {
            hoverTimer += Time.unscaledDeltaTime;
            if (hoverTimer >= tooltipDelay)
            {
                ShowTooltip(hoveredIndex);
            }
        }
    }

    void ShowTooltip(int index)
    {
        if (tooltipObj == null || index < 0 || index >= buildings.Length) return;

        BuildingData bd = buildings[index];

        // Build tooltip content
        string title = $"<b>{bd.buildingName}</b>  ({bd.sizeX}x{bd.sizeZ})";
        string pollution = bd.pollutionValue >= 0
            ? $"<color=#FF5555>Pollution: +{bd.pollutionValue}</color>"
            : $"<color=#55FF55>Pollution: {bd.pollutionValue}</color>";
        string desc = string.IsNullOrEmpty(bd.description)
            ? "No description available."
            : bd.description;

        tooltipText.text = $"{title}\n{pollution}\n\n{desc}";

        // Position tooltip to the right of the hovered button
        if (buttonObjects[index] != null)
        {
            RectTransform btnRt = buttonObjects[index].GetComponent<RectTransform>();
            Vector3 btnWorldPos = btnRt.position;

            // Place tooltip to the right of the button
            tooltipRect.position = btnWorldPos;
            tooltipRect.anchoredPosition += new Vector2(btnRt.sizeDelta.x * 0.5f + 10f, 0f);

            // Auto-size height based on text
            tooltipText.ForceMeshUpdate();
            float textHeight = tooltipText.preferredHeight + 20f;
            tooltipRect.sizeDelta = new Vector2(250f, Mathf.Max(80f, textHeight));
        }

        tooltipObj.SetActive(true);
        tooltipVisible = true;
    }

    void HideTooltip()
    {
        if (tooltipObj != null)
            tooltipObj.SetActive(false);
        tooltipVisible = false;
    }

    // ═══════════════════════════════════════════
    //  SUBMENU SHOW / HIDE
    // ═══════════════════════════════════════════

    void ShowSubmenu()
    {
        submenuOpen = true;
        selectedIndex = 0;

        if (shopPanel != null)
            shopPanel.SetActive(true);

        UpdateButtonHighlight();
    }

    void HideSubmenu()
    {
        submenuOpen = false;
        hoveredIndex = -1;
        hoverTimer = 0f;
        HideTooltip();

        if (shopPanel != null)
            shopPanel.SetActive(false);
    }

    void UpdateButtonHighlight()
    {
        for (int i = 0; i < buttonObjects.Length; i++)
        {
            if (buttonImages[i] != null)
                buttonImages[i].color = (i == selectedIndex) ? selectedColor : normalColor;

            buttonObjects[i].transform.localScale = (i == selectedIndex)
                ? Vector3.one * 1.15f
                : Vector3.one;
        }
    }

    void SelectBuilding(int index)
    {
        selectedIndex = index;
        buildingPlacer.StartPlacing(buildings[selectedIndex]);
        HideSubmenu();
    }

    // ═══════════════════════════════════════════
    //  PUBLIC API (called by BuildingPlacer)
    // ═══════════════════════════════════════════

    /// <summary>
    /// Called by BuildingPlacer after placement/cancel.
    /// Closes the submenu — user must click "Buildings" again to reopen.
    /// </summary>
    public void HideShop()
    {
        HideSubmenu();
    }

    /// <summary>
    /// Called by BuildingPlacer after cancel/cleanup.
    /// Now just closes the submenu instead of reopening it.
    /// </summary>
    public void ShowShop()
    {
        // Don't auto-open — user must click "Buildings" button to reopen
        HideSubmenu();
    }

    // ═══════════════════════════════════════════
    //  MOUSE CALLBACKS
    // ═══════════════════════════════════════════

    private void OnButtonHover(int index)
    {
        if (!submenuOpen) return;
        selectedIndex = index;
        UpdateButtonHighlight();

        // Start hover timer for tooltip
        hoveredIndex = index;
        hoverTimer = 0f;
        HideTooltip();
    }

    private void OnButtonExit(int index)
    {
        if (!submenuOpen) return;

        // Immediately hide tooltip and reset timer
        if (hoveredIndex == index)
        {
            hoveredIndex = -1;
            hoverTimer = 0f;
            HideTooltip();
        }
    }

    private void OnButtonClick(int index)
    {
        if (!submenuOpen || buildings.Length == 0) return;
        SelectBuilding(index);
    }
}
