using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// RTS-style camera controller.
/// WASD pans on the horizontal plane (no zoom effect).
/// Z/X zoom, Q/E rotate. All keyboard-based.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("References")]
    public Terrain terrain;

    [Header("Movement")]
    public float panSpeed = 50f;

    [Header("Zoom")]
    public float zoomSpeed = 60f;
    public float minY = 10f;
    public float maxY = 800f;

    [Header("Rotation")]
    public float rotationSpeed = 80f;

    [Header("Start Position")]
    [Tooltip("Starting look-down angle in degrees")]
    public float startPitch = 55f;

    private Vector2 panLimitX;
    private Vector2 panLimitZ;

    void Start()
    {
        // Read terrain size dynamically
        float terrainWidth = 1000f;
        float terrainLength = 1000f;

        if (terrain != null && terrain.terrainData != null)
        {
            Vector3 terrainSize = terrain.terrainData.size;
            Vector3 terrainPos = terrain.transform.position;
            terrainWidth = terrainSize.x;
            terrainLength = terrainSize.z;

            // Pan limits: terrain bounds + 20% margin
            float marginX = terrainWidth * 0.2f;
            float marginZ = terrainLength * 0.2f;
            panLimitX = new Vector2(terrainPos.x - marginX, terrainPos.x + terrainWidth + marginX);
            panLimitZ = new Vector2(terrainPos.z - marginZ, terrainPos.z + terrainLength + marginZ);
        }
        else
        {
            panLimitX = new Vector2(-200, 1200);
            panLimitZ = new Vector2(-200, 1200);
        }

        // Center camera over terrain, zoomed out to see the whole thing
        float centerX = (terrain != null ? terrain.transform.position.x : 0) + terrainWidth / 2f;
        float centerZ = (terrain != null ? terrain.transform.position.z : 0) + terrainLength / 2f;
        float startHeight = Mathf.Max(terrainWidth, terrainLength) * 0.5f;

        float offsetZ = startHeight / Mathf.Tan(startPitch * Mathf.Deg2Rad);
        transform.position = new Vector3(centerX, startHeight, centerZ - offsetZ);
        transform.rotation = Quaternion.Euler(startPitch, 0f, 0f);
    }

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
