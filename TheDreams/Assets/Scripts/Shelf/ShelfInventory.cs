using System;
using System.Collections.Generic;
using UnityEngine;

public class ShelfInventory : MonoBehaviour
{
    [SerializeField] private int slotCount = 0;

    public List<InventorySlotData> slots = new();
    public List<ShelfSlotItem> shelfSlotItem = new();

    int stockEpoch;

    private void Awake()
    {
        stockEpoch = ShelfStock.Epoch;
        EnsureSlots();
        ShelfStock.Apply(this);
        RefreshVisuals();
    }

    private void Start()
    {
        Remember();
    }

    private void OnDestroy()
    {
        if (!gameObject.scene.isLoaded)
            Remember();
    }

    public bool HasStock
    {
        get
        {
            for (int i = 0; i < slots.Count; i++)
            {
                InventorySlotData slot = slots[i];
                if (slot != null && slot.item != null && slot.amount > 0)
                    return true;
            }

            return false;
        }
    }

    public void Remember()
    {
        if (stockEpoch != ShelfStock.Epoch)
            return;

        ShelfStock.Save(gameObject.scene.name, gameObject.name, slots);
    }

    public bool TryChooseSlot(out int index, out ItemData item)
    {
        index = -1;
        item = null;
        int count = 0;
        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlotData slot = slots[i];
            if (slot != null && slot.item != null && slot.amount > 0)
                count++;
        }

        if (count == 0)
            return false;

        int pick = UnityEngine.Random.Range(0, count);
        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlotData slot = slots[i];
            if (slot == null || slot.item == null || slot.amount <= 0)
                continue;

            if (pick == 0)
            {
                index = i;
                item = slot.item;
                return true;
            }

            pick--;
        }

        return false;
    }

    public bool TryTakeSlot(int index, out ItemData item)
    {
        item = null;
        if (slots == null || index < 0 || index >= slots.Count)
            return false;

        InventorySlotData slot = slots[index];
        if (slot == null || slot.item == null || slot.amount <= 0)
            return false;

        item = slot.item;
        slot.amount--;
        if (slot.amount <= 0)
        {
            slot.item = null;
            slot.amount = 0;
        }

        RefreshVisuals();
        Remember();
        if (ShelfUI.Instance != null)
            ShelfUI.Instance.NotifyShelfChanged(this);

        return true;
    }

    public static void RememberLoaded()
    {
        ShelfInventory[] shelves = FindObjectsByType<ShelfInventory>(FindObjectsSortMode.None);
        for (int i = 0; i < shelves.Length; i++)
        {
            if (shelves[i] != null)
                shelves[i].Remember();
        }
    }

    public static ShelfInventory FindNamed(string shelfName)
    {
        if (string.IsNullOrEmpty(shelfName))
            return null;

        ShelfInventory[] shelves = FindObjectsByType<ShelfInventory>(FindObjectsSortMode.None);
        for (int i = 0; i < shelves.Length; i++)
        {
            if (shelves[i] != null && shelves[i].gameObject.name == shelfName)
                return shelves[i];
        }

        return null;
    }

    public static ShelfInventory FindForNpc(Vector3 position)
    {
        ShelfInventory[] shelves = FindObjectsByType<ShelfInventory>(FindObjectsSortMode.None);
        ShelfInventory nearest = null;
        ShelfInventory nearestStock = null;
        float nearestDistance = float.MaxValue;
        float stockDistance = float.MaxValue;

        for (int i = 0; i < shelves.Length; i++)
        {
            ShelfInventory shelf = shelves[i];
            if (shelf == null)
                continue;

            float distance = Vector2.Distance(position, shelf.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = shelf;
            }

            if (shelf.HasStock && distance < stockDistance)
            {
                stockDistance = distance;
                nearestStock = shelf;
            }
        }

        return nearestStock != null ? nearestStock : nearest;
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

    public static void ApplyRemembered()
    {
        ShelfInventory[] shelves = FindObjectsByType<ShelfInventory>(FindObjectsSortMode.None);
        for (int i = 0; i < shelves.Length; i++)
        {
            ShelfInventory shelf = shelves[i];
            if (shelf == null)
                continue;

            ShelfStock.Apply(shelf);
            shelf.RefreshVisuals();
        }
    }
}

public static class ShelfStock
{
    class Record
    {
        public string key;
        public ItemData[] items;
        public int[] amounts;
    }

    static readonly List<Record> records = new List<Record>();

    public static int Epoch { get; private set; }

    public static void Save(string sceneName, string shelfName, List<InventorySlotData> slots)
    {
        if (string.IsNullOrEmpty(sceneName) || string.IsNullOrEmpty(shelfName) || slots == null)
            return;

        Record record = Find(Key(sceneName, shelfName), true);
        record.items = new ItemData[slots.Count];
        record.amounts = new int[slots.Count];
        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlotData slot = slots[i];
            if (slot == null || slot.item == null || slot.amount <= 0)
                continue;

            record.items[i] = slot.item;
            record.amounts[i] = slot.amount;
        }
    }

    public static void Apply(ShelfInventory shelf)
    {
        if (shelf == null || shelf.slots == null)
            return;

        Record record = Find(Key(shelf.gameObject.scene.name, shelf.gameObject.name), false);
        if (record == null || record.amounts == null || record.items == null)
            return;

        int count = Mathf.Min(shelf.slots.Count, record.amounts.Length);
        for (int i = 0; i < count; i++)
        {
            InventorySlotData slot = shelf.slots[i];
            if (slot == null)
                continue;

            if (record.items[i] == null || record.amounts[i] <= 0)
            {
                slot.item = null;
                slot.amount = 0;
                continue;
            }

            slot.item = record.items[i];
            slot.amount = record.amounts[i];
        }
    }

    public static bool HasStock(string sceneName)
    {
        string prefix = sceneName + "/";
        for (int i = 0; i < records.Count; i++)
        {
            Record record = records[i];
            if (record.amounts == null || record.items == null || record.key == null || !record.key.StartsWith(prefix))
                continue;

            for (int s = 0; s < record.amounts.Length; s++)
            {
                if (record.items[s] != null && record.amounts[s] > 0)
                    return true;
            }
        }

        return false;
    }

    public static bool TryTakeAny(string sceneName, out ItemData item)
    {
        item = null;
        string prefix = sceneName + "/";
        for (int i = 0; i < records.Count; i++)
        {
            Record record = records[i];
            if (record.amounts == null || record.items == null || record.key == null || !record.key.StartsWith(prefix))
                continue;

            for (int s = 0; s < record.amounts.Length; s++)
            {
                if (record.items[s] == null || record.amounts[s] <= 0)
                    continue;

                item = record.items[s];
                record.amounts[s]--;
                if (record.amounts[s] <= 0)
                {
                    record.items[s] = null;
                    record.amounts[s] = 0;
                }

                return true;
            }
        }

        return false;
    }

    static Record Find(string key, bool create)
    {
        for (int i = 0; i < records.Count; i++)
        {
            if (records[i].key == key)
                return records[i];
        }

        if (!create)
            return null;

        var record = new Record { key = key };
        records.Add(record);
        return record;
    }

    static string Key(string sceneName, string shelfName)
    {
        return sceneName + "/" + shelfName;
    }

    public static void Clear()
    {
        records.Clear();
        Epoch++;
    }

    public static void Invalidate()
    {
        Epoch++;
    }

    public static SavedShelf[] Export()
    {
        SavedShelf[] saved = new SavedShelf[records.Count];
        for (int i = 0; i < records.Count; i++)
        {
            Record record = records[i];
            int count = record.amounts != null ? record.amounts.Length : 0;
            SavedSlot[] slots = new SavedSlot[count];
            for (int s = 0; s < count; s++)
            {
                ItemData item = record.items != null && s < record.items.Length ? record.items[s] : null;
                int amount = record.amounts[s];
                if (item == null || amount <= 0)
                {
                    slots[s] = new SavedSlot();
                    continue;
                }

                slots[s] = new SavedSlot
                {
                    itemId = InventoryManager.ItemKey(item),
                    amount = amount
                };
            }

            saved[i] = new SavedShelf
            {
                key = record.key,
                slots = slots
            };
        }

        return saved;
    }

    public static void Import(SavedShelf[] saved, Func<string, ItemData> resolve)
    {
        records.Clear();
        if (saved == null)
            return;

        for (int i = 0; i < saved.Length; i++)
        {
            SavedShelf shelf = saved[i];
            if (shelf == null || string.IsNullOrEmpty(shelf.key))
                continue;

            SavedSlot[] slots = shelf.slots;
            int count = slots != null ? slots.Length : 0;
            Record record = new Record
            {
                key = shelf.key,
                items = new ItemData[count],
                amounts = new int[count]
            };

            for (int s = 0; s < count; s++)
            {
                SavedSlot slot = slots[s];
                if (slot == null || slot.amount <= 0 || string.IsNullOrEmpty(slot.itemId))
                    continue;

                ItemData item = resolve != null ? resolve(slot.itemId) : null;
                if (item == null)
                    continue;

                record.items[s] = item;
                record.amounts[s] = slot.amount;
            }

            records.Add(record);
        }
    }
}
