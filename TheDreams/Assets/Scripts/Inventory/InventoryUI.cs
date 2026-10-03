using UnityEngine;
using UnityEngine.InputSystem;
public class InventoryUI : MonoBehaviour
{
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

        Debug.Log("Panel active: " + panel.activeSelf);
    }

    public void Refresh()
    {
        foreach (Transform child in itemGrid)
        {
            Destroy(child.gameObject);
        }

        foreach (var slot in InventoryManager.Instance.slots)
        {
            InventoryItemUI itemUI =
                Instantiate(itemPrefab, itemGrid);

            itemUI.SetData(slot);
        }
    }
}