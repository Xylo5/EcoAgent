using UnityEngine;

public class BuildingPlacement : MonoBehaviour
{
    public GameObject buildingPrefab;
    public GridManager gridManager;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            PlaceBuilding();
        }
    }

    void PlaceBuilding()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            Vector2Int gridPos = gridManager.GetGridPosition(hit.point);
            Vector3 worldPos = gridManager.GetWorldPosition(gridPos.x, gridPos.y);

            Instantiate(buildingPrefab, worldPos, Quaternion.identity);
        }
    }
}