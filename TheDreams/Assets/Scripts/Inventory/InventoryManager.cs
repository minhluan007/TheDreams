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
        foreach (var slot in slots)
        {
            if (slot.item == item && slot.amount > 0)
            {
                slot.amount -= amount;
                if (slot.amount <= 0)
                    slots.Remove(slot);
            }
        }
        InventoryUI.Instance.Refresh();
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
            if (slot.item == item)
            {
                slot.amount = amount;
                if (slot.amount <= 0)
                    slots.Remove(slot);
            }
        }
        InventoryUI.Instance.Refresh();
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
            if (slot.item == item)
            {
                slot.amount += amount;
            }
        }
        InventoryUI.Instance.Refresh();
    }

    public void MinusItem(ItemData item, int amount)
    {
        if (item == null)
        {
            Debug.LogError("MinusItem: ItemData == null!");
            return;
        }
        foreach (var slot in slots)
        {
            if (slot.item == item && slot.amount > 0)
            {
                slot.amount -= amount;
                if (slot.amount <= 0)
                    slots.Remove(slot);
            }
        }
        InventoryUI.Instance.Refresh();
    }

    public void AddItem(ItemData item, int amount)
    {
        if (item == null)
        {
            Debug.LogError("AddItem: ItemData == null!");
            return;
        }

        // Tìm stack đang có
        foreach (var slot in slots)
        {
            if (slot.item == item && slot.amount < item.maxStack)
            {
                int space = item.maxStack - slot.amount;
                int addAmount = Mathf.Min(space, amount);

                slot.amount += addAmount;
                amount -= addAmount;

                if (amount <= 0)
                    return;
            }
        }

        // Tạo slot mới
        while (amount > 0)
        {
            int addAmount = Mathf.Min(amount, item.maxStack);

            slots.Add(new InventorySlotData
            {
                item = item,
                amount = addAmount
            });

            amount -= addAmount;
        }

        Debug.LogWarning("Inventory đã đầy!");

        InventoryUI.Instance.Refresh();
    }
}