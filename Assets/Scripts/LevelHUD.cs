using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Small in-game HUD overlay for a gameplay level.
/// Provides a "Back to Menu" button, a "Check" button (bottom-right),
/// and Escape key shortcut.
/// </summary>
public class LevelHUD : MonoBehaviour
{
    [Header("UI References")]
    public Button backButton;

    private Button checkButton;

    void Start()
    {
        if (backButton != null)
            backButton.onClick.AddListener(OnBack);

        CreateCheckButton();
    }

    void Update()
    {
        if (InputManager.Instance.GetEscapeDown())
        {
            OnBack();
        }
    }

    void OnBack()
    {
        SceneLoader.LoadLevelSelect();
    }

    private void CreateCheckButton()
    {
        // Find or create a canvas to host the button
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        // Button object
        GameObject btnObj = new GameObject("CheckButton");
        btnObj.transform.SetParent(canvas.transform, false);
        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.9f, 0.6f, 0.1f);
        checkButton = btnObj.AddComponent<Button>();
        checkButton.onClick.AddListener(OnCheck);

        // Anchor bottom-right
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.sizeDelta = new Vector2(160, 50);
        rt.anchoredPosition = new Vector2(-20, 20);

        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI tmp = labelObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "CHECK";
        tmp.color = Color.white;
        tmp.fontSize = 28;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        RectTransform labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.sizeDelta = Vector2.zero;
    }

    void OnCheck()
    {
        int pollution = 0;
        if (PollutionManager.Instance != null)
            pollution = PollutionManager.Instance.GetPollution();

        ResultData.PollutionScore = pollution;
        ResultData.Won = pollution < 0;
        ResultData.LevelIndex = 0; // Level_0

        SceneLoader.LoadResult();
    }
}
