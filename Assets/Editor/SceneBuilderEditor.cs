using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Editor utility: Tools → Build All Scenes
/// Creates MainMenu and LevelSelect scenes, renames SampleScene to Level_0,
/// wires up all UI, and registers scenes in Build Settings.
/// </summary>
public class SceneBuilderEditor : EditorWindow
{
    [MenuItem("Tools/Build All Scenes")]
    public static void BuildAllScenes()
    {
        // Save any open scene first
        EditorSceneManager.SaveOpenScenes();

        // --- Step 1: Rename SampleScene → Level_0 ---
        string scenesFolder = "Assets/Scenes";
        string oldScenePath = scenesFolder + "/SampleScene.unity";
        string newScenePath = scenesFolder + "/Level_0.unity";

        if (File.Exists(oldScenePath) && !File.Exists(newScenePath))
        {
            AssetDatabase.RenameAsset(oldScenePath, "Level_0");
            Debug.Log("[SceneBuilder] Renamed SampleScene → Level_0");
        }
        else if (!File.Exists(oldScenePath) && !File.Exists(newScenePath))
        {
            Debug.LogWarning("[SceneBuilder] SampleScene.unity not found and Level_0.unity doesn't exist either.");
        }

        // --- Step 2: Create MainMenu scene ---
        BuildMainMenuScene(scenesFolder + "/MainMenu.unity");

        // --- Step 3: Create LevelSelect scene ---
        BuildLevelSelectScene(scenesFolder + "/LevelSelect.unity");

        // --- Step 4: Add LevelHUD to Level_0 ---
        AddLevelHUDToLevel(newScenePath);

        // --- Step 5: Register all scenes in Build Settings ---
        RegisterBuildScenes(scenesFolder);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // --- Step 6: Open only MainMenu so Play starts there ---
        EditorSceneManager.OpenScene(scenesFolder + "/MainMenu.unity", OpenSceneMode.Single);
        Debug.Log("[SceneBuilder] ✓ All scenes built and registered! MainMenu is now the active scene.");
    }

    // ─────────────────────────────────────────────────
    //  MAIN MENU
    // ─────────────────────────────────────────────────
    static void BuildMainMenuScene(string path)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Camera background
        Camera cam = Object.FindFirstObjectByType<Camera>();
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.12f, 0.10f); // dark green-teal
        }

        // ── Canvas ──
        GameObject canvasGO = CreateCanvas("MainMenuCanvas");
        Canvas canvas = canvasGO.GetComponent<Canvas>();

        // ── Background panel ──
        GameObject bg = CreatePanel(canvasGO.transform, "Background",
            new Color(0.06f, 0.15f, 0.12f, 0.95f));
        SetAnchorsStretch(bg.GetComponent<RectTransform>());

        // ── Title ──
        GameObject titleGO = CreateTMPText(canvasGO.transform, "TitleText",
            "EcoAgent", 72, new Color(0.30f, 0.85f, 0.50f),
            TextAlignmentOptions.Center, FontStyles.Bold);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.1f, 0.60f);
        titleRT.anchorMax = new Vector2(0.9f, 0.85f);
        titleRT.offsetMin = Vector2.zero;
        titleRT.offsetMax = Vector2.zero;

        // ── Subtitle ──
        GameObject subtitleGO = CreateTMPText(canvasGO.transform, "SubtitleText",
            "Build · Manage · Grow", 28, new Color(0.55f, 0.75f, 0.60f),
            TextAlignmentOptions.Center, FontStyles.Italic);
        RectTransform subRT = subtitleGO.GetComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0.2f, 0.52f);
        subRT.anchorMax = new Vector2(0.8f, 0.60f);
        subRT.offsetMin = Vector2.zero;
        subRT.offsetMax = Vector2.zero;

        // ── Start Button ──
        GameObject startBtn = CreateStyledButton(canvasGO.transform, "StartButton",
            "START GAME",
            new Color(0.15f, 0.55f, 0.30f),
            new Color(0.95f, 0.97f, 0.95f),
            36);
        RectTransform startRT = startBtn.GetComponent<RectTransform>();
        startRT.anchorMin = new Vector2(0.30f, 0.30f);
        startRT.anchorMax = new Vector2(0.70f, 0.42f);
        startRT.offsetMin = Vector2.zero;
        startRT.offsetMax = Vector2.zero;

        // ── Quit Button ──
        GameObject quitBtn = CreateStyledButton(canvasGO.transform, "QuitButton",
            "QUIT",
            new Color(0.45f, 0.15f, 0.15f),
            new Color(0.90f, 0.85f, 0.85f),
            28);
        RectTransform quitRT = quitBtn.GetComponent<RectTransform>();
        quitRT.anchorMin = new Vector2(0.35f, 0.16f);
        quitRT.anchorMax = new Vector2(0.65f, 0.26f);
        quitRT.offsetMin = Vector2.zero;
        quitRT.offsetMax = Vector2.zero;

        // ── Hint text ──
        GameObject hintGO = CreateTMPText(canvasGO.transform, "HintText",
            "Press ENTER to start  ·  ESC to quit", 18,
            new Color(0.50f, 0.60f, 0.50f, 0.7f),
            TextAlignmentOptions.Center, FontStyles.Normal);
        RectTransform hintRT = hintGO.GetComponent<RectTransform>();
        hintRT.anchorMin = new Vector2(0.2f, 0.05f);
        hintRT.anchorMax = new Vector2(0.8f, 0.12f);
        hintRT.offsetMin = Vector2.zero;
        hintRT.offsetMax = Vector2.zero;

        // ── MainMenuUI component ──
        MainMenuUI menuUI = canvasGO.AddComponent<MainMenuUI>();
        menuUI.startButton = startBtn.GetComponent<Button>();
        menuUI.quitButton  = quitBtn.GetComponent<Button>();

        // ── EventSystem ──
        CreateEventSystem();

        EditorSceneManager.SaveScene(scene, path);
        Debug.Log("[SceneBuilder] Created MainMenu scene at " + path);
    }

    // ─────────────────────────────────────────────────
    //  LEVEL SELECT
    // ─────────────────────────────────────────────────
    static void BuildLevelSelectScene(string path)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        Camera cam = Object.FindFirstObjectByType<Camera>();
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.10f, 0.16f); // dark blue-teal
        }

        // ── Canvas ──
        GameObject canvasGO = CreateCanvas("LevelSelectCanvas");

        // ── Background ──
        GameObject bg = CreatePanel(canvasGO.transform, "Background",
            new Color(0.07f, 0.12f, 0.18f, 0.95f));
        SetAnchorsStretch(bg.GetComponent<RectTransform>());

        // ── Title ──
        GameObject titleGO = CreateTMPText(canvasGO.transform, "TitleText",
            "SELECT LEVEL", 56, new Color(0.45f, 0.75f, 0.95f),
            TextAlignmentOptions.Center, FontStyles.Bold);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.1f, 0.82f);
        titleRT.anchorMax = new Vector2(0.9f, 0.95f);
        titleRT.offsetMin = Vector2.zero;
        titleRT.offsetMax = Vector2.zero;

        // ── Level buttons container ──
        GameObject container = new GameObject("LevelButtonContainer");
        container.transform.SetParent(canvasGO.transform, false);
        RectTransform containerRT = container.AddComponent<RectTransform>();
        containerRT.anchorMin = new Vector2(0.10f, 0.30f);
        containerRT.anchorMax = new Vector2(0.90f, 0.78f);
        containerRT.offsetMin = Vector2.zero;
        containerRT.offsetMax = Vector2.zero;

        // Grid layout
        GridLayoutGroup grid = container.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(200, 200);
        grid.spacing = new Vector2(30, 30);
        grid.childAlignment = TextAnchor.MiddleCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;

        // Create 5 level buttons
        Color enabledCol  = new Color(0.15f, 0.55f, 0.30f);
        Color disabledCol = new Color(0.22f, 0.22f, 0.25f);

        Button[] levelButtons = new Button[5];
        for (int i = 0; i < 5; i++)
        {
            bool unlocked = (i == 0);
            string label = unlocked ? "Level " + (i + 1) : "Level " + (i + 1) + "\n(Locked)";
            Color col = unlocked ? enabledCol : disabledCol;
            Color textCol = unlocked
                ? new Color(0.95f, 0.97f, 0.95f)
                : new Color(0.50f, 0.50f, 0.52f);

            GameObject btn = CreateStyledButton(container.transform,
                "LevelBtn_" + i, label, col, textCol, 26);
            levelButtons[i] = btn.GetComponent<Button>();
        }

        // ── Back Button ──
        GameObject backBtn = CreateStyledButton(canvasGO.transform, "BackButton",
            "← BACK",
            new Color(0.50f, 0.25f, 0.20f),
            new Color(0.90f, 0.85f, 0.85f),
            26);
        RectTransform backRT = backBtn.GetComponent<RectTransform>();
        backRT.anchorMin = new Vector2(0.02f, 0.02f);
        backRT.anchorMax = new Vector2(0.18f, 0.10f);
        backRT.offsetMin = Vector2.zero;
        backRT.offsetMax = Vector2.zero;

        // ── Hint Text ──
        GameObject hintGO = CreateTMPText(canvasGO.transform, "HintText",
            "Arrow keys to navigate  ·  ENTER to select  ·  ESC to go back", 16,
            new Color(0.50f, 0.55f, 0.65f, 0.7f),
            TextAlignmentOptions.Center, FontStyles.Normal);
        RectTransform hintRT = hintGO.GetComponent<RectTransform>();
        hintRT.anchorMin = new Vector2(0.15f, 0.14f);
        hintRT.anchorMax = new Vector2(0.85f, 0.22f);
        hintRT.offsetMin = Vector2.zero;
        hintRT.offsetMax = Vector2.zero;

        // ── LevelSelectUI component ──
        LevelSelectUI selectUI = canvasGO.AddComponent<LevelSelectUI>();
        selectUI.levelButtons = levelButtons;
        selectUI.backButton   = backBtn.GetComponent<Button>();

        // ── EventSystem ──
        CreateEventSystem();

        EditorSceneManager.SaveScene(scene, path);
        Debug.Log("[SceneBuilder] Created LevelSelect scene at " + path);
    }

    // ─────────────────────────────────────────────────
    //  ADD LEVEL HUD TO EXISTING LEVEL
    // ─────────────────────────────────────────────────
    static void AddLevelHUDToLevel(string scenePath)
    {
        if (!File.Exists(scenePath))
        {
            Debug.LogWarning("[SceneBuilder] Level scene not found at " + scenePath + ". Skipping HUD addition.");
            return;
        }

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Check if LevelHUD already exists
        if (Object.FindFirstObjectByType<LevelHUD>() != null)
        {
            Debug.Log("[SceneBuilder] LevelHUD already exists in scene. Skipping.");
            EditorSceneManager.SaveScene(scene);
            return;
        }

        // ── HUD Canvas ──
        GameObject canvasGO = CreateCanvas("LevelHUDCanvas");

        // ── Back button (top-left corner) ──
        GameObject backBtn = CreateStyledButton(canvasGO.transform, "BackToMenuButton",
            "← Menu",
            new Color(0.35f, 0.35f, 0.35f, 0.75f),
            new Color(0.90f, 0.90f, 0.90f),
            20);
        RectTransform backRT = backBtn.GetComponent<RectTransform>();
        backRT.anchorMin = new Vector2(0.01f, 0.91f);
        backRT.anchorMax = new Vector2(0.12f, 0.99f);
        backRT.offsetMin = Vector2.zero;
        backRT.offsetMax = Vector2.zero;

        // ── LevelHUD component ──
        LevelHUD hud = canvasGO.AddComponent<LevelHUD>();
        hud.backButton = backBtn.GetComponent<Button>();

        // ── EventSystem (only if none exists) ──
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            CreateEventSystem();

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SceneBuilder] Added LevelHUD to " + scenePath);
    }

    // ─────────────────────────────────────────────────
    //  BUILD SETTINGS REGISTRATION
    // ─────────────────────────────────────────────────
    static void RegisterBuildScenes(string scenesFolder)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
        scenes.Add(new EditorBuildSettingsScene(scenesFolder + "/MainMenu.unity",   true));
        scenes.Add(new EditorBuildSettingsScene(scenesFolder + "/LevelSelect.unity", true));
        scenes.Add(new EditorBuildSettingsScene(scenesFolder + "/Level_0.unity",     true));
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("[SceneBuilder] Registered 3 scenes in Build Settings.");
    }

    // ═════════════════════════════════════════════════
    //  HELPER METHODS
    // ═════════════════════════════════════════════════

    static GameObject CreateCanvas(string name)
    {
        GameObject go = new GameObject(name);
        Canvas canvas       = go.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode  = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    static void SetAnchorsStretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static GameObject CreateTMPText(Transform parent, string name, string text,
        int fontSize, Color color, TextAlignmentOptions alignment, FontStyles style)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = color;
        tmp.alignment = alignment;
        tmp.fontStyle = style;
        tmp.enableAutoSizing = false;

        return go;
    }

    static GameObject CreateStyledButton(Transform parent, string name,
        string label, Color bgColor, Color textColor, int fontSize)
    {
        // Button root
        GameObject btnGO = new GameObject(name);
        btnGO.transform.SetParent(parent, false);
        RectTransform rt = btnGO.AddComponent<RectTransform>();

        Image img   = btnGO.AddComponent<Image>();
        img.color   = bgColor;
        img.type    = Image.Type.Sliced;

        Button btn  = btnGO.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
        cb.pressedColor     = new Color(0.8f, 0.8f, 0.8f, 1f);
        btn.colors = cb;

        // Label
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(btnGO.transform, false);
        RectTransform labelRT = labelGO.AddComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = new Vector2(8, 4);
        labelRT.offsetMax = new Vector2(-8, -4);

        TextMeshProUGUI tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = fontSize;
        tmp.color     = textColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;

        return btnGO;
    }

    static void CreateEventSystem()
    {
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null)
            return;

        GameObject esGO = new GameObject("EventSystem");
        esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }
}
