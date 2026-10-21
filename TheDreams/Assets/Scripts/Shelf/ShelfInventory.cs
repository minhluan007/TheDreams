
using System.Collections.Generic;
using UnityEngine;

public class ShelfInventory : MonoBehaviour
{
    [SerializeField] private int slotCount = 0;

    public List<InventorySlotData> slots = new();

    private void Awake()
    {
        if (slots.Count > 0)
            return;

        for (int i = 0; i < slotCount; i++)
        {
            slots.Add(new InventorySlotData
            {
                item = null,
                amount = 0
            });
        }
    }
}
