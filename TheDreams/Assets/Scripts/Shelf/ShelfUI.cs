
using UnityEngine;
using UnityEngine.InputSystem;
public class ShelfUI : MonoBehaviour
{
    public static ShelfUI Instance;

    [SerializeField] private GameObject shelfPanel;
    [SerializeField] private Transform shelfGrid;
    [SerializeField] private Transform inventoryGrid;
    [SerializeField] private InventoryItemUI itemPrefab;
    [SerializeField] private InventoryUI inventoryUI;
    

    private ShelfInventory currentShelf;

    public bool IsOpen =>
        shelfPanel != null && shelfPanel.activeSelf;

    private void Awake()
    {
        Instance = this;
        shelfPanel.SetActive(false);
    }

    
    private void Update()
    {
        if (!IsOpen || Keyboard.current == null)
            return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
    }


    public void Open(ShelfInventory shelf)
    {
        if (shelf == null || InventoryManager.Instance == null)
            return;

        currentShelf = shelf;
        shelfPanel.SetActive(true);
        inventoryUI?.Open();

        // Mở UI kệ thì ẩn InventoryPanel độc lập nếu đang mở.
        // Hai vùng item sẽ được hiển thị trong ShelfPanel.
        Refresh();
    }

    public void Close()
    {
        shelfPanel.SetActive(false);
        currentShelf = null;
    }

    public void SelectShelf(ShelfInventory shelf)
    {
        if (shelf == null)
            return;

        currentShelf = shelf;
        Refresh();
    }

    public void Refresh()
    {
        if (currentShelf == null ||
            InventoryManager.Instance == null ||
            itemPrefab == null ||
            shelfGrid == null ||
            inventoryGrid == null)
            return;

        ClearGrid(shelfGrid);

        for (int i = 0; i < currentShelf.slots.Count; i++)
        {
            int index = i;

            var ui = Instantiate(itemPrefab, shelfGrid);

            ui.SetData(
                currentShelf.slots[index],
                () => MoveFromShelf(index)
            );
        }

        ClearGrid(inventoryGrid);

        for (int i = 0; i < InventoryManager.Instance.slots.Count; i++)
        {
            int index = i;

            var ui = Instantiate(itemPrefab, inventoryGrid);

            ui.SetData(
                InventoryManager.Instance.slots[index],
                () => MoveFromInventory(index)
            );
        }
    }

    public void MoveFromShelf(int index)
    {
        if (currentShelf == null)
            return;

        bool moved = InventoryTransfer.Move(
            currentShelf.slots,
            index,
            InventoryManager.Instance.slots
        );

        if (moved)
            Refresh();
        else
            Debug.Log("Inventory không đủ chỗ nhận item.");
    }

    public void MoveFromInventory(int index)
    {
        if (currentShelf == null)
            return;

        bool moved = InventoryTransfer.Move(
            InventoryManager.Instance.slots,
            index,
            currentShelf.slots
        );

        if (moved)
            Refresh();
        else
            Debug.Log("Kệ không đủ chỗ nhận item.");
    }

    private void ClearGrid(Transform grid)
    {
        foreach (Transform child in grid)
            Destroy(child.gameObject);
    }
}
