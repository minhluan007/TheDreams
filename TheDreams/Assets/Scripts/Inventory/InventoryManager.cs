using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    public List<InventorySlotData> slots = new();

    [SerializeField] private List<ItemData> itemDatabase = new();

    static string[] carryIds;
    static int[] carryAmounts;
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
        for (int i = 0; i < data.Length; i++)
        {
            SavedSlot saved = data[i];
            if (saved == null || saved.amount <= 0 || string.IsNullOrEmpty(saved.itemId))
                continue;

            carryIds[i] = saved.itemId;
            carryAmounts[i] = saved.amount;
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
        for (int i = 0; i < count; i++)
        {
            InventorySlotData slot = slots[i];
            if (slot == null || slot.item == null || slot.amount <= 0)
                continue;

            carryIds[i] = ItemKey(slot.item);
            carryAmounts[i] = slot.amount;
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
                amount = string.IsNullOrEmpty(carryIds[i]) ? 0 : carryAmounts[i]
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
                slots[i].item = null;
                slots[i].amount = 0;
                continue;
            }

            ItemData item = FindItem(carryIds[i]);
            if (item == null)
            {
                Debug.LogWarning("Không tìm thấy item id: " + carryIds[i]);
                slots[i].item = null;
                slots[i].amount = 0;
                continue;
            }

            slots[i].item = item;
            slots[i].amount = carryAmounts[i];
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

            slots[i].item = null;
            slots[i].amount = 0;
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
        slot.item = null;
        slot.amount = 0;
    }

    private void RefreshUI()
    {
        Capture();
        if (InventoryUI.Instance != null && InventoryUI.Instance.gameObject.scene == gameObject.scene)
            InventoryUI.Instance.Refresh();
    }
}
