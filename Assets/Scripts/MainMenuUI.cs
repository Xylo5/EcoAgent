using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Main menu controller.
/// Shows the game title and Start / Quit buttons.
/// Keyboard: Enter = Start, Escape = Quit.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("UI References")]
    public Button startButton;
    public Button quitButton;

    void Start()
    {
        if (startButton != null)
            startButton.onClick.AddListener(OnStartGame);
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuit);
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.enterKey.wasPressedThisFrame ||
            Keyboard.current.numpadEnterKey.wasPressedThisFrame)
        {
            OnStartGame();
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            OnQuit();
        }
    }

    void OnStartGame()
    {
        SceneLoader.LoadLevelSelect();
    }

    void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
