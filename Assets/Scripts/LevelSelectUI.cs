using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Level selection screen.
/// Shows 5 level buttons (only Level 1 is enabled for now).
/// Keyboard: Arrow keys / Tab to navigate, Enter to select, Escape for back.
/// </summary>
public class LevelSelectUI : MonoBehaviour
{
    [Header("UI References")]
    public Button[] levelButtons;   // Assign in inspector or built by editor script
    public Button backButton;

    [Header("Colors")]
    public Color enabledColor   = new Color(0.15f, 0.55f, 0.30f, 1f);
    public Color disabledColor  = new Color(0.30f, 0.30f, 0.30f, 1f);
    public Color highlightColor = new Color(0.20f, 0.75f, 0.45f, 1f);

    private int selectedIndex = 0;
    private int enabledLevelCount = 1; // only Level 1 (index 0) is playable
    private Image[] buttonImages;

    void Start()
    {
        // Cache Image components
        buttonImages = new Image[levelButtons.Length];

        // Wire up buttons
        for (int i = 0; i < levelButtons.Length; i++)
        {
            buttonImages[i] = levelButtons[i].GetComponent<Image>();
            int level = i; // closure capture
            if (i < enabledLevelCount)
            {
                levelButtons[i].onClick.AddListener(() => OnLevelSelected(level));
            }
            else
            {
                levelButtons[i].interactable = false;
            }
        }

        if (backButton != null)
            backButton.onClick.AddListener(OnBack);

        UpdateHighlight();
    }

    void Update()
    {
        // Navigation
        bool moved = false;

        if (InputManager.Instance.GetRightArrowDown() ||
            InputManager.Instance.GetTabDown())
        {
            selectedIndex = (selectedIndex + 1) % levelButtons.Length;
            moved = true;
        }
        else if (InputManager.Instance.GetLeftArrowDown())
        {
            selectedIndex = (selectedIndex - 1 + levelButtons.Length) % levelButtons.Length;
            moved = true;
        }
        else if (InputManager.Instance.GetDownArrowDown())
        {
            // Jump forward by ~columns (assume 5 in a row, wraps)
            selectedIndex = Mathf.Min(selectedIndex + 1, levelButtons.Length - 1);
            moved = true;
        }
        else if (InputManager.Instance.GetUpArrowDown())
        {
            selectedIndex = Mathf.Max(selectedIndex - 1, 0);
            moved = true;
        }

        if (moved) UpdateHighlight();

        // Select (keyboard only — button clicks handled by onClick listeners)
        if (InputManager.Instance.GetEnterKeyDown())
        {
            if (selectedIndex < enabledLevelCount)
                OnLevelSelected(selectedIndex);
        }

        // Back
        if (InputManager.Instance.GetEscapeDown())
        {
            OnBack();
        }
    }

    void UpdateHighlight()
    {
        for (int i = 0; i < levelButtons.Length; i++)
        {
            if (buttonImages[i] == null) continue;

            if (i == selectedIndex)
                buttonImages[i].color = highlightColor;
            else if (i < enabledLevelCount)
                buttonImages[i].color = enabledColor;
            else
                buttonImages[i].color = disabledColor;
        }
    }

    void OnLevelSelected(int index)
    {
        SceneLoader.LoadLevel(index);
    }

    void OnBack()
    {
        SceneLoader.LoadMainMenu();
    }
}
