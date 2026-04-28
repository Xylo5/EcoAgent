using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Displays a "Development Goals" panel in the top-left, directly below the city name box.
/// Always visible — shows a checklist of required buildings with live status indicators.
/// Each entry updates in real-time: ✗ (red) when missing, ✓ (green) when placed.
/// Call ScanForBuildings() after any placement/demolition to refresh.
/// </summary>
public class RequiredBuildingsUI : MonoBehaviour
{
    [Header("Level Requirements")]
    [Tooltip("Assign the LevelRequirements ScriptableObject for this level.")]
    public LevelRequirements levelRequirements;

    [Header("Panel Position")]
    [Tooltip("Offset from top-left corner. Align below CityNameUI (default: x=20, y=-75).")]
    public Vector2 panelOffset = new Vector2(20f, -75f);
    [Tooltip("Width of the panel.")]
    public float panelWidth = 240f;

    [Header("Panel Style")]
    [Tooltip("Background color of the panel (use alpha for semi-transparency).")]
    public Color panelColor = new Color(0.08f, 0.08f, 0.12f, 0.55f);
    [Tooltip("Color of the header title.")]
    public Color headerColor = new Color(0.95f, 0.85f, 0.45f, 1f);
    [Tooltip("Font size for the header.")]
    public float headerFontSize = 16f;
    [Tooltip("Font size for list items.")]
    public float itemFontSize = 14f;

    [Header("Status Colors")]
    [Tooltip("Color for completed requirements.")]
    public Color completedColor = new Color(0.3f, 0.9f, 0.4f, 1f);
    [Tooltip("Color for incomplete requirements.")]
    public Color incompleteColor = new Color(1f, 0.4f, 0.35f, 0.9f);

    // ── Internal state ──
    private GameObject panelObj;
    private List<TextMeshProUGUI> itemTexts = new List<TextMeshProUGUI>();
    private List<TextMeshProUGUI> statusIcons = new List<TextMeshProUGUI>();
    private List<bool> requirementsMet = new List<bool>();

    // Layout constants
    private const float HEADER_HEIGHT = 32f;
    private const float ITEM_HEIGHT = 26f;
    private const float DIVIDER_HEIGHT = 1f;
    private const float PADDING_TOP = 10f;
    private const float PADDING_BOTTOM = 10f;
    private const float PADDING_HORIZONTAL = 14f;
    private const float ICON_WIDTH = 24f;

    void Start()
    {
        CreateUI();
        ScanForBuildings();
    }

    // ═══════════════════════════════════════════
    //  PUBLIC API
    // ═══════════════════════════════════════════

    /// <summary>
    /// Scans all PlacedBuilding instances in the scene and updates the checklist.
    /// Call this after every building placement, move, or demolition.
    /// </summary>
    public void ScanForBuildings()
    {
        if (levelRequirements == null || levelRequirements.requiredBuildings == null) return;

        PlacedBuilding[] allPlaced = FindObjectsByType<PlacedBuilding>(FindObjectsSortMode.None);

        for (int i = 0; i < levelRequirements.requiredBuildings.Length; i++)
        {
            BuildingData required = levelRequirements.requiredBuildings[i];
            if (required == null) continue;

            bool found = false;
            foreach (PlacedBuilding pb in allPlaced)
            {
                if (pb.buildingData == required)
                {
                    found = true;
                    break;
                }
            }

            if (i < requirementsMet.Count)
                requirementsMet[i] = found;

            // Update visual
            if (i < statusIcons.Count && statusIcons[i] != null)
            {
                statusIcons[i].text = found ? "✓" : "✗";
                statusIcons[i].color = found ? completedColor : incompleteColor;
            }
            if (i < itemTexts.Count && itemTexts[i] != null)
            {
                itemTexts[i].color = found ? completedColor : incompleteColor;
                itemTexts[i].fontStyle = found ? FontStyles.Strikethrough : FontStyles.Normal;
            }
        }
    }

    /// <summary>
    /// Returns true if every required building has at least one placed instance.
    /// </summary>
    public bool AreAllRequirementsMet()
    {
        if (levelRequirements == null || levelRequirements.requiredBuildings == null)
            return true;

        for (int i = 0; i < requirementsMet.Count; i++)
        {
            if (!requirementsMet[i]) return false;
        }
        return true;
    }

    // ═══════════════════════════════════════════
    //  UI CREATION
    // ═══════════════════════════════════════════

    private void CreateUI()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        if (levelRequirements == null || levelRequirements.requiredBuildings == null) return;
        int count = levelRequirements.requiredBuildings.Length;
        if (count == 0) return;

        // Calculate panel height
        float contentHeight = PADDING_TOP + HEADER_HEIGHT + DIVIDER_HEIGHT + (count * ITEM_HEIGHT) + PADDING_BOTTOM;

        // ── Panel container ──
        panelObj = new GameObject("DevelopmentGoalsPanel");
        panelObj.transform.SetParent(canvas.transform, false);

        Image panelImg = panelObj.AddComponent<Image>();
        panelImg.color = panelColor;
        panelImg.raycastTarget = false;

        RectTransform panelRt = panelObj.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0f, 1f);
        panelRt.anchorMax = new Vector2(0f, 1f);
        panelRt.pivot = new Vector2(0f, 1f);
        panelRt.sizeDelta = new Vector2(panelWidth, contentHeight);
        panelRt.anchoredPosition = panelOffset;

        float yOffset = -PADDING_TOP;

        // ── Header: "DEVELOPMENT GOALS" ──
        yOffset = CreateHeader(panelObj.transform, yOffset);

        // ── Thin divider line ──
        yOffset = CreateDivider(panelObj.transform, yOffset);

        // ── Requirement list items ──
        itemTexts.Clear();
        statusIcons.Clear();
        requirementsMet.Clear();

        for (int i = 0; i < count; i++)
        {
            BuildingData bd = levelRequirements.requiredBuildings[i];
            if (bd == null) continue;

            yOffset = CreateListItem(panelObj.transform, yOffset, bd.buildingName, i);
            requirementsMet.Add(false);
        }
    }

    private float CreateHeader(Transform parent, float yOffset)
    {
        // Header container for left-aligned title
        GameObject headerObj = new GameObject("Header");
        headerObj.transform.SetParent(parent, false);

        TextMeshProUGUI headerText = headerObj.AddComponent<TextMeshProUGUI>();
        headerText.text = "DEVELOPMENT GOALS";
        headerText.color = headerColor;
        headerText.fontSize = headerFontSize;
        headerText.fontStyle = FontStyles.Bold;
        headerText.alignment = TextAlignmentOptions.Left;
        headerText.enableWordWrapping = false;
        headerText.raycastTarget = false;

        // Add letter spacing for a premium feel
        headerText.characterSpacing = 2.5f;

        RectTransform rt = headerObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(-PADDING_HORIZONTAL * 2, HEADER_HEIGHT);
        rt.anchoredPosition = new Vector2(0f, yOffset);

        return yOffset - HEADER_HEIGHT;
    }

    private float CreateDivider(Transform parent, float yOffset)
    {
        GameObject divObj = new GameObject("Divider");
        divObj.transform.SetParent(parent, false);

        Image divImg = divObj.AddComponent<Image>();
        // Subtle gradient-like divider
        divImg.color = new Color(headerColor.r, headerColor.g, headerColor.b, 0.3f);
        divImg.raycastTarget = false;

        RectTransform rt = divObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(-PADDING_HORIZONTAL * 2, DIVIDER_HEIGHT);
        rt.anchoredPosition = new Vector2(0f, yOffset - 2f);

        return yOffset - DIVIDER_HEIGHT - 4f;
    }

    private float CreateListItem(Transform parent, float yOffset, string buildingName, int index)
    {
        // ── Row container ──
        GameObject rowObj = new GameObject("Goal_" + buildingName);
        rowObj.transform.SetParent(parent, false);

        RectTransform rowRt = rowObj.AddComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0f, 1f);
        rowRt.anchorMax = new Vector2(1f, 1f);
        rowRt.pivot = new Vector2(0.5f, 1f);
        rowRt.sizeDelta = new Vector2(-PADDING_HORIZONTAL * 2, ITEM_HEIGHT);
        rowRt.anchoredPosition = new Vector2(0f, yOffset);

        // ── Status icon (✗ / ✓) ──
        GameObject iconObj = new GameObject("StatusIcon");
        iconObj.transform.SetParent(rowObj.transform, false);

        TextMeshProUGUI iconText = iconObj.AddComponent<TextMeshProUGUI>();
        iconText.text = "✗";
        iconText.color = incompleteColor;
        iconText.fontSize = itemFontSize + 2;
        iconText.fontStyle = FontStyles.Bold;
        iconText.alignment = TextAlignmentOptions.Left;
        iconText.enableWordWrapping = false;
        iconText.raycastTarget = false;

        RectTransform iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0f, 0f);
        iconRt.anchorMax = new Vector2(0f, 1f);
        iconRt.pivot = new Vector2(0f, 0.5f);
        iconRt.sizeDelta = new Vector2(ICON_WIDTH, 0f);
        iconRt.anchoredPosition = new Vector2(0f, 0f);

        statusIcons.Add(iconText);

        // ── Building name text ──
        GameObject nameObj = new GameObject("Name");
        nameObj.transform.SetParent(rowObj.transform, false);

        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = buildingName;
        nameText.color = incompleteColor;
        nameText.fontSize = itemFontSize;
        nameText.fontStyle = FontStyles.Normal;
        nameText.alignment = TextAlignmentOptions.Left;
        nameText.enableWordWrapping = false;
        nameText.overflowMode = TextOverflowModes.Ellipsis;
        nameText.raycastTarget = false;

        RectTransform nameRt = nameObj.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0f, 0f);
        nameRt.anchorMax = new Vector2(1f, 1f);
        nameRt.pivot = new Vector2(0.5f, 0.5f);
        nameRt.offsetMin = new Vector2(ICON_WIDTH, 0f); // left edge after icon
        nameRt.offsetMax = new Vector2(0f, 0f);

        itemTexts.Add(nameText);

        return yOffset - ITEM_HEIGHT;
    }
}
