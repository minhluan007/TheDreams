using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    public List<InventorySlotData> slots = new();

    [SerializeField] private List<ItemData> itemDatabase = new();

    static string[] carryIds;
    static int[] carryAmounts;
    static int[] carrySell;
    static bool[] carrySellSet;
    static bool hasCarry;
    static int sessionEpoch;

    int awakeEpoch;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetSession()
    {
        Instance = null;
        sessionEpoch = 0;
        ClearCarry();
    }

    public static void PrepareNewGame()
    {
        sessionEpoch++;
        ClearCarry();
        if (Instance == null)
            return;

        Instance.ClearSlots();
        if (InventoryUI.Instance != null && InventoryUI.Instance.gameObject.scene == Instance.gameObject.scene)
            InventoryUI.Instance.Refresh();
    }

    private void Awake()
    {
        Instance = this;
        awakeEpoch = sessionEpoch;
        if (hasCarry)
            ApplyCarry();
    }

    private void OnDisable()
    {
        if (awakeEpoch != sessionEpoch)
            return;

        Capture();
        if (Instance != null && Instance != this)
            Instance.ApplyCarry();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static void CaptureActive()
    {
        if (Instance != null)
            Instance.Capture();
    }

    public static SavedSlot[] ExportActive()
    {
        if (Instance == null)
            return new SavedSlot[0];

        return Instance.ExportSlots();
    }

    public static void Store(SavedSlot[] data)
    {
        if (data == null)
            data = new SavedSlot[0];

        carryIds = new string[data.Length];
        carryAmounts = new int[data.Length];
        carrySell = new int[data.Length];
        carrySellSet = new bool[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            SavedSlot saved = data[i];
            if (saved == null || saved.amount <= 0 || string.IsNullOrEmpty(saved.itemId))
                continue;

            carryIds[i] = saved.itemId;
            carryAmounts[i] = saved.amount;
            carrySell[i] = saved.coinPriceSell;
            carrySellSet[i] = saved.sellPriceSet;
        }

        hasCarry = true;
        if (Instance != null)
            Instance.ApplyCarry();
    }

    public static void ClearCarry()
    {
        hasCarry = false;
        carryIds = null;
        carryAmounts = null;
        carrySell = null;
        carrySellSet = null;
    }

    public static string ItemKey(ItemData item)
    {
        if (item == null)
            return "";

        if (!string.IsNullOrEmpty(item.itemId))
            return item.itemId;

        return item.name;
    }

    public void Capture()
    {
        awakeEpoch = sessionEpoch;
        int count = slots != null ? slots.Count : 0;
        carryIds = new string[count];
        carryAmounts = new int[count];
        carrySell = new int[count];
        carrySellSet = new bool[count];
        for (int i = 0; i < count; i++)
        {
            InventorySlotData slot = slots[i];
            if (slot == null || !slot.HasItem)
                continue;

            carryIds[i] = ItemKey(slot.item);
            carryAmounts[i] = slot.amount;
            carrySell[i] = slot.coinPriceSell;
            carrySellSet[i] = slot.sellPriceSet;
        }

        hasCarry = true;
    }

    public SavedSlot[] ExportSlots()
    {
        Capture();
        int count = carryAmounts != null ? carryAmounts.Length : 0;
        SavedSlot[] data = new SavedSlot[count];
        for (int i = 0; i < count; i++)
        {
            data[i] = new SavedSlot
            {
                itemId = carryIds[i] ?? "",
                amount = string.IsNullOrEmpty(carryIds[i]) ? 0 : carryAmounts[i],
                coinPriceSell = carrySell != null && i < carrySell.Length ? carrySell[i] : 0,
                sellPriceSet = carrySellSet != null && i < carrySellSet.Length && carrySellSet[i]
            };
        }

        return data;
    }

    public ItemData FindItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId) || itemDatabase == null)
            return null;

        for (int i = 0; i < itemDatabase.Count; i++)
        {
            ItemData item = itemDatabase[i];
            if (item == null)
                continue;

            if (item.itemId == itemId || item.name == itemId)
                return item;
        }

        return null;
    }

    public void RegisterItem(ItemData item)
    {
        if (item == null)
            return;

        if (itemDatabase == null)
            itemDatabase = new List<ItemData>();

        string key = ItemKey(item);
        for (int i = 0; i < itemDatabase.Count; i++)
        {
            ItemData existing = itemDatabase[i];
            if (existing == null)
                continue;

            if (existing == item || ItemKey(existing) == key)
                return;
        }

        itemDatabase.Add(item);
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

    public int RoomFor(ItemData item)
    {
        if (item == null || slots == null)
            return 0;

        int maxStack = Mathf.Max(1, item.maxStack);
        int room = 0;
        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlotData slot = slots[i];
            if (slot == null)
                continue;

            if (slot.item == null || slot.amount <= 0)
                room += maxStack;
            else if (slot.item == item && slot.amount < maxStack)
                room += maxStack - slot.amount;
        }

        return room;
    }

    public bool CanAdd(ItemData item, int amount)
    {
        if (amount <= 0)
            return true;

        return RoomFor(item) >= amount;
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

    void ApplyCarry()
    {
        if (!hasCarry || carryAmounts == null || carryIds == null)
            return;

        while (slots.Count < carryAmounts.Length)
            slots.Add(new InventorySlotData());

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null)
                slots[i] = new InventorySlotData();

            if (i >= carryAmounts.Length || carryAmounts[i] <= 0 || string.IsNullOrEmpty(carryIds[i]))
            {
                slots[i].Clear();
                continue;
            }

            ItemData item = FindItem(carryIds[i]);
            if (item == null)
            {
                Debug.LogWarning("Không tìm thấy item id: " + carryIds[i]);
                slots[i].Clear();
                continue;
            }

            slots[i].item = item;
            slots[i].amount = carryAmounts[i];
            bool priced = carrySellSet != null && i < carrySellSet.Length && carrySellSet[i];
            slots[i].sellPriceSet = priced;
            slots[i].coinPriceSell = priced && carrySell != null && i < carrySell.Length
                ? carrySell[i]
                : 0;
        }

        if (InventoryUI.Instance != null && InventoryUI.Instance.gameObject.scene == gameObject.scene)
            InventoryUI.Instance.Refresh();
    }

    void ClearSlots()
    {
        if (slots == null)
            return;

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null)
                continue;

            slots[i].Clear();
        }
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
        if (slot != null)
            slot.Clear();
    }

    private void RefreshUI()
    {
        Capture();
        if (InventoryUI.Instance != null && InventoryUI.Instance.gameObject.scene == gameObject.scene)
            InventoryUI.Instance.Refresh();
    }
}
