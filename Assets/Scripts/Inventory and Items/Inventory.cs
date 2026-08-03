using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public List<InventorySlot> slots = new List<InventorySlot>();

    public void AddItem(Item item)
    {
        // Try to stack with existing
        var slot = slots.Find(s => s.item.itemName == item.itemName);
        if (slot != null)
        {
            slot.count++;
        }
        else
        {
            slots.Add(new InventorySlot(item, 1));
        }

        Debug.Log("Added " + item.itemName);
    }

    public void UseItem(int slotIndex, GameObject user, bool inBattle = false)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count) return;

        var slot = slots[slotIndex];
        var item = slot.item;

        // Prevent use outside battle for battle-only items
        if (item.itemType == ItemType.BattleOnly && !inBattle)
        {
            Debug.Log(item.itemName + " can only be used in battle!");
            return;
        }

        // First, use the item
        item.Use(user);

        // Handle removal depending on type
        if (item.isConsumable)
        {
            if (!(item is HealingItem))
            {
                slot.count--;
            }
        }
        else if (item.isConsumable && inBattle)
        {
            slot.count--;
        }

        // Remove slot if count hits 0
        if (slot.count <= 0)
            slots.RemoveAt(slotIndex);
    }
}