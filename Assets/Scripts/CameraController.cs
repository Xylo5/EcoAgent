using UnityEngine;

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
    public float panSpeed = 40f;

    [Header("Zoom")]
    public float zoomSpeed = 40f;
    public float minY = 5f;
    public float maxY = 300f;

    [Header("Rotation")]
    public float rotationSpeed = 80f;

    [Header("Start Position")]
    [Tooltip("Starting look-down angle in degrees")]
    public float startPitch = 55f;

    private Vector2 panLimitX;
    private Vector2 panLimitZ;

    void Start()
    {
        // Auto-find terrain if not assigned in Inspector
        if (terrain == null)
        {
            terrain = FindAnyObjectByType<Terrain>();
            if (terrain == null)
                Debug.LogWarning("[CameraController] No Terrain found in scene!");
        }

        // Read terrain size dynamically
        float terrainWidth = 250f;
        float terrainLength = 250f;

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
            panLimitX = new Vector2(-50, 300);
            panLimitZ = new Vector2(-50, 300);
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
        Vector3 pos = transform.position;

        // Flatten the forward/right vectors so WASD only moves horizontally
        Vector3 flatForward = transform.forward;
        flatForward.y = 0f;
        flatForward.Normalize();

        Vector3 flatRight = transform.right;
        flatRight.y = 0f;
        flatRight.Normalize();

        // --- Panning (WASD + Middle Mouse drag) ---
        Vector2 pan = InputManager.Instance.GetPanInput();
        pos += flatRight * pan.x * panSpeed * Time.deltaTime;
        pos += flatForward * pan.y * panSpeed * Time.deltaTime;

        // --- Zoom (Z / X keys + Scroll Wheel) ---
        float zoom = InputManager.Instance.GetZoomInput();
        pos.y += zoom * zoomSpeed * Time.deltaTime;

        // --- Rotation (Q / E keys + Right Mouse drag) ---
        float rotate = InputManager.Instance.GetRotateInput();
        transform.Rotate(Vector3.up, rotate * rotationSpeed * Time.deltaTime, Space.World);

        // --- Clamp ---
        pos.x = Mathf.Clamp(pos.x, panLimitX.x, panLimitX.y);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        pos.z = Mathf.Clamp(pos.z, panLimitZ.x, panLimitZ.y);

        transform.position = pos;
    }
}
