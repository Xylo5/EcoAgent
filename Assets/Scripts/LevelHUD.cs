using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
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
    public RequiredBuildingsUI requiredBuildingsUI;

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

        bool allBuildings = true;
        if (requiredBuildingsUI != null)
            allBuildings = requiredBuildingsUI.AreAllRequirementsMet();

        ResultData.PollutionScore = pollution;
        ResultData.AllBuildingsPlaced = allBuildings;

        // Auto-detect level index from scene name (e.g. "Level_3" → index 2)
        string sceneName = SceneManager.GetActiveScene().name;
        int levelIndex = 0;
        if (sceneName.StartsWith("Level_"))
        {
            int.TryParse(sceneName.Substring(6), out int sceneNum);
            levelIndex = sceneNum - 1; // Level_1 = index 0, Level_2 = index 1, etc.
        }
        ResultData.LevelIndex = levelIndex;

        // --- Evaluate result ---
        if (!allBuildings)
        {
            // Fail: missing required buildings
            ResultData.Won = false;
            ResultData.LeafRating = 0;
            ResultData.ResultMessage = "Not all required buildings placed.";
        }
        else if (pollution > 120)
        {
            // Fail: AQI too high
            ResultData.Won = false;
            ResultData.LeafRating = 0;
            ResultData.ResultMessage = "AQI is too high!";
        }
        else if (pollution >= 71)
        {
            // Pass: 1 leaf (71–120)
            ResultData.Won = true;
            ResultData.LeafRating = 1;
            ResultData.ResultMessage = "Pollution can be reduced";
        }
        else if (pollution >= 41)
        {
            // Pass: 2 leaves (41–70)
            ResultData.Won = true;
            ResultData.LeafRating = 2;
            ResultData.ResultMessage = "Good Efforts";
        }
        else
        {
            // Pass: 3 leaves (0–40)
            ResultData.Won = true;
            ResultData.LeafRating = 3;
            ResultData.ResultMessage = "Excellent Management";
        }

        SceneLoader.LoadResult();
    }
}
