using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    [Tooltip("0 = unlimited")]
    [SerializeField] private int capacity = 30;

    [Tooltip("Hand-made items to start with (optional, handy for testing)")]
    [SerializeField] private List<ItemData> startingItems = new List<ItemData>();

    private readonly List<ItemData> items = new List<ItemData>();

    public IReadOnlyList<ItemData> Items => items;
    public event Action Changed;

    private void Awake()
    {
        Instance = this;
        items.AddRange(startingItems);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool IsFull => capacity > 0 && items.Count >= capacity;

    public bool Add(ItemData item, bool ignoreCapacity = false)
    {
        if (item == null || (!ignoreCapacity && IsFull)) return false;

        items.Add(item);
        Changed?.Invoke();
        return true;
    }

    public bool Remove(ItemData item)
    {
        bool removed = items.Remove(item);
        if (removed) Changed?.Invoke();
        return removed;
    }

    // Moves an item to the tower. Whatever it replaces goes back into the inventory.
    public bool EquipFromInventory(ItemData item, EquipmentSlots tower)
    {
        if (tower == null || !items.Contains(item) || !tower.CanEquip(item))
            return false;

        items.Remove(item);

        if (!tower.Equip(item, out ItemData replaced))
        {
            items.Add(item);
            return false;
        }

        if (replaced != null) items.Add(replaced);
        Changed?.Invoke();
        return true;
    }

    public bool UnequipToInventory(EquipmentSlots tower, int slot)
    {
        if (tower == null || tower.GetItem(slot) == null || IsFull)
            return false;

        ItemData item = tower.Unequip(slot);
        items.Add(item);
        Changed?.Invoke();
        return true;
    }
}