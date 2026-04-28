using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays the city/level name as a clickable button at the top-left corner.
/// Clicking toggles a centered info panel with a header tagline and description.
/// Fully configurable from the Inspector: name, tagline, description, box size, colors,
/// and optional background image. Independent script — attach to any GameObject under a Canvas.
/// </summary>
public class CityNameUI : MonoBehaviour
{
    [Header("City Name")]
    [Tooltip("The city name to display. Easily editable per scene.")]
    public string cityName = "My City";

    [Header("City Info Panel")]
    [Tooltip("Header tagline shown at the top of the info panel.")]
    [TextArea(1, 2)]
    public string headerTagline = "A City of Tomorrow";
    [Tooltip("Description text shown in the info panel. Supports longer text with scrolling.")]
    [TextArea(3, 10)]
    public string description = "This thriving metropolis faces the challenge of balancing growth with environmental sustainability. Your decisions will shape the future of this city and its people.";

    [Header("Info Panel Settings")]
    [Tooltip("Size of the info panel in pixels (width, height).")]
    public Vector2 infoPanelSize = new Vector2(460f, 300f);
    [Tooltip("Background color of the info panel.")]
    public Color infoPanelColor = new Color(0.12f, 0.12f, 0.16f, 1f);

    [Header("Box Settings")]
    [Tooltip("Size of the name button in pixels (width, height).")]
    public Vector2 boxSize = new Vector2(220f, 50f);
    [Tooltip("Offset from the top-left corner (x = right, y = down).")]
    public Vector2 boxOffset = new Vector2(20f, -15f);
    [Tooltip("Background color of the box (use alpha for semi-transparency).")]
    public Color boxColor = new Color(0.1f, 0.1f, 0.15f, 0.6f);
    [Tooltip("Hover highlight color.")]
    public Color boxHoverColor = new Color(0.15f, 0.15f, 0.22f, 0.75f);

    [Header("Background Image (Optional)")]
    [Tooltip("Optional sprite to use as box background instead of solid color.")]
    public Sprite backgroundImage;
    [Tooltip("Transparency of the background image (0 = invisible, 1 = opaque).")]
    [Range(0f, 1f)]
    public float imageAlpha = 0.6f;

    [Header("Text Settings")]
    [Tooltip("Font size for the city name button.")]
    public float fontSize = 22f;
    [Tooltip("Text color.")]
    public Color textColor = Color.white;
    [Tooltip("Font style.")]
    public FontStyles fontStyle = FontStyles.Bold;
    [Tooltip("Optional TMP font asset. Leave null to use the default.")]
    public TMP_FontAsset fontAsset;

    // Internal references
    private GameObject boxObj;
    private Image boxImage;
    private TextMeshProUGUI nameText;
    private GameObject infoPanelObj;
    private bool isPanelOpen = false;

    void Start()
    {
        CreateUI();
    }

    /// <summary>
    /// Change the displayed city name at runtime.
    /// </summary>
    public void SetCityName(string newName)
    {
        cityName = newName;
        if (nameText != null)
            nameText.text = cityName;
    }

    /// <summary>
    /// Swap the background image at runtime.
    /// Pass null to revert to the solid color box.
    /// </summary>
    public void SetBackgroundImage(Sprite sprite, float alpha = 0.6f)
    {
        backgroundImage = sprite;
        imageAlpha = alpha;
        ApplyBackground();
    }

    // ═══════════════════════════════════════════
    //  UI CREATION
    // ═══════════════════════════════════════════

    private void CreateUI()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[CityNameUI] No Canvas found!");
            return;
        }

        CreateCityNameButton(canvas);
        CreateInfoPanel(canvas);
    }

    private void CreateCityNameButton(Canvas canvas)
    {
        // ── Clickable box ──
        boxObj = new GameObject("CityNameButton");
        boxObj.transform.SetParent(canvas.transform, false);

        boxImage = boxObj.AddComponent<Image>();
        ApplyBackground();

        // Add Button component for click handling
        Button btn = boxObj.AddComponent<Button>();
        btn.onClick.AddListener(ToggleInfoPanel);

        // Hover color transitions
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.3f, 1f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.9f, 1f);
        colors.selectedColor = Color.white;
        btn.colors = colors;

        // Anchor top-left
        RectTransform boxRt = boxObj.GetComponent<RectTransform>();
        boxRt.anchorMin = new Vector2(0f, 1f);
        boxRt.anchorMax = new Vector2(0f, 1f);
        boxRt.pivot = new Vector2(0f, 1f);
        boxRt.sizeDelta = boxSize;
        boxRt.anchoredPosition = boxOffset;

        // ── City name text ──
        GameObject textObj = new GameObject("CityNameText");
        textObj.transform.SetParent(boxObj.transform, false);

        nameText = textObj.AddComponent<TextMeshProUGUI>();
        nameText.text = cityName;
        nameText.color = textColor;
        nameText.fontSize = fontSize;
        nameText.fontStyle = fontStyle;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.enableWordWrapping = false;
        nameText.overflowMode = TextOverflowModes.Ellipsis;
        nameText.raycastTarget = false;

        if (fontAsset != null)
            nameText.font = fontAsset;

        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = new Vector2(-16f, -8f);
        textRt.anchoredPosition = Vector2.zero;
    }

    private void CreateInfoPanel(Canvas canvas)
    {
        // ── Info panel (centered, starts hidden) ──
        infoPanelObj = new GameObject("CityInfoPanel");
        infoPanelObj.transform.SetParent(canvas.transform, false);

        Image panelImg = infoPanelObj.AddComponent<Image>();
        panelImg.color = infoPanelColor;
        panelImg.raycastTarget = true;

        RectTransform panelRt = infoPanelObj.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = infoPanelSize;
        panelRt.anchoredPosition = Vector2.zero;

        // ── Close button (X) in top-right corner of panel ──
        CreateCloseButton(infoPanelObj.transform);

        // ── Content placed directly inside the panel (no ScrollView/Mask) ──
        float pad = 24f;
        float topPad = 20f;
        float yOffset = -topPad;

        // ── City name title ──
        GameObject titleObj = new GameObject("PanelTitle");
        titleObj.transform.SetParent(infoPanelObj.transform, false);

        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = cityName;
        titleText.color = Color.white;
        titleText.fontSize = 30f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.TopLeft;
        titleText.enableWordWrapping = false;
        titleText.raycastTarget = false;

        if (fontAsset != null)
            titleText.font = fontAsset;

        float titleHeight = 40f;
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0f, 1f);
        titleRt.offsetMin = new Vector2(pad, 0f);
        titleRt.offsetMax = new Vector2(-pad - 40f, 0f); // leave room for close button
        titleRt.sizeDelta = new Vector2(titleRt.sizeDelta.x, titleHeight);
        titleRt.anchoredPosition = new Vector2(titleRt.anchoredPosition.x, yOffset);
        yOffset -= titleHeight;

        // ── Tagline ──
        GameObject tagObj = new GameObject("Tagline");
        tagObj.transform.SetParent(infoPanelObj.transform, false);

        TextMeshProUGUI tagText = tagObj.AddComponent<TextMeshProUGUI>();
        tagText.text = headerTagline;
        tagText.color = new Color(0.95f, 0.85f, 0.45f, 1f); // Golden
        tagText.fontSize = 18f;
        tagText.fontStyle = FontStyles.Italic;
        tagText.alignment = TextAlignmentOptions.TopLeft;
        tagText.enableWordWrapping = true;
        tagText.raycastTarget = false;

        if (fontAsset != null)
            tagText.font = fontAsset;

        float tagHeight = 32f;
        RectTransform tagRt = tagObj.GetComponent<RectTransform>();
        tagRt.anchorMin = new Vector2(0f, 1f);
        tagRt.anchorMax = new Vector2(1f, 1f);
        tagRt.pivot = new Vector2(0f, 1f);
        tagRt.offsetMin = new Vector2(pad, 0f);
        tagRt.offsetMax = new Vector2(-pad, 0f);
        tagRt.sizeDelta = new Vector2(tagRt.sizeDelta.x, tagHeight);
        tagRt.anchoredPosition = new Vector2(tagRt.anchoredPosition.x, yOffset);
        yOffset -= tagHeight;

        // ── Divider ──
        yOffset -= 6f;
        GameObject divObj = new GameObject("Divider");
        divObj.transform.SetParent(infoPanelObj.transform, false);

        Image divImg = divObj.AddComponent<Image>();
        divImg.color = new Color(1f, 1f, 1f, 0.2f);
        divImg.raycastTarget = false;

        RectTransform divRt = divObj.GetComponent<RectTransform>();
        divRt.anchorMin = new Vector2(0f, 1f);
        divRt.anchorMax = new Vector2(1f, 1f);
        divRt.pivot = new Vector2(0f, 1f);
        divRt.offsetMin = new Vector2(pad, 0f);
        divRt.offsetMax = new Vector2(-pad, 0f);
        divRt.sizeDelta = new Vector2(divRt.sizeDelta.x, 2f);
        divRt.anchoredPosition = new Vector2(divRt.anchoredPosition.x, yOffset);
        yOffset -= 2f;
        yOffset -= 12f;

        // ── Description ──
        GameObject descObj = new GameObject("Description");
        descObj.transform.SetParent(infoPanelObj.transform, false);

        TextMeshProUGUI descText = descObj.AddComponent<TextMeshProUGUI>();
        descText.text = description;
        descText.color = new Color(0.9f, 0.9f, 0.95f, 1f);
        descText.fontSize = 15f;
        descText.fontStyle = FontStyles.Normal;
        descText.alignment = TextAlignmentOptions.TopLeft;
        descText.enableWordWrapping = true;
        descText.overflowMode = TextOverflowModes.Overflow;
        descText.raycastTarget = false;
        descText.lineSpacing = 8f;

        if (fontAsset != null)
            descText.font = fontAsset;

        float descHeight = Mathf.Abs(yOffset) - pad; // fill remaining space
        RectTransform descRt = descObj.GetComponent<RectTransform>();
        descRt.anchorMin = new Vector2(0f, 1f);
        descRt.anchorMax = new Vector2(1f, 1f);
        descRt.pivot = new Vector2(0f, 1f);
        descRt.offsetMin = new Vector2(pad, 0f);
        descRt.offsetMax = new Vector2(-pad, 0f);
        descRt.sizeDelta = new Vector2(descRt.sizeDelta.x, descHeight);
        descRt.anchoredPosition = new Vector2(descRt.anchoredPosition.x, yOffset);

        // Start hidden
        infoPanelObj.SetActive(false);
    }

    private void CreateCloseButton(Transform panelParent)
    {
        GameObject closeBtnObj = new GameObject("CloseButton");
        closeBtnObj.transform.SetParent(panelParent, false);

        Image closeImg = closeBtnObj.AddComponent<Image>();
        closeImg.color = new Color(0.8f, 0.2f, 0.2f, 1f);

        Button closeBtn = closeBtnObj.AddComponent<Button>();
        closeBtn.onClick.AddListener(ToggleInfoPanel);

        ColorBlock cb = closeBtn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.2f, 1f, 1f, 1f);
        cb.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        closeBtn.colors = cb;

        RectTransform closeRt = closeBtnObj.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(1f, 1f);
        closeRt.anchorMax = new Vector2(1f, 1f);
        closeRt.pivot = new Vector2(1f, 1f);
        closeRt.sizeDelta = new Vector2(36f, 36f);
        closeRt.anchoredPosition = new Vector2(-6f, -6f);

        // X label
        GameObject xObj = new GameObject("XLabel");
        xObj.transform.SetParent(closeBtnObj.transform, false);

        TextMeshProUGUI xText = xObj.AddComponent<TextMeshProUGUI>();
        xText.text = "X";
        xText.color = Color.white;
        xText.fontSize = 20f;
        xText.fontStyle = FontStyles.Bold;
        xText.alignment = TextAlignmentOptions.Center;
        xText.raycastTarget = false;

        RectTransform xRt = xObj.GetComponent<RectTransform>();
        xRt.anchorMin = Vector2.zero;
        xRt.anchorMax = Vector2.one;
        xRt.sizeDelta = Vector2.zero;
        xRt.anchoredPosition = Vector2.zero;
    }

    // ═══════════════════════════════════════════
    //  TOGGLE
    // ═══════════════════════════════════════════

    private void ToggleInfoPanel()
    {
        if (infoPanelObj == null) return;

        isPanelOpen = !isPanelOpen;
        infoPanelObj.SetActive(isPanelOpen);

        // Bring to front so it renders above all other UI
        if (isPanelOpen)
            infoPanelObj.transform.SetAsLastSibling();
    }

    private void ApplyBackground()
    {
        if (boxImage == null) return;

        if (backgroundImage != null)
        {
            boxImage.sprite = backgroundImage;
            boxImage.type = Image.Type.Sliced;
            boxImage.color = new Color(1f, 1f, 1f, imageAlpha);
        }
        else
        {
            boxImage.sprite = null;
            boxImage.color = boxColor;
        }
    }
}
