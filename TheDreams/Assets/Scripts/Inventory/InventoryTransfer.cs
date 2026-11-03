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

        InventorySlotData from = source[sourceIndex];
        if (from == null || !from.HasItem)
            return false;

        ItemData item = from.item;
        const int amountToMove = 1;
        int maxStack = Mathf.Max(1, item.maxStack);

        for (int i = 0; i < destination.Count; i++)
        {
            InventorySlotData slot = destination[i];
            if (slot == null || slot.item != item || slot.amount <= 0 || slot.amount >= maxStack)
                continue;

            slot.amount += amountToMove;
            CopySellPrice(from, slot);
            Consume(from, amountToMove);
            return true;
        }

        for (int i = 0; i < destination.Count; i++)
        {
            InventorySlotData slot = destination[i];
            if (slot == null || slot.HasItem)
                continue;

            slot.item = item;
            slot.amount = amountToMove;
            if (from.sellPriceSet)
                CopySellPrice(from, slot);
            else
            {
                slot.coinPriceSell = 0;
                slot.sellPriceSet = false;
            }

            Consume(from, amountToMove);
            return true;
        }

        return false;
    }

    static void CopySellPrice(InventorySlotData from, InventorySlotData slot)
    {
        if (!from.sellPriceSet)
            return;

        slot.coinPriceSell = from.coinPriceSell;
        slot.sellPriceSet = true;
    }

    static void Consume(InventorySlotData from, int amount)
    {
        from.amount -= amount;
        if (from.amount <= 0)
            from.Clear();
    }
}
