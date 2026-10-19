using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class InventoryItem : MonoBehaviour, IPointerClickHandler
{


    Image itemIcon;

    public CanvasGroup canvasgroup { get; private set; }

    public Item myitem { get; private set; }

    public InventorySlot activeSlot { get; private set; }


    private void Awake()
    {
        canvasgroup = GetComponent<CanvasGroup>();
        itemIcon = GetComponent<Image>();

    }


    public void Initialize(Item item, Inventoryslot parent)
    {
        activeSlot = parent;
        activeSlot.myItem = this;
        myitem = item;
        itemIcon.sprite = item.sprite;
    }

    public void OnPointerClick(PointerEventData eventData)
    {

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            Inventory.Singleton.SetCarriedItem(this);
        }
    }
}
