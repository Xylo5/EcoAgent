using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Singleton manager for handling all input.
/// Caches device references per-frame for efficiency.
/// </summary>
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
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
