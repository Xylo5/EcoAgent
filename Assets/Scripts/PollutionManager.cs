using UnityEngine;
using UnityEngine.UI;
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
            // Create an opaque background panel behind the text
            GameObject bgObj = new GameObject("PollutionBG");
            bgObj.transform.SetParent(pollutionText.transform.parent, false);

            Image bgImage = bgObj.AddComponent<Image>();
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
