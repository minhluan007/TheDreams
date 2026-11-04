using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SupplierUI : MonoBehaviour
{
    public static SupplierUI Instance;

    [SerializeField] private GameObject panel;

    private Supplier current;
    private RectTransform listContent;
    private TMP_Text titleLabel;
    private TMP_Text coinLabel;
    private TMP_Text statusLabel;
    private bool shellReady;
    private readonly List<OfferRow> rows = new();

    public bool IsOpen => panel != null && panel.activeSelf;

    private void Awake()
    {
        Instance = this;
        ResolvePanel();
        if (panel != null)
            panel.SetActive(false);
    }

    private void OnEnable()
    {
        PlayerCoin.Changed += RefreshCoins;
    }

    private void OnDisable()
    {
        PlayerCoin.Changed -= RefreshCoins;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (!IsOpen || Keyboard.current == null)
            return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
    }

    public bool IsShowing(Supplier supplier)
    {
        return IsOpen && current == supplier;
    }

    public void Toggle(Supplier supplier)
    {
        if (supplier == null)
            return;

        if (IsShowing(supplier))
            Close();
        else
            Open(supplier);
    }

    public void Open(Supplier supplier)
    {
        if (supplier == null)
            return;

        ResolvePanel();
        if (panel == null)
            return;

        ActivateUp(panel);
        panel.transform.SetAsLastSibling();
        current = supplier;
        EnsureShell();
        Rebuild(supplier);
    }

    public void Close()
    {
        current = null;
        if (panel != null)
            panel.SetActive(false);
    }

    public void Buy(SupplierOffer offer, int quantity)
    {
        if (!IsOpen || offer == null || offer.item == null)
            return;

        SupplierQuote quote = offer.Quote(quantity);
        if (!PlayerCoin.CanAfford(quote.totalPrice))
        {
            SetStatus("Không đủ coin.", false);
            RefreshPrices();
            return;
        }

        if (InventoryManager.Instance == null)
        {
            SetStatus("Chưa có inventory.", false);
            return;
        }

        if (!Fits(quote, offer.item))
        {
            SetStatus("Inventory đã đầy.", false);
            RefreshPrices();
            return;
        }

        if (!PlayerCoin.TrySpend(quote.totalPrice))
        {
            SetStatus("Không đủ coin.", false);
            RefreshPrices();
            return;
        }

        InventoryManager.Instance.AddItem(offer.item, quote.quantity);
        for (int i = 0; i < quote.giftItems.Count; i++)
            InventoryManager.Instance.AddItem(quote.giftItems[i], quote.giftAmounts[i]);

        SetStatus(Receipt(offer, quote), true);
        RefreshCoins();
    }

    private void Rebuild(Supplier supplier)
    {
        if (titleLabel != null)
            titleLabel.text = supplier.Title;

        SetStatus("Chọn số lượng rồi bấm Mua. Esc để đóng.", true);
        RefreshCoins();
        ClearRows();

        IReadOnlyList<SupplierOffer> offers = supplier.Offers;
        if (offers == null || listContent == null)
            return;

        bool any = false;
        for (int i = 0; i < offers.Count; i++)
        {
            SupplierOffer offer = offers[i];
            if (offer == null || offer.item == null)
                continue;

            rows.Add(CreateRow(offer));
            any = true;
        }

        if (!any)
            SetStatus("Supplier chưa có item.", false);

        LayoutRebuilder.ForceRebuildLayoutImmediate(listContent);
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(listContent);
        RefreshPrices();
    }

    private void RefreshCoins()
    {
        if (coinLabel != null)
            coinLabel.text = PlayerCoin.Amount + " coin";

        RefreshPrices();
    }

    private void RefreshPrices()
    {
        for (int i = 0; i < rows.Count; i++)
            RefreshRow(rows[i]);
    }

    private void RefreshRow(OfferRow row)
    {
        if (row == null || row.offer == null)
            return;

        SupplierQuote quote = row.offer.Quote(row.quantity);
        bool canBuy = PlayerCoin.CanAfford(quote.totalPrice) && Fits(quote, row.offer.item);

        if (row.priceLabel != null)
        {
            row.priceLabel.text = PriceText(quote);
            row.priceLabel.color = canBuy
                ? new Color(1f, 0.9f, 0.45f, 1f)
                : new Color(0.9f, 0.4f, 0.35f, 1f);
        }

        if (row.quantityLabel != null)
            row.quantityLabel.text = quote.quantity.ToString();

        SupplierDeal discount = row.offer.ActiveDiscount(quote.quantity);
        IReadOnlyList<SupplierDeal> deals = row.offer.deals;
        for (int i = 0; i < row.dealLabels.Count; i++)
        {
            TMP_Text label = row.dealLabels[i];
            if (label == null)
                continue;

            SupplierDeal deal = deals != null && i < deals.Count ? deals[i] : null;
            bool active = deal != null && quote.quantity >= deal.MinQuantity && (deal == discount || deal.IsGift);
            bool broken = deal == null || (!deal.IsDiscount && !deal.IsGift);
            if (broken)
                label.color = new Color(0.9f, 0.4f, 0.35f, 1f);
            else if (deal.kind == SupplierDealKind.Gift)
                label.color = active
                    ? new Color(0.65f, 0.95f, 0.55f, 1f)
                    : new Color(0.5f, 0.62f, 0.46f, 1f);
            else
                label.color = active
                    ? new Color(1f, 0.86f, 0.4f, 1f)
                    : new Color(0.72f, 0.64f, 0.4f, 1f);
        }

        if (row.buyImage != null)
        {
            row.buyImage.color = canBuy
                ? new Color(0.29f, 0.48f, 0.24f, 1f)
                : new Color(0.28f, 0.28f, 0.28f, 1f);
        }

        if (row.minusImage != null)
        {
            row.minusImage.color = row.quantity > 1
                ? new Color(0.35f, 0.3f, 0.22f, 1f)
                : new Color(0.22f, 0.2f, 0.18f, 1f);
        }

        if (row.plusImage != null)
        {
            row.plusImage.color = row.quantity < SupplierOffer.MaxQuantity
                ? new Color(0.35f, 0.3f, 0.22f, 1f)
                : new Color(0.22f, 0.2f, 0.18f, 1f);
        }
    }

    private void ChangeQuantity(OfferRow row, int delta)
    {
        if (row == null)
            return;

        int next = Mathf.Clamp(row.quantity + delta, 1, SupplierOffer.MaxQuantity);
        if (next == row.quantity)
            return;

        row.quantity = next;
        RefreshRow(row);
    }

    private void ClearRows()
    {
        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i].root != null)
                DestroyImmediate(rows[i].root);
        }

        rows.Clear();

        if (listContent == null)
            return;

        for (int i = listContent.childCount - 1; i >= 0; i--)
            DestroyImmediate(listContent.GetChild(i).gameObject);
    }

    private void EnsureShell()
    {
        if (shellReady || panel == null)
            return;

        shellReady = true;
        int layer = panel.layer;

        RectTransform view = CreateRect(panel.transform, "SupplierView", layer);
        Stretch(view, 6f, 6f, 6f, 6f);

        RectTransform header = CreateRect(view, "Header", layer);
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(0.5f, 1f);
        header.sizeDelta = new Vector2(0f, 14f);
        header.anchoredPosition = Vector2.zero;

        titleLabel = CreateText(
            header,
            "Title",
            "Nhà cung cấp",
            8f,
            FontStyles.Bold,
            TextAlignmentOptions.MidlineLeft,
            layer);
        Stretch(titleLabel.rectTransform, 0f, 0f, 70f, 0f);

        coinLabel = CreateText(
            header,
            "Coin",
            PlayerCoin.Amount + " coin",
            6f,
            FontStyles.Bold,
            TextAlignmentOptions.MidlineRight,
            layer);
        coinLabel.color = new Color(1f, 0.9f, 0.45f, 1f);
        SetRect(coinLabel.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f));
        coinLabel.rectTransform.offsetMin = new Vector2(-64f, 0f);
        coinLabel.rectTransform.offsetMax = new Vector2(-16f, 0f);

        Button closeButton = CreateButton(header, "Close", "X", layer, Close);
        RectTransform closeRect = (RectTransform)closeButton.transform;
        closeRect.anchorMin = new Vector2(1f, 0.5f);
        closeRect.anchorMax = new Vector2(1f, 0.5f);
        closeRect.pivot = new Vector2(1f, 0.5f);
        closeRect.anchoredPosition = Vector2.zero;
        closeRect.sizeDelta = new Vector2(12f, 12f);

        RectTransform list = CreateRect(view, "List", layer);
        Stretch(list, 0f, 12f, 0f, 16f);

        RectTransform viewport = CreateRect(list, "Viewport", layer);
        Stretch(viewport, 0f, 0f, 0f, 0f);
        viewport.gameObject.AddComponent<RectMask2D>();
        Image viewportHit = viewport.gameObject.AddComponent<Image>();
        viewportHit.color = new Color(1f, 1f, 1f, 0.01f);
        viewportHit.raycastTarget = true;

        listContent = CreateRect(viewport, "Content", layer);
        listContent.anchorMin = new Vector2(0f, 1f);
        listContent.anchorMax = new Vector2(1f, 1f);
        listContent.pivot = new Vector2(0.5f, 1f);
        listContent.anchoredPosition = Vector2.zero;
        listContent.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup layout = listContent.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(1, 1, 1, 1);
        layout.spacing = 3f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = listContent.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = list.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = listContent;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 16f;
        scroll.inertia = false;

        statusLabel = CreateText(
            view,
            "Status",
            "",
            5f,
            FontStyles.Normal,
            TextAlignmentOptions.MidlineLeft,
            layer);
        SetRect(statusLabel.rectTransform, Vector2.zero, new Vector2(1f, 0f), new Vector2(0.5f, 0f));
        statusLabel.rectTransform.sizeDelta = new Vector2(0f, 12f);
        statusLabel.rectTransform.anchoredPosition = Vector2.zero;
        statusLabel.color = new Color(0.9f, 0.88f, 0.8f, 1f);
    }

    private OfferRow CreateRow(SupplierOffer offer)
    {
        int layer = panel.layer;
        OfferRow row = new OfferRow
        {
            offer = offer,
            quantity = 1
        };

        RectTransform root = CreateRect(listContent, "Offer", layer);
        row.root = root.gameObject;
        Image background = root.gameObject.AddComponent<Image>();
        background.color = new Color(0.08f, 0.07f, 0.05f, 0.45f);
        background.raycastTarget = false;

        HorizontalLayoutGroup rowLayout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.padding = new RectOffset(2, 2, 2, 2);
        rowLayout.spacing = 3f;
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;

        Image icon = CreateImage(root, "Icon", layer);
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.sprite = offer.item.icon;
        icon.enabled = offer.item.icon != null;
        icon.color = Color.white;
        LayoutElement iconLayout = icon.gameObject.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = 20f;
        iconLayout.preferredHeight = 20f;
        iconLayout.minWidth = 20f;
        iconLayout.minHeight = 20f;

        RectTransform info = CreateRect(root, "Info", layer);
        LayoutElement infoLayout = info.gameObject.AddComponent<LayoutElement>();
        infoLayout.flexibleWidth = 1f;
        infoLayout.minWidth = 40f;

        VerticalLayoutGroup infoGroup = info.gameObject.AddComponent<VerticalLayoutGroup>();
        infoGroup.spacing = 0f;
        infoGroup.childAlignment = TextAnchor.UpperLeft;
        infoGroup.childControlWidth = true;
        infoGroup.childControlHeight = true;
        infoGroup.childForceExpandWidth = true;
        infoGroup.childForceExpandHeight = false;

        TMP_Text nameLabel = CreateText(
            info,
            "Name",
            offer.DisplayName,
            7f,
            FontStyles.Bold,
            TextAlignmentOptions.MidlineLeft,
            layer);
        nameLabel.textWrappingMode = TextWrappingModes.NoWrap;
        nameLabel.overflowMode = TextOverflowModes.Ellipsis;
        AddLineHeight(nameLabel, 8f);

        row.priceLabel = CreateText(
            info,
            "Price",
            offer.UnitPrice + " coin",
            6f,
            FontStyles.Bold,
            TextAlignmentOptions.MidlineLeft,
            layer);
        row.priceLabel.color = new Color(1f, 0.9f, 0.45f, 1f);
        row.priceLabel.textWrappingMode = TextWrappingModes.NoWrap;
        row.priceLabel.overflowMode = TextOverflowModes.Ellipsis;
        AddLineHeight(row.priceLabel, 7f);

        IReadOnlyList<SupplierDeal> deals = offer.deals;
        if (deals != null)
        {
            for (int i = 0; i < deals.Count; i++)
            {
                SupplierDeal deal = deals[i];
                if (deal == null)
                {
                    row.dealLabels.Add(null);
                    continue;
                }

                TMP_Text dealLabel = CreateText(
                    info,
                    "Deal",
                    deal.Label,
                    5f,
                    FontStyles.Bold,
                    TextAlignmentOptions.MidlineLeft,
                    layer);
                dealLabel.textWrappingMode = TextWrappingModes.NoWrap;
                dealLabel.overflowMode = TextOverflowModes.Ellipsis;
                AddLineHeight(dealLabel, 6f);
                row.dealLabels.Add(dealLabel);
            }
        }

        List<string> lines = SplitLines(offer.Description);
        for (int i = 0; i < lines.Count; i++)
        {
            string line = lines[i];
            TMP_Text lineLabel = CreateText(
                info,
                "Line",
                string.IsNullOrEmpty(line) ? " " : line,
                5f,
                FontStyles.Normal,
                TextAlignmentOptions.MidlineLeft,
                layer);
            lineLabel.color = new Color(0.86f, 0.82f, 0.74f, 1f);
            lineLabel.textWrappingMode = TextWrappingModes.NoWrap;
            lineLabel.overflowMode = TextOverflowModes.Ellipsis;
            AddLineHeight(lineLabel, 6f);
        }

        RectTransform stepper = CreateRect(root, "Quantity", layer);
        HorizontalLayoutGroup stepperLayout = stepper.gameObject.AddComponent<HorizontalLayoutGroup>();
        stepperLayout.spacing = 1f;
        stepperLayout.childAlignment = TextAnchor.MiddleCenter;
        stepperLayout.childControlWidth = true;
        stepperLayout.childControlHeight = true;
        stepperLayout.childForceExpandWidth = false;
        stepperLayout.childForceExpandHeight = false;
        LayoutElement stepperElement = stepper.gameObject.AddComponent<LayoutElement>();
        stepperElement.preferredWidth = 42f;
        stepperElement.minWidth = 42f;
        stepperElement.preferredHeight = 14f;

        Button minus = CreateButton(stepper, "Minus", "-", layer, () => ChangeQuantity(row, -1));
        LayoutElement minusLayout = minus.gameObject.AddComponent<LayoutElement>();
        minusLayout.preferredWidth = 12f;
        minusLayout.minWidth = 12f;
        minusLayout.preferredHeight = 14f;
        row.minusImage = minus.targetGraphic as Image;

        row.quantityLabel = CreateText(
            stepper,
            "Count",
            "1",
            6f,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            layer);
        LayoutElement countLayout = row.quantityLabel.gameObject.AddComponent<LayoutElement>();
        countLayout.preferredWidth = 16f;
        countLayout.minWidth = 16f;
        countLayout.preferredHeight = 14f;

        Button plus = CreateButton(stepper, "Plus", "+", layer, () => ChangeQuantity(row, 1));
        LayoutElement plusLayout = plus.gameObject.AddComponent<LayoutElement>();
        plusLayout.preferredWidth = 12f;
        plusLayout.minWidth = 12f;
        plusLayout.preferredHeight = 14f;
        row.plusImage = plus.targetGraphic as Image;

        Button buyButton = CreateButton(root, "Buy", "Mua", layer, () => Buy(row.offer, row.quantity));
        LayoutElement buyLayout = buyButton.gameObject.AddComponent<LayoutElement>();
        buyLayout.preferredWidth = 28f;
        buyLayout.minWidth = 28f;
        buyLayout.preferredHeight = 14f;
        buyLayout.minHeight = 14f;
        row.buyImage = buyButton.targetGraphic as Image;

        return row;
    }

    private void ResolvePanel()
    {
        if (panel != null)
            return;

        Canvas canvas = GetComponentInParent<Canvas>(true);
        if (canvas == null)
            return;

        Transform found = canvas.transform.Find("SupplierPanel");
        if (found != null)
            panel = found.gameObject;
    }

    private static bool Fits(SupplierQuote quote, ItemData item)
    {
        if (quote == null || item == null || InventoryManager.Instance == null)
            return false;

        int main = quote.quantity;
        for (int i = 0; i < quote.giftItems.Count; i++)
        {
            if (quote.giftItems[i] == item)
                main += quote.giftAmounts[i];
        }

        if (!InventoryManager.Instance.CanAdd(item, main))
            return false;

        for (int i = 0; i < quote.giftItems.Count; i++)
        {
            ItemData gift = quote.giftItems[i];
            if (gift == null || gift == item)
                continue;

            if (!InventoryManager.Instance.CanAdd(gift, quote.giftAmounts[i]))
                return false;
        }

        return true;
    }

    private static string PriceText(SupplierQuote quote)
    {
        if (quote.discountPercent > 0)
            return quote.totalPrice + " coin (-" + quote.discountPercent + "%)";

        if (quote.quantity <= 1)
            return quote.totalPrice + " coin";

        return quote.unitPrice + " x " + quote.quantity + " = " + quote.totalPrice + " coin";
    }

    private static string Receipt(SupplierOffer offer, SupplierQuote quote)
    {
        string message = "Đã mua " + quote.quantity + " " + offer.DisplayName;
        if (quote.discountPercent > 0)
            message += " (-" + quote.discountPercent + "%)";

        if (quote.giftItems.Count == 0)
            return message + ".";

        message += ". Tặng ";
        for (int i = 0; i < quote.giftItems.Count; i++)
        {
            if (i > 0)
                message += ", ";

            ItemData gift = quote.giftItems[i];
            string name = gift != null ? gift.Label : "quà";
            message += name + " x" + quote.giftAmounts[i];
        }

        return message + ".";
    }

    private void SetStatus(string message, bool ok)
    {
        if (statusLabel == null)
            return;

        statusLabel.text = message;
        statusLabel.color = ok
            ? new Color(0.82f, 0.9f, 0.7f, 1f)
            : new Color(1f, 0.55f, 0.42f, 1f);
    }

    private static List<string> SplitLines(string description)
    {
        List<string> lines = new();
        if (string.IsNullOrEmpty(description))
            return lines;

        string text = description.Replace("\r\n", "\n").Replace('\r', '\n');
        int end = text.Length;
        while (end > 0 && text[end - 1] == '\n')
            end--;

        int start = 0;
        for (int i = 0; i <= end; i++)
        {
            if (i != end && text[i] != '\n')
                continue;

            lines.Add(text.Substring(start, i - start));
            start = i + 1;
        }

        return lines;
    }

    private static void ActivateUp(GameObject leaf)
    {
        Transform node = leaf.transform;
        while (node != null)
        {
            if (!node.gameObject.activeSelf)
                node.gameObject.SetActive(true);

            node = node.parent;
        }
    }

    private static void AddLineHeight(TMP_Text text, float height)
    {
        LayoutElement element = text.gameObject.AddComponent<LayoutElement>();
        element.minHeight = height;
        element.preferredHeight = height;
    }

    private static RectTransform CreateRect(Transform parent, string name, int layer)
    {
        GameObject child = new GameObject(name, typeof(RectTransform));
        child.layer = layer;
        child.transform.SetParent(parent, false);
        return (RectTransform)child.transform;
    }

    private static Image CreateImage(Transform parent, string name, int layer)
    {
        GameObject child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        child.layer = layer;
        child.transform.SetParent(parent, false);
        return child.GetComponent<Image>();
    }

    private static TMP_Text CreateText(
        Transform parent,
        string name,
        string value,
        float size,
        FontStyles style,
        TextAlignmentOptions alignment,
        int layer)
    {
        GameObject child = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        child.layer = layer;
        child.transform.SetParent(parent, false);

        TextMeshProUGUI text = child.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        if (TMP_Settings.defaultFontAsset != null)
            text.font = TMP_Settings.defaultFontAsset;

        return text;
    }

    private static Button CreateButton(
        Transform parent,
        string name,
        string label,
        int layer,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject child = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        child.layer = layer;
        child.transform.SetParent(parent, false);

        Image image = child.GetComponent<Image>();
        image.color = new Color(0.29f, 0.48f, 0.24f, 1f);
        image.raycastTarget = true;

        Button button = child.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        TMP_Text text = CreateText(
            child.transform,
            "Text",
            label,
            6f,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            layer);
        Stretch(text.rectTransform, 0f, 0f, 0f, 0f);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return button;
    }

    private static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
    }

    private sealed class OfferRow
    {
        public GameObject root;
        public SupplierOffer offer;
        public int quantity;
        public TMP_Text priceLabel;
        public TMP_Text quantityLabel;
        public Image buyImage;
        public Image minusImage;
        public Image plusImage;
        public readonly List<TMP_Text> dealLabels = new();
    }
}
