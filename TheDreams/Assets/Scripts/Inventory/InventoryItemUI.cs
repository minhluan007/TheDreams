using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryItemUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private Button button;

    Image background;
    Action onSelect;
    Action onTransfer;

    public InventorySlotData Slot { get; private set; }

    void Awake()
    {
        Transform frame = transform.Find("Background");
        if (frame != null)
            background = frame.GetComponent<Image>();

        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        EventTrigger trigger = button.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = button.gameObject.AddComponent<EventTrigger>();

        trigger.triggers.Clear();
        EventTrigger.Entry entry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerClick
        };
        entry.callback.AddListener(OnPointerEvent);
        trigger.triggers.Add(entry);
    }

    public void SetData(
        InventorySlotData data,
        Action onSelect = null,
        Action onTransfer = null,
        bool selected = false)
    {
        Slot = data;
        this.onSelect = onSelect;
        this.onTransfer = onTransfer;

        bool hasItem = data != null && data.HasItem;

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
            button.onClick.RemoveAllListeners();

        SetSelected(selected && hasItem);
    }

    public void SetSelected(bool selected)
    {
        if (background == null)
            return;

        background.color = selected
            ? new Color(0.65f, 1f, 1f, 1f)
            : Color.white;
    }

    public static void HighlightGrid(Transform grid, InventorySlotData selected)
    {
        if (grid == null)
            return;

        for (int i = 0; i < grid.childCount; i++)
        {
            InventoryItemUI ui = grid.GetChild(i).GetComponent<InventoryItemUI>();
            if (ui != null)
                ui.SetSelected(selected != null && ui.Slot == selected);
        }
    }

    void OnPointerEvent(BaseEventData data)
    {
        if (data is PointerEventData pointer)
            HandleClick(pointer);
    }

    void HandleClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (Slot != null && Slot.HasItem)
                onTransfer?.Invoke();
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Left)
            onSelect?.Invoke();
    }
}
