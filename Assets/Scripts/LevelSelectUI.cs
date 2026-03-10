using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

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

    void Start()
    {
        // Wire up buttons
        for (int i = 0; i < levelButtons.Length; i++)
        {
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
        if (Keyboard.current == null) return;

        // Navigation
        bool moved = false;

        if (Keyboard.current.rightArrowKey.wasPressedThisFrame ||
            Keyboard.current.tabKey.wasPressedThisFrame)
        {
            selectedIndex = (selectedIndex + 1) % levelButtons.Length;
            moved = true;
        }
        else if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            selectedIndex = (selectedIndex - 1 + levelButtons.Length) % levelButtons.Length;
            moved = true;
        }
        else if (Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            // Jump forward by ~columns (assume 5 in a row, wraps)
            selectedIndex = Mathf.Min(selectedIndex + 1, levelButtons.Length - 1);
            moved = true;
        }
        else if (Keyboard.current.upArrowKey.wasPressedThisFrame)
        {
            selectedIndex = Mathf.Max(selectedIndex - 1, 0);
            moved = true;
        }

        if (moved) UpdateHighlight();

        // Select
        if (Keyboard.current.enterKey.wasPressedThisFrame ||
            Keyboard.current.numpadEnterKey.wasPressedThisFrame)
        {
            if (selectedIndex < enabledLevelCount)
                OnLevelSelected(selectedIndex);
        }

        // Back
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            OnBack();
        }
    }

    void UpdateHighlight()
    {
        for (int i = 0; i < levelButtons.Length; i++)
        {
            Image img = levelButtons[i].GetComponent<Image>();
            if (img == null) continue;

            if (i == selectedIndex)
                img.color = highlightColor;
            else if (i < enabledLevelCount)
                img.color = enabledColor;
            else
                img.color = disabledColor;
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
