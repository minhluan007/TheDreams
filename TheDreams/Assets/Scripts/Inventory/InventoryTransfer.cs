
using System.Collections.Generic;
using UnityEngine;

public static class InventoryTransfer
{
    public static bool Move(
        List<InventorySlotData> source,
        int sourceIndex,
        List<InventorySlotData> destination)
    {
        if (source == null || destination == null)
            return false;

        if (sourceIndex < 0 || sourceIndex >= source.Count)
            return false;

        var from = source[sourceIndex];

        if (from == null || from.item == null || from.amount <= 0)
            return false;

        ItemData item = from.item;
        int amount = from.amount;
        int maxStack = Mathf.Max(1, item.maxStack);

        int capacity = 0;

        foreach (var slot in destination)
        {
            if (slot == null || slot.item == null)
            {
                capacity += maxStack;
            }
            else if (slot.item == item)
            {
                capacity += Mathf.Max(0, maxStack - slot.amount);
            }
        }

        // Chỉ chuyển khi kho đích chứa được toàn bộ stack.
        if (capacity < amount)
            return false;

        foreach (var slot in destination)
        {
            if (slot.item != item)
                continue;

            int available = maxStack - slot.amount;
            int moved = Mathf.Min(available, amount);

            if (moved <= 0)
                continue;

            slot.amount += moved;
            amount -= moved;

            if (amount == 0)
                break;
        }

        foreach (var slot in destination)
        {
            if (slot.item != null)
                continue;

            int moved = Mathf.Min(maxStack, amount);

            slot.item = item;
            slot.amount = moved;
            amount -= moved;

            if (amount == 0)
                break;
        }

        from.item = null;
        from.amount = 0;

        return true;
    }
}
