using UnityEngine;
using TMPro;

/// <summary>
/// Shows keyboard control hints during building placement.
/// Replaces the clickable confirm/cancel buttons with text instructions.
/// Attach to a Canvas.
/// </summary>
public class PlacementConfirmUI : MonoBehaviour
{
    [Header("References")]
    public BuildingPlacer buildingPlacer;
    public Camera mainCamera;

    [Header("UI Elements")]
    public GameObject panel;               // The hints panel
    public TextMeshProUGUI hintsText;      // Text showing controls
    public TextMeshProUGUI statusText;     // Text showing valid/invalid

    private Transform followTarget;

    // Kept for backward compatibility with BuildingPlacer
    public void Show(Transform target)
    {
        followTarget = target;
        if (panel != null)
            panel.SetActive(true);

        if (hintsText != null)
            hintsText.text = "Arrow Keys: Move  |  Enter: Place  |  Esc: Cancel";

        UpdateStatus(true);
    }

    public void Hide()
    {
        followTarget = null;
        if (panel != null)
            panel.SetActive(false);
    }

    public void SetConfirmInteractable(bool canPlace)
    {
        UpdateStatus(canPlace);
    }

    private void UpdateStatus(bool canPlace)
    {
        if (statusText != null)
        {
            statusText.text = canPlace ? "<color=#00FF00>✓ Valid Position</color>"
                                       : "<color=#FF0000>✗ Blocked!</color>";
        }
    }

    void LateUpdate()
    {
        if (followTarget != null && panel != null && panel.activeSelf && mainCamera != null)
        {
            Vector3 screenPos = mainCamera.WorldToScreenPoint(followTarget.position);
            if (screenPos.z > 0)
            {
                panel.transform.position = screenPos + new Vector3(0, -70f, 0);
            }
        }
    }
}
