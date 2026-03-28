using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

/// <summary>
/// Result screen shown after checking AQI.
/// Builds the entire UI at runtime — no manual scene setup needed.
/// Reads ResultData static fields set before scene load.
///
/// Shows: Title, AQI value (color-coded), leaf rating (1–3 🍃), 
/// result message, and navigation buttons.
/// </summary>
public class ResultUI : MonoBehaviour
{
    [Header("Make A Change Links")]
    public string directActionUrl = "https://pgportal.gov.in/";
    public string raiseAwarenessUrl = "https://www.change.org/search?q=pollution%20india";
    void Start()
    {
        BuildUI();
    }

    void Update()
    {
        if (InputManager.Instance.GetEscapeDown())
            SceneLoader.LoadLevelSelect();
    }

    private void BuildUI()
    {
        bool won = ResultData.Won;
        int aqi = ResultData.PollutionScore;
        int leaves = ResultData.LeafRating;
        string message = ResultData.ResultMessage ?? "";

        // ── Canvas ──
        GameObject canvasObj = new GameObject("ResultCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        // ── EventSystem ──
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<InputSystemUIInputModule>();
        }

        // ── Full-screen background ──
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bg = bgObj.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.06f, 0.10f, 1f);
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.sizeDelta = Vector2.zero;

        // ── Center panel ──
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(canvasObj.transform, false);
        Image panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.10f, 0.10f, 0.16f, 0.92f);
        RectTransform panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(650, 520);
        panelRt.anchoredPosition = new Vector2(0, 80);

        // ── Title ──
        Color titleColor = won
            ? new Color(0.2f, 0.9f, 0.35f)
            : new Color(1f, 0.25f, 0.25f);
        string titleText = won ? "LEVEL COMPLETE" : "LEVEL FAILED";
        CreateText(panel.transform, "TitleText", titleText, titleColor,
            52, FontStyles.Bold, new Vector2(0, 190));

        // ── Divider line ──
        GameObject divider = new GameObject("Divider");
        divider.transform.SetParent(panel.transform, false);
        Image divImg = divider.AddComponent<Image>();
        divImg.color = new Color(1f, 1f, 1f, 0.12f);
        RectTransform divRt = divider.GetComponent<RectTransform>();
        divRt.anchorMin = new Vector2(0.5f, 0.5f);
        divRt.anchorMax = new Vector2(0.5f, 0.5f);
        divRt.sizeDelta = new Vector2(500, 2);
        divRt.anchoredPosition = new Vector2(0, 148);

        // ── AQI Label ──
        CreateText(panel.transform, "AQILabel", "AIR QUALITY INDEX",
            new Color(0.6f, 0.6f, 0.7f), 20, FontStyles.Normal, new Vector2(0, 115));

        // ── AQI Value (large, color-coded) ──
        Color aqiColor = PollutionManager.GetAQIColor(aqi);
        CreateText(panel.transform, "AQIValue", aqi.ToString(),
            aqiColor, 72, FontStyles.Bold, new Vector2(0, 68));

        // ── AQI Tier Badge ──
        string tierLabel = GetTierLabel(aqi);
        CreateText(panel.transform, "TierBadge", tierLabel,
            aqiColor, 22, FontStyles.Bold, new Vector2(0, 28));

        // ── Leaf Rating Row ──
        if (won && leaves > 0)
        {
            CreateLeafDisplay(panel.transform, leaves, new Vector2(0, -20));
        }

        // ── Result Message ──
        Color msgColor = won ? new Color(0.8f, 0.9f, 0.8f) : new Color(0.85f, 0.65f, 0.65f);
        CreateText(panel.transform, "ResultMessage", message,
            msgColor, 26, FontStyles.Italic, new Vector2(0, -75));

        // ── Buttons ──
        CreateButton(panel.transform, "LevelSelectBtn", "LEVEL SELECT",
            new Vector2(-140, -170), new Color(0.18f, 0.40f, 0.70f),
            () => SceneLoader.LoadLevelSelect());

        CreateButton(panel.transform, "RetryBtn", "RETRY",
            new Vector2(140, -170), new Color(0.18f, 0.60f, 0.35f),
            () => SceneLoader.LoadLevel(ResultData.LevelIndex));

        // ── Hint text ──
        CreateText(panel.transform, "HintText", "ESC to go back",
            new Color(0.45f, 0.45f, 0.55f, 0.6f), 16, FontStyles.Normal,
            new Vector2(0, -225));

        // ── "Make A Change" Box (separate panel below) ──
        BuildMakeAChangeBox(canvasObj.transform);
    }

    private void BuildMakeAChangeBox(Transform canvasTransform)
    {
        // ── Container panel ──
        GameObject box = new GameObject("MakeAChangeBox");
        box.transform.SetParent(canvasTransform, false);
        Image boxImg = box.AddComponent<Image>();
        boxImg.color = new Color(0.10f, 0.10f, 0.16f, 0.92f);
        RectTransform boxRt = box.GetComponent<RectTransform>();
        boxRt.anchorMin = new Vector2(0.5f, 0.5f);
        boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        boxRt.sizeDelta = new Vector2(650, 130);
        boxRt.anchoredPosition = new Vector2(0, -230);

        // ── Heading ──
        CreateText(box.transform, "MakeAChangeTitle", "MAKE A CHANGE",
            new Color(0.95f, 0.85f, 0.35f), 30, FontStyles.Bold,
            new Vector2(0, 28));

        // ── Link buttons row ──
        CreateLinkButton(box.transform, "DirectActionLink", "Direct Action",
            new Vector2(-140, -30), new Color(0.30f, 0.75f, 0.95f),
            directActionUrl);

        CreateLinkButton(box.transform, "RaiseAwarenessLink", "Raise Awareness",
            new Vector2(140, -30), new Color(0.30f, 0.75f, 0.95f),
            raiseAwarenessUrl);
    }

    private void CreateLinkButton(Transform parent, string name, string label,
        Vector2 position, Color textColor, string url)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        // Invisible background for click area
        Image bg = obj.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0f);

        Button btn = obj.AddComponent<Button>();
        btn.onClick.AddListener(() => Application.OpenURL(url));

        // No color tint on the invisible bg
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = Color.white;
        cb.pressedColor = Color.white;
        btn.colors = cb;

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(220, 40);
        rt.anchoredPosition = position;

        // Link-style text with underline
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(obj.transform, false);
        TextMeshProUGUI tmp = labelObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "<u>" + label + "</u>";
        tmp.color = textColor;
        tmp.fontSize = 22;
        tmp.fontStyle = FontStyles.Normal;
        tmp.alignment = TextAlignmentOptions.Center;
        RectTransform labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.sizeDelta = Vector2.zero;
    }

    private string GetTierLabel(int aqi)
    {
        if (aqi <= 40) return "EXCELLENT";
        if (aqi <= 70) return "GOOD";
        if (aqi <= 120) return "MODERATE";
        return "POOR";
    }

    private void CreateLeafDisplay(Transform parent, int filledCount, Vector2 position)
    {
        GameObject container = new GameObject("LeafRating");
        container.transform.SetParent(parent, false);
        RectTransform containerRt = container.AddComponent<RectTransform>();
        containerRt.anchorMin = new Vector2(0.5f, 0.5f);
        containerRt.anchorMax = new Vector2(0.5f, 0.5f);
        containerRt.sizeDelta = new Vector2(300, 60);
        containerRt.anchoredPosition = position;

        float spacing = 60f;
        float startX = -spacing;

        for (int i = 0; i < 3; i++)
        {
            bool filled = (i < filledCount);

            // Outer leaf shape (rotated 45° diamond)
            GameObject leafObj = new GameObject("Leaf_" + (i + 1));
            leafObj.transform.SetParent(container.transform, false);

            Image leafImg = leafObj.AddComponent<Image>();
            leafImg.color = filled
                ? new Color(0.25f, 0.90f, 0.40f)  // Bright green
                : new Color(0.25f, 0.25f, 0.30f);  // Dim gray

            RectTransform rt = leafObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(30, 30);
            rt.anchoredPosition = new Vector2(startX + i * spacing, 0);
            rt.localRotation = UnityEngine.Quaternion.Euler(0, 0, 45); // Diamond shape

            // Inner dot for visual detail
            GameObject dotObj = new GameObject("Dot");
            dotObj.transform.SetParent(leafObj.transform, false);
            Image dotImg = dotObj.AddComponent<Image>();
            dotImg.color = filled
                ? new Color(0.15f, 0.70f, 0.25f)  // Darker green center
                : new Color(0.18f, 0.18f, 0.22f);  // Darker gray center

            RectTransform dotRt = dotObj.GetComponent<RectTransform>();
            dotRt.anchorMin = new Vector2(0.5f, 0.5f);
            dotRt.anchorMax = new Vector2(0.5f, 0.5f);
            dotRt.sizeDelta = new Vector2(12, 12);
            dotRt.anchoredPosition = Vector2.zero;
        }
    }

    private void CreateText(Transform parent, string name, string content,
        Color color, int fontSize, FontStyles style, Vector2 position)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.color = color;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(550, 70);
        rt.anchoredPosition = position;
    }

    private void CreateButton(Transform parent, string name, string label,
        Vector2 position, Color bgColor, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        Image img = btnObj.AddComponent<Image>();
        img.color = bgColor;
        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(onClick);

        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        btn.colors = cb;

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(220, 55);
        rt.anchoredPosition = position;

        // Button label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI tmp = labelObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.color = Color.white;
        tmp.fontSize = 24;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        RectTransform labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.sizeDelta = Vector2.zero;
    }
}
