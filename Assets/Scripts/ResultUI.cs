using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

/// <summary>
/// Result screen shown after checking pollution.
/// Builds the entire UI at runtime — no manual scene setup needed.
/// Reads ResultData static fields set before scene load.
/// </summary>
public class ResultUI : MonoBehaviour
{
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
        // Canvas
        GameObject canvasObj = new GameObject("ResultCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // EventSystem (required for button clicks)
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<InputSystemUIInputModule>();
        }

        // Background overlay
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bg = bgObj.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.12f, 1f);
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.sizeDelta = Vector2.zero;

        // Center panel
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(canvasObj.transform, false);
        RectTransform panelRt = panel.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(600, 400);
        panelRt.anchoredPosition = Vector2.zero;

        // Win/Lose title
        bool won = ResultData.Won;
        CreateText(panel.transform, "TitleText",
            won ? "YOU WIN!" : "YOU LOSE",
            won ? new Color(0.2f, 0.9f, 0.3f) : new Color(1f, 0.25f, 0.25f),
            60, new Vector2(0, 120));

        // Pollution score
        CreateText(panel.transform, "ScoreText",
            "Pollution: " + ResultData.PollutionScore,
            Color.white, 36, new Vector2(0, 30));

        // Threshold hint
        string hintMsg;
        if (won)
            hintMsg = "Pollution is below 0!";
        else if (!ResultData.AllBuildingsPlaced)
            hintMsg = "Not all required buildings were placed.";
        else
            hintMsg = "Reduce pollution below 0 to win.";

        CreateText(panel.transform, "HintText",
            hintMsg,
            new Color(0.7f, 0.7f, 0.7f), 22, new Vector2(0, -20));

        // Buttons
        CreateButton(panel.transform, "LevelSelectBtn", "Level Select",
            new Vector2(-120, -120), new Color(0.2f, 0.5f, 0.8f),
            () => SceneLoader.LoadLevelSelect());

        CreateButton(panel.transform, "RetryBtn", "Retry",
            new Vector2(120, -120), new Color(0.2f, 0.7f, 0.4f),
            () => SceneLoader.LoadLevel(ResultData.LevelIndex));
    }

    private void CreateText(Transform parent, string name, string content,
        Color color, int fontSize, Vector2 position)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.color = color;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(500, 70);
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

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(200, 55);
        rt.anchoredPosition = position;

        // Button label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI tmp = labelObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.color = Color.white;
        tmp.fontSize = 26;
        tmp.alignment = TextAlignmentOptions.Center;
        RectTransform labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.sizeDelta = Vector2.zero;
    }
}
