using UnityEngine;
using UnityEngine.UI;

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
        if (InputManager.Instance.GetEnterDown())
        {
            OnStartGame();
        }

        if (InputManager.Instance.GetEscapeDown())
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
