using System.Collections.Generic;
using UnityEngine;

public class ShelfInventory : MonoBehaviour
{
    [SerializeField] private int slotCount = 0;

    public List<InventorySlotData> slots = new();
    public List<ShelfSlotItem> shelfSlotItem = new();

    private void Awake()
    {
        EnsureSlots();
        RefreshVisuals();
    }

    private void EnsureSlots()
    {
        if (slotCount <= 0)
            slotCount = shelfSlotItem.Count;

        while (slots.Count < slotCount)
        {
            slots.Add(new InventorySlotData
            {
                item = null,
                amount = 0
            });
        }
    }

    public void RefreshVisuals()
    {
        int count = Mathf.Min(slots.Count, shelfSlotItem.Count);

        for (int i = 0; i < count; i++)
        {
            if (shelfSlotItem[i] == null)
                continue;

            var data = slots[i];
            bool hasItem = data != null && data.item != null && data.amount > 0;
            shelfSlotItem[i].UpdateSprite(hasItem ? data.item.icon : null);
        }
    }
}
