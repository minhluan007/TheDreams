using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItemUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text amountText;

    private InventorySlotData slotData;

    public void SetData(InventorySlotData data)
{
    if (data == null)
    {
        Debug.LogError("InventoryItemUI: data == null");
        return;
    }

    if (data.item == null)
    {
        Debug.LogError("InventoryItemUI: data.item == null");
        return;
    }

    if (icon == null)
    {
        Debug.LogError("InventoryItemUI: Icon chưa được gán!");
        return;
    }

    if (amountText == null)
    {
        Debug.LogError("InventoryItemUI: Amount chưa được gán!");
        return;
    }

    slotData = data;

    icon.sprite = data.item.icon;
    icon.enabled = data.item.icon != null;

    amountText.text = data.amount > 1
        ? $"x{data.amount}"
        : "";
}
}