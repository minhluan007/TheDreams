
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
        const int amountToMove = 1;
        int maxStack = Mathf.Max(1, item.maxStack);

        foreach (var slot in destination)
        {
            if (slot == null || slot.item != item)
                continue;

            if (slot.amount >= maxStack)
                continue;

            slot.amount += amountToMove;
            from.amount -= amountToMove;

            if (from.amount <= 0)
            {
                from.item = null;
                from.amount = 0;
            }

            return true;
        }

        foreach (var slot in destination)
        {
            if (slot == null || slot.item != null)
                continue;

            slot.item = item;
            slot.amount = amountToMove;
            from.amount -= amountToMove;

            if (from.amount <= 0)
            {
                from.item = null;
                from.amount = 0;
            }

            return true;
        }

        return false;
    }
}
