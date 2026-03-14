using UnityEngine;
using TMPro;

/// <summary>
/// Tracks total pollution in the level.
/// Starts at 100 and updates whenever a building is placed.
/// Positive pollutionValue on a building increases the counter,
/// negative pollutionValue decreases it.
/// </summary>
public class PollutionManager : MonoBehaviour
{
    public static PollutionManager Instance { get; private set; }

    [Header("Settings")]
    public int startingPollution = 100;

    [Header("UI")]
    public TextMeshProUGUI pollutionText;

    private int currentPollution;

    void Awake()
    {
        Instance = this;
        currentPollution = startingPollution;
    }

    void Start()
    {
        // Force the text to top-center of the screen
        if (pollutionText != null)
        {
            RectTransform rt = pollutionText.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -10f);
            rt.sizeDelta = new Vector2(300f, 50f);
            pollutionText.alignment = TextAlignmentOptions.Center;
            pollutionText.fontSize = 28;
        }
        UpdateUI();
    }

    public void AddPollution(int amount)
    {
        currentPollution += amount;
        UpdateUI();
    }

    public int GetPollution()
    {
        return currentPollution;
    }

    private void UpdateUI()
    {
        if (pollutionText != null)
        {
            pollutionText.text = "Pollution: " + currentPollution;

            if (currentPollution < 0)
                pollutionText.color = new Color(0.2f, 0.8f, 0.2f); // Green
            else if (currentPollution < 50)
                pollutionText.color = new Color(1f, 0.8f, 0f);     // Yellow
            else
                pollutionText.color = new Color(1f, 0.2f, 0.2f);   // Red
        }
    }
}
