using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Small in-game HUD overlay for a gameplay level.
/// Provides a "Back to Menu" button and Escape key shortcut.
/// </summary>
public class LevelHUD : MonoBehaviour
{
    [Header("UI References")]
    public Button backButton;

    void Start()
    {
        if (backButton != null)
            backButton.onClick.AddListener(OnBack);
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            OnBack();
        }
    }

    void OnBack()
    {
        SceneLoader.LoadLevelSelect();
    }
}
