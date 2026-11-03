using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryDetailView : MonoBehaviour
{
    InventorySlotData current;
    RectTransform root;
    Image icon;
    TMP_Text nameLabel;
    TMP_Text descriptionLabel;
    TMP_Text coinLabel;
    TMP_InputField sellInput;
    bool built;
    bool ignoreInput;

    public void Build(RectTransform grid)
    {
        if (!built)
        {
            built = true;
            CreateView();
        }

        Fit(grid);
    }

    public void Show(InventorySlotData slot)
    {
        if (root == null)
            return;

        if (slot == null || !slot.HasItem)
        {
            current = null;
            root.gameObject.SetActive(false);
            return;
        }

        bool same = current == slot;
        current = slot;
        root.gameObject.SetActive(true);

        ItemData item = slot.item;
        if (icon != null)
        {
            icon.sprite = item.icon;
            icon.enabled = item.icon != null;
        }

        if (nameLabel != null)
            nameLabel.text = item.Label;

        if (descriptionLabel != null)
            descriptionLabel.text = item.description ?? "";

        if (coinLabel != null)
            coinLabel.text = "Coin  " + Mathf.Max(0, item.coinPrice);

        if (sellInput != null && (!same || !sellInput.isFocused))
        {
            ignoreInput = true;
            sellInput.text = slot.SellPrice.ToString();
            ignoreInput = false;
        }
    }

    public void Commit()
    {
        if (ignoreInput || current == null || !current.HasItem || sellInput == null)
            return;

        if (!int.TryParse(sellInput.text, out int price))
        {
            ignoreInput = true;
            sellInput.text = current.SellPrice.ToString();
            ignoreInput = false;
            return;
        }

        current.SetSellPrice(price);
    }

    void CreateView()
    {
        int layer = gameObject.layer;
        root = CreateRect("ItemDetail", transform, layer);
        root.gameObject.SetActive(false);

        Image line = CreateImage("Line", root, layer);
        line.color = new Color(0.55f, 0.78f, 1f, 0.9f);
        line.raycastTarget = false;
        SetTopBar(line.rectTransform, 1f);

        icon = CreateImage("Icon", root, layer);
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.enabled = false;
        SetTopLeft(icon.rectTransform, 0f, 2f, 16f, 16f);

        nameLabel = CreateText("Name", root, layer, 6f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        SetTopStretch(nameLabel.rectTransform, 20f, 2f, 16f);

        descriptionLabel = CreateText("Description", root, layer, 5f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        descriptionLabel.color = new Color(0.82f, 0.9f, 1f, 1f);
        descriptionLabel.textWrappingMode = TextWrappingModes.Normal;
        descriptionLabel.overflowMode = TextOverflowModes.Ellipsis;
        StretchBetween(descriptionLabel.rectTransform, 0f, 24f, 0f, 20f);

        coinLabel = CreateText("CoinPrice", root, layer, 5f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        coinLabel.color = new Color(1f, 0.9f, 0.45f, 1f);
        SetBottomStretch(coinLabel.rectTransform, 0f, 12f, 12f);

        TMP_Text sellLabel = CreateText("SellLabel", root, layer, 5f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        sellLabel.text = "Sell";
        SetBottomLeft(sellLabel.rectTransform, 0f, 0f, 28f, 12f);

        sellInput = CreateInput(root, layer);
        SetBottomLeft(sellInput.GetComponent<RectTransform>(), 30f, 0f, 48f, 12f);
        sellInput.onEndEdit.AddListener(OnSellEdited);
        sellInput.onValueChanged.AddListener(OnSellEdited);
    }

    void OnSellEdited(string raw)
    {
        if (ignoreInput)
            return;

        Commit();
        if (ShelfUI.Instance != null && ShelfUI.Instance.IsOpen)
            ShelfUI.Instance.RememberCurrent();
    }

    void Fit(RectTransform grid)
    {
        if (root == null || grid == null)
            return;

        RectTransform panelRect = (RectTransform)transform;
        float gridBottom = grid.anchoredPosition.y - grid.sizeDelta.y * grid.pivot.y;
        float panelBottom = -panelRect.sizeDelta.y * panelRect.pivot.y;
        float top = gridBottom - 3f;
        float height = top - (panelBottom + 4f);
        if (height < 36f)
            height = 36f;

        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 1f);
        root.anchoredPosition = new Vector2(grid.anchoredPosition.x, top);
        root.sizeDelta = new Vector2(grid.sizeDelta.x, height);
    }

    static RectTransform CreateRect(string name, Transform parent, int layer)
    {
        GameObject child = new GameObject(name, typeof(RectTransform));
        child.layer = layer;
        child.transform.SetParent(parent, false);
        return (RectTransform)child.transform;
    }

    static Image CreateImage(string name, Transform parent, int layer)
    {
        GameObject child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        child.layer = layer;
        child.transform.SetParent(parent, false);
        return child.GetComponent<Image>();
    }

    static TMP_Text CreateText(
        string name,
        Transform parent,
        int layer,
        float size,
        FontStyles style,
        TextAlignmentOptions alignment)
    {
        GameObject child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        child.layer = layer;
        child.transform.SetParent(parent, false);

        TextMeshProUGUI text = child.GetComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.enableAutoSizing = false;
        if (TMP_Settings.defaultFontAsset != null)
            text.font = TMP_Settings.defaultFontAsset;

        return text;
    }

    static TMP_InputField CreateInput(Transform parent, int layer)
    {
        GameObject child = new GameObject(
            "CoinPriceSell",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(TMP_InputField));
        child.layer = layer;
        child.transform.SetParent(parent, false);

        Image background = child.GetComponent<Image>();
        background.color = new Color(0.72f, 0.86f, 1f, 1f);
        background.raycastTarget = true;

        RectTransform viewport = CreateRect("Text Area", child.transform, layer);
        StretchFull(viewport, 1f, 1f, 1f, 1f);
        viewport.gameObject.AddComponent<RectMask2D>();

        TMP_Text text = CreateText("Text", viewport, layer, 5f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        text.color = new Color(0.06f, 0.12f, 0.24f, 1f);
        text.raycastTarget = false;
        StretchFull(text.rectTransform, 2f, 0f, 2f, 0f);

        TMP_InputField input = child.GetComponent<TMP_InputField>();
        input.textViewport = viewport;
        input.textComponent = text;
        input.pointSize = 5f;
        input.characterLimit = 6;
        input.contentType = TMP_InputField.ContentType.IntegerNumber;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.caretColor = new Color(0.06f, 0.12f, 0.24f, 1f);
        input.customCaretColor = true;
        input.selectionColor = new Color(0.25f, 0.5f, 0.85f, 0.35f);
        if (TMP_Settings.defaultFontAsset != null)
            input.fontAsset = TMP_Settings.defaultFontAsset;

        Navigation navigation = input.navigation;
        navigation.mode = Navigation.Mode.None;
        input.navigation = navigation;
        return input;
    }

    static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
    }

    static void SetBottomLeft(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
    }

    static void SetTopBar(RectTransform rect, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(0f, -height);
        rect.offsetMax = Vector2.zero;
    }

    static void SetTopStretch(RectTransform rect, float left, float y, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(left, -y - height);
        rect.offsetMax = new Vector2(0f, -y);
    }

    static void SetBottomStretch(RectTransform rect, float left, float y, float height)
    {
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.offsetMin = new Vector2(left, y);
        rect.offsetMax = new Vector2(0f, y + height);
    }

    static void StretchBetween(RectTransform rect, float left, float bottom, float right, float top)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    static void StretchFull(RectTransform rect, float left, float bottom, float right, float top)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
        rect.pivot = new Vector2(0.5f, 0.5f);
    }
}
