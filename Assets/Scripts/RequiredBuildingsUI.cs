using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Displays a "REQUIRED" button above the Check button in Level HUD.
/// Clicking it toggles a checklist of required buildings.
/// Each entry shows "<buildingName> must be built" in red (missing) or green (placed).
/// Call ScanForBuildings() after any placement/demolition to refresh the checklist.
/// </summary>
public class RequiredBuildingsUI : MonoBehaviour
{
    [Header("Level Requirements")]
    [Tooltip("Assign the LevelRequirements ScriptableObject for this level.")]
    public LevelRequirements levelRequirements;

    // ── Internal state ──
    private bool isExpanded = false;
    private GameObject checklistPanel;
    private List<TextMeshProUGUI> checklistTexts = new List<TextMeshProUGUI>();
    private List<bool> requirementsMet = new List<bool>();

    // Colors
    private readonly Color metColor = new Color(0.2f, 0.85f, 0.3f);   // Green
    private readonly Color unmetColor = new Color(1f, 0.25f, 0.25f);   // Red
    private readonly Color buttonColor = new Color(0.3f, 0.55f, 0.9f); // Blue

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

            requirementsMet[i] = found;

            if (i < checklistTexts.Count && checklistTexts[i] != null)
            {
                checklistTexts[i].color = found ? metColor : unmetColor;
            }
        }
    }

    /// <summary>
    /// Returns true if every required building has at least one placed instance.
    /// </summary>
    public bool AreAllRequirementsMet()
    {
        if (levelRequirements == null || levelRequirements.requiredBuildings == null)
            return true; // No requirements = always met

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

        // ── "REQUIRED" button ──
        // Check button is at anchoredPosition (-20, 20) with size (160, 50).
        // Place this button directly above it.
        GameObject btnObj = new GameObject("RequiredButton");
        btnObj.transform.SetParent(canvas.transform, false);
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = buttonColor;
        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(ToggleChecklist);

        RectTransform btnRt = btnObj.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(1f, 0f);
        btnRt.anchorMax = new Vector2(1f, 0f);
        btnRt.pivot = new Vector2(1f, 0f);
        btnRt.sizeDelta = new Vector2(160, 50);
        btnRt.anchoredPosition = new Vector2(-20, 80); // Above Check button (20 + 50 + 10 gap)

        // Button label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
        labelTmp.text = "REQUIRED";
        labelTmp.color = Color.white;
        labelTmp.fontSize = 24;
        labelTmp.fontStyle = FontStyles.Bold;
        labelTmp.alignment = TextAlignmentOptions.Center;
        RectTransform labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.sizeDelta = Vector2.zero;

        // ── Checklist panel (starts hidden) ──
        CreateChecklistPanel(canvas.transform, btnRt);
    }

    private void CreateChecklistPanel(Transform canvasTransform, RectTransform buttonRt)
    {
        if (levelRequirements == null || levelRequirements.requiredBuildings == null) return;

        int count = levelRequirements.requiredBuildings.Length;
        if (count == 0) return;

        float rowHeight = 35f;
        float padding = 10f;
        float panelHeight = count * rowHeight + padding * 2;
        float panelWidth = 280f;

        // Panel anchored bottom-right, sitting above the REQUIRED button
        checklistPanel = new GameObject("ChecklistPanel");
        checklistPanel.transform.SetParent(canvasTransform, false);
        Image panelImg = checklistPanel.AddComponent<Image>();
        panelImg.color = new Color(0.1f, 0.1f, 0.15f, 0.9f); // Dark semi-transparent

        RectTransform panelRt = checklistPanel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(1f, 0f);
        panelRt.anchorMax = new Vector2(1f, 0f);
        panelRt.pivot = new Vector2(1f, 0f);
        panelRt.sizeDelta = new Vector2(panelWidth, panelHeight);
        // Position above the REQUIRED button: button is at y=80, height=50, so panel starts at y=140
        panelRt.anchoredPosition = new Vector2(-20, 140);

        // Create checklist entries
        checklistTexts.Clear();
        requirementsMet.Clear();

        for (int i = 0; i < count; i++)
        {
            BuildingData bd = levelRequirements.requiredBuildings[i];
            if (bd == null) continue;

            GameObject textObj = new GameObject("Req_" + bd.buildingName);
            textObj.transform.SetParent(checklistPanel.transform, false);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = bd.buildingName + " must be built";
            tmp.color = unmetColor;
            tmp.fontSize = 18;
            tmp.fontStyle = FontStyles.Normal;
            tmp.alignment = TextAlignmentOptions.Left;

            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0f, 1f);
            textRt.anchorMax = new Vector2(1f, 1f);
            textRt.pivot = new Vector2(0.5f, 1f);
            textRt.sizeDelta = new Vector2(-padding * 2, rowHeight);
            textRt.anchoredPosition = new Vector2(0, -(padding + i * rowHeight));

            checklistTexts.Add(tmp);
            requirementsMet.Add(false);
        }

        // Start collapsed
        checklistPanel.SetActive(false);
    }

    // ═══════════════════════════════════════════
    //  TOGGLE
    // ═══════════════════════════════════════════

    private void ToggleChecklist()
    {
        if (checklistPanel == null) return;

        isExpanded = !isExpanded;
        checklistPanel.SetActive(isExpanded);

        // Refresh when opening
        if (isExpanded)
            ScanForBuildings();
    }
}
