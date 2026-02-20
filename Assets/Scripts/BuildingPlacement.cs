using UnityEngine;
using UnityEngine.InputSystem;

public class BuildingPlacement : MonoBehaviour
{
    public GameObject buildingPrefab;
    public GridManager gridManager;

    private PlayerInputActions inputActions;

    private void Awake()
    {
        Debug.Log("Input system initialized");
        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        // Enable only the Player action map
        inputActions.Player.Enable();
        inputActions.Player.Place.performed += OnPlace;
    }

    private void OnDisable()
    {
        inputActions.Player.Place.performed -= OnPlace;
        inputActions.Player.Disable();
    }

    private void OnPlace(InputAction.CallbackContext context)
    {
        Debug.Log("Click detected");

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector2Int gridPos = gridManager.GetGridPosition(hit.point);
            Vector3 worldPos = gridManager.GetWorldPosition(gridPos.x, gridPos.y);

            Instantiate(buildingPrefab, worldPos, Quaternion.identity);
        }
    }
}