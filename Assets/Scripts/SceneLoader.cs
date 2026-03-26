using UnityEngine.SceneManagement;

/// <summary>
/// Static utility for navigating between scenes.
/// Scene names must match exactly: MainMenu, LevelSelect, Level_1, Level_2, …
/// </summary>
public static class SceneLoader
{
    public static void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public static void LoadLevelSelect()
    {
        SceneManager.LoadScene("LevelSelect");
    }

    /// <summary>
    /// Loads a gameplay level by index (e.g. 0 → "Level_1").
    /// </summary>
    public static void LoadLevel(int levelIndex)
    {
        SceneManager.LoadScene("Level_" + (levelIndex + 1));
    }

    public static void LoadResult()
    {
        SceneManager.LoadScene("Result");
    }
}
