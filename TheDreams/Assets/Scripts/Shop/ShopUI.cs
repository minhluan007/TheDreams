using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    public static ShopUI Instance;

    [SerializeField] private GameObject panel;

    private Shop current;
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

    public bool IsShowing(Shop shop)
    {
        return IsOpen && current == shop;
    }

    public void Toggle(Shop shop)
    {
        if (shop == null)
            return;

        if (IsShowing(shop))
            Close();
        else
            Open(shop);
    }

    public void Open(Shop shop)
    {
        if (shop == null)
            return;

        ResolvePanel();
        if (panel == null)
            return;

        ActivateUp(panel);
        panel.transform.SetAsLastSibling();
        current = shop;
        EnsureShell();
        Rebuild(shop);
    }

    public void Close()
    {
        current = null;
        if (panel != null)
            panel.SetActive(false);
    }

    public void Buy(ShopOffer offer)
    {
        if (!IsOpen || offer == null || offer.item == null)
            return;

        int price = offer.Price;
        if (!PlayerCoin.CanAfford(price))
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

        int added = InventoryManager.Instance.AddItem(offer.item, 1);
        if (added <= 0)
        {
            SetStatus("Inventory đã đầy.", false);
            return;
        }

        PlayerCoin.TrySpend(price);
        SetStatus("Đã mua " + offer.DisplayName + ".", true);
        RefreshCoins();
    }

    private void Rebuild(Shop shop)
    {
        if (titleLabel != null)
            titleLabel.text = shop.Title;

        SetStatus("Bấm Mua. Esc để đóng.", true);
        RefreshCoins();
        ClearRows();

        IReadOnlyList<ShopOffer> offers = shop.Offers;
        if (offers == null || listContent == null)
            return;

        bool any = false;
        for (int i = 0; i < offers.Count; i++)
        {
            ShopOffer offer = offers[i];
            if (offer == null || offer.item == null)
                continue;

            rows.Add(CreateRow(offer));
            any = true;
        }

        if (!any)
            SetStatus("Shop chưa có item.", false);

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
        {
            OfferRow row = rows[i];
            if (row.offer == null)
                continue;

            bool canBuy = PlayerCoin.CanAfford(row.offer.Price);
            if (row.priceLabel != null)
            {
                row.priceLabel.text = row.offer.Price + " coin";
                row.priceLabel.color = canBuy
                    ? new Color(1f, 0.9f, 0.45f, 1f)
                    : new Color(0.9f, 0.4f, 0.35f, 1f);
            }

            if (row.buyImage != null)
            {
                row.buyImage.color = canBuy
                    ? new Color(0.29f, 0.48f, 0.24f, 1f)
                    : new Color(0.28f, 0.28f, 0.28f, 1f);
            }
        }
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

        RectTransform view = CreateRect(panel.transform, "ShopView", layer);
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
            "Cửa hàng",
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

    private OfferRow CreateRow(ShopOffer offer)
    {
        int layer = panel.layer;
        RectTransform row = CreateRect(listContent, "Offer", layer);
        Image background = row.gameObject.AddComponent<Image>();
        background.color = new Color(0.08f, 0.07f, 0.05f, 0.45f);
        background.raycastTarget = false;

        HorizontalLayoutGroup rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.padding = new RectOffset(2, 2, 2, 2);
        rowLayout.spacing = 3f;
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;

        Image icon = CreateImage(row, "Icon", layer);
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

        RectTransform info = CreateRect(row, "Info", layer);
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

        TMP_Text priceLabel = CreateText(
            info,
            "Price",
            offer.Price + " coin",
            6f,
            FontStyles.Bold,
            TextAlignmentOptions.MidlineLeft,
            layer);
        priceLabel.color = new Color(1f, 0.9f, 0.45f, 1f);
        priceLabel.textWrappingMode = TextWrappingModes.NoWrap;
        priceLabel.overflowMode = TextOverflowModes.Ellipsis;
        AddLineHeight(priceLabel, 7f);

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

        ShopOffer captured = offer;
        Button buyButton = CreateButton(row, "Buy", "Mua", layer, () => Buy(captured));
        LayoutElement buyLayout = buyButton.gameObject.AddComponent<LayoutElement>();
        buyLayout.preferredWidth = 30f;
        buyLayout.minWidth = 30f;
        buyLayout.preferredHeight = 14f;
        buyLayout.minHeight = 14f;

        return new OfferRow
        {
            root = row.gameObject,
            offer = offer,
            priceLabel = priceLabel,
            buyImage = buyButton.targetGraphic as Image
        };
    }

    private void ResolvePanel()
    {
        if (panel != null)
            return;

        Canvas canvas = GetComponentInParent<Canvas>(true);
        if (canvas == null)
            return;

        Transform found = canvas.transform.Find("ShoppingPanel");
        if (found != null)
            panel = found.gameObject;
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
        public ShopOffer offer;
        public TMP_Text priceLabel;
        public Image buyImage;
    }
}
