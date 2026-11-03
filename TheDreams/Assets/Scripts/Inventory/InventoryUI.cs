using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance;
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform itemGrid;
    [SerializeField] private InventoryItemUI itemPrefab;

    InventorySlotData selected;
    InventoryDetailView detail;

    public InventorySlotData Selected => selected;

    private void Awake()
    {
        Instance = this;
        EnsureDetail();
    }

    private void Start()
    {
        panel.SetActive(false);
    }

    public void Toggle()
    {
        bool isOpen = panel.activeSelf;
        panel.SetActive(!isOpen);

        if (!isOpen)
            Refresh();
    }

    public void Open()
    {
        panel.SetActive(true);
        Refresh();
    }

    public void Select(InventorySlotData slot)
    {
        selected = slot != null && slot.HasItem ? slot : null;
        detail?.Show(selected);
        Highlight();
    }

    public void CommitSelectionPrice()
    {
        detail?.Commit();
        if (ShelfUI.Instance != null && ShelfUI.Instance.IsOpen)
            ShelfUI.Instance.RememberCurrent();
    }

    public void OnSlotsChanged()
    {
        if (selected != null && !selected.HasItem)
            selected = null;

        detail?.Show(selected);
        Highlight();
    }

    public void Refresh()
    {
        EnsureDetail();

        if (ShelfUI.Instance != null && ShelfUI.Instance.IsOpen)
        {
            ShelfUI.Instance.Refresh();
            return;
        }

        RebuildInventory();
        OnSlotsChanged();
    }

    void RebuildInventory()
    {
        if (itemGrid == null || InventoryManager.Instance == null || itemPrefab == null)
            return;

        for (int i = itemGrid.childCount - 1; i >= 0; i--)
            Destroy(itemGrid.GetChild(i).gameObject);

        for (int i = 0; i < InventoryManager.Instance.slots.Count; i++)
        {
            InventorySlotData slot = InventoryManager.Instance.slots[i];
            InventoryItemUI itemUI = Instantiate(itemPrefab, itemGrid);
            itemUI.SetData(slot, () => Select(slot), null, slot == selected);
        }
    }

    void EnsureDetail()
    {
        if (panel == null)
            return;

        if (detail == null)
            detail = panel.GetComponent<InventoryDetailView>();

        if (detail == null)
            detail = panel.AddComponent<InventoryDetailView>();

        detail.Build(itemGrid as RectTransform);
    }

    void Highlight()
    {
        InventoryItemUI.HighlightGrid(itemGrid, selected);
        if (ShelfUI.Instance != null)
            ShelfUI.Instance.HighlightShelf(selected);
    }
}
