using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Singleton manager for handling all input.
/// Caches device references per-frame for efficiency.
/// </summary>
public class InputManager : MonoBehaviour
{
    private static InputManager _instance;
    public static InputManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<InputManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("InputManager");
                    _instance = go.AddComponent<InputManager>();
                }
            }
            return _instance;
        }
    }

    [Header("Mouse Settings")]
    [Tooltip("Sensitivity multiplier for panning with holding middle mouse button.")]
    public float mousePanSensitivity = 1.0f;
    [Tooltip("Sensitivity multiplier for zooming with scroll wheel.")]
    public float mouseZoomSensitivity = 10.0f;
    [Tooltip("Sensitivity multiplier for rotating with right mouse button.")]
    public float mouseRotateSensitivity = 0.5f;
    [Tooltip("If true, dragging the mouse pans the camera in the opposite direction (like grabbing).")]
    public bool invertMousePan = true;

    // Cached device references — updated once per frame
    private Keyboard kb;
    private Mouse mouse;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        // Initialize device refs immediately so they're available on frame 0
        kb = Keyboard.current;
        mouse = Mouse.current;
    }

    private void Update()
    {
        kb = Keyboard.current;
        mouse = Mouse.current;
    }

    // ═══════════════════════════════════════════
    //  CAMERA INPUTS
    // ═══════════════════════════════════════════

    public Vector2 GetPanInput()
    {
        Vector2 pan = Vector2.zero;

        if (kb != null)
        {
            if (kb.dKey.isPressed) pan.x += 1f;
            if (kb.aKey.isPressed) pan.x -= 1f;
            if (kb.wKey.isPressed) pan.y += 1f;
            if (kb.sKey.isPressed) pan.y -= 1f;
        }

        if (mouse != null && mouse.middleButton.isPressed)
        {
            Vector2 delta = mouse.delta.ReadValue() * mousePanSensitivity;
            pan += invertMousePan ? -delta : delta;
        }

        return pan;
    }

    public float GetZoomInput()
    {
        float zoom = 0f;

        if (kb != null)
        {
            if (kb.zKey.isPressed) zoom -= 1f;
            if (kb.xKey.isPressed) zoom += 1f;
        }

        if (mouse != null)
        {
            float scrollY = mouse.scroll.y.ReadValue();
            if (scrollY != 0f)
                zoom -= scrollY * mouseZoomSensitivity;
        }

        return zoom;
    }

    public float GetRotateInput()
    {
        float rot = 0f;

        if (kb != null)
        {
            if (kb.eKey.isPressed) rot += 1f;
            if (kb.qKey.isPressed) rot -= 1f;
        }

        if (mouse != null && mouse.rightButton.isPressed)
            rot += mouse.delta.x.ReadValue() * mouseRotateSensitivity;

        return rot;
    }

    // ═══════════════════════════════════════════
    //  ACTION INPUTS
    // ═══════════════════════════════════════════

    public bool GetEnterDown()
    {
        return (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
            || (mouse != null && mouse.leftButton.wasPressedThisFrame);
    }

    /// <summary>
    /// Returns true if the Enter key was pressed this frame (keyboard only, no mouse click).
    /// Use this in UI menus to avoid left-click conflicting with button EventTriggers.
    /// </summary>
    public bool GetEnterKeyDown()
    {
        return kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame);
    }

    public bool GetEnterHeld()
    {
        return (kb != null && (kb.enterKey.isPressed || kb.numpadEnterKey.isPressed))
            || (mouse != null && mouse.leftButton.isPressed);
    }

    public bool GetEscapeDown() => kb != null && kb.escapeKey.wasPressedThisFrame;
    public bool GetTabDown()    => kb != null && kb.tabKey.wasPressedThisFrame;
    public bool GetShiftHeld()  => kb != null && kb.leftShiftKey.isPressed;

    // ═══════════════════════════════════════════
    //  MOUSE POSITION
    // ═══════════════════════════════════════════

    public Vector2 GetMousePosition()
    {
        return mouse != null ? mouse.position.ReadValue() : Vector2.zero;
    }

    // ═══════════════════════════════════════════
    //  ARROW KEYS
    // ═══════════════════════════════════════════

    public bool GetUpArrowDown()    => kb != null && kb.upArrowKey.wasPressedThisFrame;
    public bool GetDownArrowDown()  => kb != null && kb.downArrowKey.wasPressedThisFrame;
    public bool GetLeftArrowDown()  => kb != null && kb.leftArrowKey.wasPressedThisFrame;
    public bool GetRightArrowDown() => kb != null && kb.rightArrowKey.wasPressedThisFrame;

    public bool GetUpArrowHeld()    => kb != null && kb.upArrowKey.isPressed;
    public bool GetDownArrowHeld()  => kb != null && kb.downArrowKey.isPressed;
    public bool GetLeftArrowHeld()  => kb != null && kb.leftArrowKey.isPressed;
    public bool GetRightArrowHeld() => kb != null && kb.rightArrowKey.isPressed;
}
