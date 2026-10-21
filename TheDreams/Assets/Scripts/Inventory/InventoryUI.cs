using UnityEngine;
using UnityEngine.InputSystem;
public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance;
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform itemGrid;
    [SerializeField] private InventoryItemUI itemPrefab;

    private void Start()
    {
        panel.SetActive(false);
    }

    public void Toggle()
    {
        Debug.Log("Toggle called");

        bool isOpen = panel.activeSelf;

        panel.SetActive(!isOpen);

        if(!isOpen)
        {
            Refresh();
        }

        Debug.Log("Panel active: " + panel.activeSelf);
    }

    public void Open()
    {
        panel.SetActive(true);
        Refresh();
    }

 
    public void Refresh()
    {
        foreach (Transform child in itemGrid)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < InventoryManager.Instance.slots.Count; i++)
        {
            int index = i;
            var slot = InventoryManager.Instance.slots[index];

            InventoryItemUI itemUI = Instantiate(itemPrefab, itemGrid);

            if (slot.item != null)
            {
                if (ShelfUI.Instance != null && ShelfUI.Instance.IsOpen)
                {
                    itemUI.SetData(
                        slot,
                        () => ShelfUI.Instance.MoveFromInventory(index)
                    );
                }
                else
                {
                    itemUI.SetData(slot);
                }
            }
            else
            {
                itemUI.SetData(slot);
            }
        }
    }

}