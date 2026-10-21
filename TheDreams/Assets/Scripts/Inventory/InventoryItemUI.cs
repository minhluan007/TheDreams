using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItemUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private Button button;

    public void SetData(InventorySlotData data, Action onClick = null)
    {
        bool hasItem = data != null
            && data.item != null
            && data.amount > 0;

        if (icon != null)
        {
            icon.sprite = hasItem ? data.item.icon : null;
            icon.enabled = hasItem && data.item.icon != null;
        }

        if (amountText != null)
        {
            amountText.text = hasItem && data.amount > 1
                ? $"x{data.amount}"
                : "";
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();

            if (hasItem && onClick != null)
                button.onClick.AddListener(() => onClick());
        }
    }
}