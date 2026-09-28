using UnityEngine;

public class TowerplacementAndbuying : MonoBehaviour
{
    [SerializeField] private Camera worldCamera;
    [SerializeField] private bool snapToGrid;
    [SerializeField] private float gridSize = 1f;

    private void Awake()
    {
        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }
    }

    public bool TryPlaceTower(GameObject towerPrefab, Vector2 screenPosition)
    {
        if (towerPrefab == null || worldCamera == null)
            return false;

        if (GameManager.Instance == null ||
            GameManager.Instance.CurrentPhase != GamePhase.Buy)
        {
            Debug.Log("Towers can only be placed during the buy phase.");
            return false;
        }

        Ray pointerRay = worldCamera.ScreenPointToRay(screenPosition);
        Plane gamePlane = new Plane(Vector3.forward, Vector3.zero);

        if (!gamePlane.Raycast(pointerRay, out float distance))
            return false;

        Vector3 placementPosition = pointerRay.GetPoint(distance);

        if (snapToGrid)
        {
            placementPosition.x = Mathf.Round(placementPosition.x / gridSize) * gridSize;
            placementPosition.y = Mathf.Round(placementPosition.y / gridSize) * gridSize;
        }

        placementPosition.z = 0f;

        Tower towerToPlace = towerPrefab.GetComponent<Tower>();
        if (towerToPlace == null)
        {
            Debug.LogWarning("The selected prefab does not have a Tower component.");
            return false;
        }

        if (!CanPlaceTower(placementPosition, towerToPlace.PlacementRadius))
        {
            Debug.Log("A tower is already occupying that space.");
            return false;
        }

        if (!GameManager.Instance.TrySpendCoins(towerToPlace.Cost))
        {
            Debug.Log("Not enough coins to buy this tower.");
            return false;
        }

        Instantiate(towerPrefab, placementPosition, Quaternion.identity);
        return true;
    }

    private bool CanPlaceTower(Vector3 position, float newTowerRadius)
    {
        foreach (Tower existingTower in Tower.ActiveTowers)
        {
            if (existingTower == null)
                continue;

            float minimumDistance =
                newTowerRadius + existingTower.PlacementRadius;

            if (Vector2.Distance(position, existingTower.transform.position)
                < minimumDistance)
            {
                return false;
            }
        }

        return true;
    }
}
