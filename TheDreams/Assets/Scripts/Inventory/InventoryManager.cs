using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    public List<InventorySlotData> slots = new();

    [SerializeField] private List<ItemData> itemDatabase = new();

    private void Awake()
    {
        Instance = this;
    }


    public void RemoveItem(ItemData item, int amount)
    {
        if (item == null)
        {
            Debug.LogError("RemoveItem: ItemData == null!");
            return;
        }
        Consume(item, amount);
        RefreshUI();
    }

    public void UpdateItem(ItemData item, int amount)
    {
        if (item == null)
        {
            Debug.LogError("EditItem: ItemData == null!");
            return;
        }
        foreach (var slot in slots)
        {
            if (slot == null || slot.item != item)
                continue;

            slot.amount = amount;
            if (slot.amount <= 0)
                ClearSlot(slot);
        }
        RefreshUI();
    }

    public void PlusItem(ItemData item, int amount)
    {
        if (item == null)
        {
            Debug.LogError("PlusItem: ItemData == null!");
            return;
        }
        foreach (var slot in slots)
        {
            if (slot == null || slot.item != item)
                continue;

            slot.amount += amount;
        }
        RefreshUI();
    }

    public void MinusItem(ItemData item, int amount)
    {
        if (item == null)
        {
            Debug.LogError("MinusItem: ItemData == null!");
            return;
        }

        Consume(item, amount);
        RefreshUI();
    }

    public int AddItem(ItemData item, int amount)
    {
        if (item == null)
        {
            Debug.LogError("AddItem: ItemData == null!");
            return 0;
        }

        if (amount <= 0)
            return 0;

        int remaining = amount;
        int maxStack = Mathf.Max(1, item.maxStack);

        foreach (var slot in slots)
        {
            if (slot == null || slot.item != item || slot.amount >= maxStack)
                continue;

            int addAmount = Mathf.Min(maxStack - slot.amount, remaining);
            slot.amount += addAmount;
            remaining -= addAmount;

            if (remaining <= 0)
                break;
        }

        if (remaining > 0)
        {
            foreach (var slot in slots)
            {
                if (slot == null || (slot.item != null && slot.amount > 0))
                    continue;

                int addAmount = Mathf.Min(maxStack, remaining);
                slot.item = item;
                slot.amount = addAmount;
                remaining -= addAmount;

                if (remaining <= 0)
                    break;
            }
        }

        int added = amount - remaining;

        if (added <= 0)
            Debug.LogWarning("Inventory đã đầy!");
        else
            RefreshUI();

        return added;
    }

    private void Consume(ItemData item, int amount)
    {
        if (amount <= 0)
            return;

        foreach (var slot in slots)
        {
            if (slot == null || slot.item != item || slot.amount <= 0)
                continue;

            int removeAmount = Mathf.Min(slot.amount, amount);
            slot.amount -= removeAmount;
            amount -= removeAmount;

            if (slot.amount <= 0)
                ClearSlot(slot);

            if (amount <= 0)
                return;
        }
    }

    private static void ClearSlot(InventorySlotData slot)
    {
        slot.item = null;
        slot.amount = 0;
    }

    private static void RefreshUI()
    {
        if (InventoryUI.Instance != null)
            InventoryUI.Instance.Refresh();
    }
}