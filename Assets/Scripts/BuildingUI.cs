using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Two-level building shop.
/// A fixed "Buildings" button at the top-right of the screen (always visible).
/// Clicking it toggles a submenu ribbon with individual building options.
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

    [Header("Selection Colors")]
    public Color normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    public Color selectedColor = new Color(0.1f, 0.6f, 0.1f, 1f);

    private int selectedIndex = 0;
    private bool submenuOpen = false;
    private bool initialized = false;

    // Fixed "Buildings" button (created programmatically at top-right)
    private GameObject buildingsButtonObj;

    // Building submenu buttons
    private GameObject[] buttonObjects;
    private Image[] buttonImages;

    void Start()
    {
        CreateFixedBuildingsButton();
        CreateBuildingButtons();

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

        // Tab cycles through buildings in submenu
        if (InputManager.Instance.GetTabDown())
        {
            if (InputManager.Instance.GetShiftHeld())
                selectedIndex = (selectedIndex - 1 + buildings.Length) % buildings.Length;
            else
                selectedIndex = (selectedIndex + 1) % buildings.Length;

            UpdateButtonHighlight();
        }

        // Enter = select the highlighted building
        if (InputManager.Instance.GetEnterDown())
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
    //  BUILDING SUBMENU
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
                btnText.text = building.buildingName + "\n(" + building.sizeInCells + "x" + building.sizeInCells + ")";

            buttonObjects[i] = btnObj;
            buttonImages[i] = btnObj.GetComponent<Image>();

            int idx = i;
            EventTrigger trigger = btnObj.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = btnObj.AddComponent<EventTrigger>();

            EventTrigger.Entry hoverEntry = new EventTrigger.Entry();
            hoverEntry.eventID = EventTriggerType.PointerEnter;
            hoverEntry.callback.AddListener((_) => OnButtonHover(idx));
            trigger.triggers.Add(hoverEntry);

            EventTrigger.Entry clickEntry = new EventTrigger.Entry();
            clickEntry.eventID = EventTriggerType.PointerClick;
            clickEntry.callback.AddListener((_) => OnButtonClick(idx));
            trigger.triggers.Add(clickEntry);
        }
    }

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
    }

    private void OnButtonClick(int index)
    {
        if (!submenuOpen || buildings.Length == 0) return;
        SelectBuilding(index);
    }
}
