using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// RTS-style camera controller.
/// WASD pans on the horizontal plane (no zoom effect).
/// Z/X zoom, Q/E rotate. All keyboard-based.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Movement")]
    public float panSpeed = 20f;

    [Header("Zoom")]
    public float zoomSpeed = 15f;
    public float minY = 10f;
    public float maxY = 80f;

    [Header("Rotation")]
    public float rotationSpeed = 80f;

    [Header("Bounds")]
    public Vector2 panLimitX = new Vector2(-10, 110);
    public Vector2 panLimitZ = new Vector2(-10, 110);

    void Update()
    {
        if (Keyboard.current == null) return;

        Vector3 pos = transform.position;
        Keyboard kb = Keyboard.current;

        // Flatten the forward/right vectors so WASD only moves horizontally
        Vector3 flatForward = transform.forward;
        flatForward.y = 0f;
        flatForward.Normalize();

        Vector3 flatRight = transform.right;
        flatRight.y = 0f;
        flatRight.Normalize();

        // --- Panning (WASD) — horizontal only ---
        if (kb.wKey.isPressed)
            pos += flatForward * panSpeed * Time.deltaTime;
        if (kb.sKey.isPressed)
            pos -= flatForward * panSpeed * Time.deltaTime;
        if (kb.dKey.isPressed)
            pos += flatRight * panSpeed * Time.deltaTime;
        if (kb.aKey.isPressed)
            pos -= flatRight * panSpeed * Time.deltaTime;

        // --- Zoom (Z / X keys) ---
        if (kb.zKey.isPressed)
            pos.y -= zoomSpeed * Time.deltaTime;
        if (kb.xKey.isPressed)
            pos.y += zoomSpeed * Time.deltaTime;

        // --- Rotation (Q / E keys) ---
        if (kb.qKey.isPressed)
            transform.Rotate(Vector3.up, -rotationSpeed * Time.deltaTime, Space.World);
        if (kb.eKey.isPressed)
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

        // --- Clamp ---
        pos.x = Mathf.Clamp(pos.x, panLimitX.x, panLimitX.y);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        pos.z = Mathf.Clamp(pos.z, panLimitZ.x, panLimitZ.y);

        transform.position = pos;
    }
}
