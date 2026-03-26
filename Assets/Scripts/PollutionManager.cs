using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tracks total pollution (AQI) in the level.
/// Starts at 100 and updates whenever a building is placed.
/// Positive pollutionValue on a building increases the counter,
/// negative pollutionValue decreases it. Clamped to minimum 0.
///
/// AQI Color Ranges:
///   0–40  → Green
///   41–70 → Yellow
///   71–120 → Orange
///   121+  → Red
/// </summary>
public class PollutionManager : MonoBehaviour
{
    public static PollutionManager Instance { get; private set; }

    [Header("Settings")]
    public int startingPollution = 100;

    [Header("UI")]
    public TextMeshProUGUI pollutionText;

    private int currentPollution;
    private Image bgImage;

    void Awake()
    {
        Instance = this;
        currentPollution = Mathf.Max(0, startingPollution);
    }

    void Start()
    {
        // Force the text to top-center of the screen
        if (pollutionText != null)
        {
            // Create an opaque background panel behind the text
            GameObject bgObj = new GameObject("PollutionBG");
            bgObj.transform.SetParent(pollutionText.transform.parent, false);

            bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.14f, 0.85f);

            RectTransform bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0.5f, 1f);
            bgRt.anchorMax = new Vector2(0.5f, 1f);
            bgRt.pivot = new Vector2(0.5f, 1f);
            bgRt.anchoredPosition = new Vector2(0f, -6f);
            bgRt.sizeDelta = new Vector2(320f, 50f);

            // Re-parent the text under the background so it renders on top
            pollutionText.transform.SetParent(bgObj.transform, false);
            RectTransform rt = pollutionText.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            pollutionText.alignment = TextAlignmentOptions.Center;
            pollutionText.fontSize = 28;
        }
        UpdateUI();
    }

    public void AddPollution(int amount)
    {
        currentPollution = Mathf.Max(0, currentPollution + amount);
        UpdateUI();
    }

    public int GetPollution()
    {
        return currentPollution;
    }

    /// <summary>
    /// Returns the AQI tier color for a given pollution value.
    /// </summary>
    public static Color GetAQIColor(int aqi)
    {
        if (aqi <= 40)
            return new Color(0.2f, 0.85f, 0.3f);      // Green
        else if (aqi <= 70)
            return new Color(1f, 0.85f, 0f);            // Yellow
        else if (aqi <= 120)
            return new Color(1f, 0.55f, 0.1f);           // Orange
        else
            return new Color(1f, 0.2f, 0.2f);            // Red
    }

    private void UpdateUI()
    {
        if (pollutionText != null)
        {
            pollutionText.text = "AQI: " + currentPollution;

            Color tierColor = GetAQIColor(currentPollution);
            pollutionText.color = tierColor;

            // Tint the background panel to a subtle version of the tier color
            if (bgImage != null)
            {
                bgImage.color = new Color(
                    tierColor.r * 0.15f,
                    tierColor.g * 0.15f,
                    tierColor.b * 0.15f,
                    0.85f
                );
            }
        }
    }
}
