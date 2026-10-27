using System;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentSlots : MonoBehaviour
{
    [SerializeField]
    private List<ItemSlotType> slotTypes = new List<ItemSlotType>
    {
        ItemSlotType.Weapon, ItemSlotType.Armor, ItemSlotType.Core
    };

    [Tooltip("Give equipped items back to the inventory if the tower is destroyed")]
    [SerializeField] private bool returnItemsWhenDestroyed = true;

    private ItemData[] equipped;

    // Abilities each equipped item created, so they can be removed on swap.
    private readonly Dictionary<ItemData, List<AbilityRuntime>> activeAbilities =
        new Dictionary<ItemData, List<AbilityRuntime>>();

    public event Action Changed;

    public int SlotCount => slotTypes.Count;
    public ItemSlotType GetSlotType(int slot) => slotTypes[slot];

    private ItemData[] Equipped
    {
        get
        {
            if (equipped == null || equipped.Length != slotTypes.Count)
                equipped = new ItemData[slotTypes.Count];
            return equipped;
        }
    }

    public ItemData GetItem(int slot) =>
        slot >= 0 && slot < SlotCount ? Equipped[slot] : null;

    public bool CanEquip(ItemData item) =>
        item != null && slotTypes.Contains(item.slot);

    // First empty matching slot, otherwise the first matching slot (it gets swapped).
    public int FindSlotFor(ItemData item)
    {
        if (item == null) return -1;

        int firstMatch = -1;
        for (int i = 0; i < slotTypes.Count; i++)
        {
            if (slotTypes[i] != item.slot) continue;
            if (Equipped[i] == null) return i;
            if (firstMatch < 0) firstMatch = i;
        }
        return firstMatch;
    }

    public bool Equip(ItemData item, out ItemData replaced)
    {
        return EquipAt(FindSlotFor(item), item, out replaced);
    }

    public bool EquipAt(int slot, ItemData item, out ItemData replaced)
    {
        replaced = null;

        if (item == null || slot < 0 || slot >= SlotCount || slotTypes[slot] != item.slot)
            return false;

        replaced = Equipped[slot];
        if (replaced != null) Remove(replaced);

        Equipped[slot] = item;
        Apply(item);
        Changed?.Invoke();
        return true;
    }

    public ItemData Unequip(int slot)
    {
        ItemData item = GetItem(slot);
        if (item == null) return null;

        Remove(item);
        Equipped[slot] = null;
        Changed?.Invoke();
        return item;
    }

    private void Apply(ItemData item)
    {
        TowerStats stats = GetComponent<TowerStats>();
        if (stats != null)
        {
            foreach (StatBonus bonus in item.stats)
            {
                stats.AddModifier(item, bonus.stat, bonus.amount);
            }
        }

        if (item.abilities.Count == 0) return;

        AbilityController controller = GetComponent<AbilityController>();
        if (controller == null) controller = gameObject.AddComponent<AbilityController>();

        List<AbilityRuntime> runtimes = new List<AbilityRuntime>();
        foreach (AbilityData data in item.abilities)
        {
            runtimes.Add(controller.AddAbility(data.Clone()));
        }
        activeAbilities[item] = runtimes;
    }

    private void Remove(ItemData item)
    {
        TowerStats stats = GetComponent<TowerStats>();
        if (stats != null) stats.RemoveModifiers(item);

        if (activeAbilities.TryGetValue(item, out List<AbilityRuntime> runtimes))
        {
            AbilityController controller = GetComponent<AbilityController>();
            if (controller != null)
            {
                foreach (AbilityRuntime runtime in runtimes)
                {
                    controller.RemoveAbility(runtime);
                }
            }
            activeAbilities.Remove(item);
        }
    }

    private void OnDestroy()
    {
        if (!returnItemsWhenDestroyed || PlayerInventory.Instance == null) return;

        foreach (ItemData item in Equipped)
        {
            if (item != null) PlayerInventory.Instance.Add(item, ignoreCapacity: true);
        }
    }
}