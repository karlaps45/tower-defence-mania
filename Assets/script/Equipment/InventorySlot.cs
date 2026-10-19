using UnityEngine;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour, IPointerClickHandler
{
    public InventoryItem myItem { get; set; }

    public Slottag myTag;


    public void OnPointerclick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (Inventory.carriedItem == null) return;
            if (myTag != Slottag.None && Inventory.carriedItem.myItem.Itemtag != myTag) return;
            setItem(InventoryItem.carriedItem);
        }
    }


    public void setItem(InventoryItem item)
    {
        Inventory.carriedITem = null;

        item.activeSlot.myItem = null;

        myItem = item;
        myItem.activeSlot = this;
        myItem.transform.SetParent(transform);
        myItem.canvasgroup.blocksRaycasts = true;

        if (myTag != Slottag.None)
        {
            Inventory.Singleton.EquipEquipment(myTag, myItem);
        }
    }


}
