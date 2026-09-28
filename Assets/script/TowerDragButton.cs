using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class TowerDragButton : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [SerializeField] private GameObject towerPrefab;
    [SerializeField] private TowerplacementAndbuying placementManager;
    [SerializeField] private Canvas canvas;

    private RectTransform dragIcon;

    private void Awake()
    {
        if (placementManager == null)
        {
            placementManager = FindFirstObjectByType<TowerplacementAndbuying>();
        }

        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (towerPrefab == null || canvas == null)
            return;

        GameObject iconObject = new GameObject(
            "Tower Drag Icon",
            typeof(RectTransform),
            typeof(CanvasGroup),
            typeof(Image)
        );

        dragIcon = iconObject.GetComponent<RectTransform>();
        dragIcon.SetParent(canvas.transform, false);
        dragIcon.SetAsLastSibling();

        RectTransform sourceRect = transform as RectTransform;
        dragIcon.sizeDelta = sourceRect.rect.size;

        Image sourceImage = GetComponent<Image>();
        Image iconImage = iconObject.GetComponent<Image>();
        iconImage.sprite = sourceImage.sprite;
        iconImage.color = new Color(1f, 1f, 1f, 0.75f);
        iconImage.raycastTarget = false;

        iconObject.GetComponent<CanvasGroup>().blocksRaycasts = false;
        UpdateDragIcon(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        UpdateDragIcon(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
        {
            Destroy(dragIcon.gameObject);
        }

        GameObject objectUnderPointer = eventData.pointerCurrentRaycast.gameObject;
        bool releasedOverUi = objectUnderPointer != null &&
            objectUnderPointer.GetComponentInParent<Canvas>() != null;

        if (!releasedOverUi && placementManager != null)
        {
            placementManager.TryPlaceTower(towerPrefab, eventData.position);
        }
    }

    private void UpdateDragIcon(PointerEventData eventData)
    {
        if (dragIcon == null)
            return;

        RectTransform canvasRect = canvas.transform as RectTransform;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            eventData.position,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera,
            out Vector2 localPosition))
        {
            dragIcon.anchoredPosition = localPosition;
        }
    }
}
