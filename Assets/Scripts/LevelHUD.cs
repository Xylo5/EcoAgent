using UnityEngine;
using UnityEngine.UI;

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
        if (InputManager.Instance.GetEscapeDown())
        {
            OnBack();
        }
    }

    void OnBack()
    {
        SceneLoader.LoadLevelSelect();
    }
}
