using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    public List<InventorySlotData> slots = new();

    [SerializeField] private ItemData testApple;

    private void Awake()
    {
        Instance = this;
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