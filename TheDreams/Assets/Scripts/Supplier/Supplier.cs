using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Supplier : MonoBehaviour
{
    [SerializeField] private string supplierTitle = "Nhà cung cấp";
    [SerializeField] private List<SupplierOffer> offers = new();

    private bool playerInRange;

    public string Title =>
        string.IsNullOrWhiteSpace(supplierTitle) ? "Nhà cung cấp" : supplierTitle;

    public IReadOnlyList<SupplierOffer> Offers => offers;

    private void Awake()
    {
        EnsureTrigger();
    }

    private void Update()
    {
        if (!playerInRange)
            return;

        if (Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame)
            return;

        if (SupplierUI.Instance == null)
            return;

        SupplierUI.Instance.Toggle(this);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = true;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = false;

        if (SupplierUI.Instance != null && SupplierUI.Instance.IsShowing(this))
            SupplierUI.Instance.Close();
    }

    private void EnsureTrigger()
    {
        Collider2D[] colliders = GetComponents<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null && colliders[i].isTrigger)
                return;
        }

        BoxCollider2D trigger = gameObject.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;

        BoxCollider2D body = null;
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] is BoxCollider2D box && !box.isTrigger)
            {
                body = box;
                break;
            }
        }

        if (body != null)
        {
            trigger.offset = body.offset;
            trigger.size = body.size + new Vector2(1.2f, 1.2f);
            return;
        }

        trigger.size = new Vector2(2.4f, 2.4f);
    }
}

public enum SupplierDealKind
{
    Discount,
    Gift
}

[Serializable]
public class SupplierDeal
{
    public SupplierDealKind kind;

    [Min(2)]
    [Tooltip("Mua từ số lượng này trở lên thì ưu đãi có hiệu lực.")]
    public int minQuantity = 5;

    [Range(0, 100)]
    [Tooltip("Dùng khi loại là Discount. Phần trăm giảm trên tổng tiền.")]
    public int discountPercent = 10;

    [Tooltip("Dùng khi loại là Gift.")]
    public ItemData giftItem;

    [Min(1)]
    public int giftAmount = 1;

    public int MinQuantity => Mathf.Max(2, minQuantity);

    public int DiscountPercent => Mathf.Clamp(discountPercent, 0, 100);

    public int GiftCount => Mathf.Max(1, giftAmount);

    public bool IsDiscount => kind == SupplierDealKind.Discount && DiscountPercent > 0;

    public bool IsGift => kind == SupplierDealKind.Gift && giftItem != null;

    public bool Applies(int quantity)
    {
        return quantity >= MinQuantity && (IsDiscount || IsGift);
    }

    public string Label
    {
        get
        {
            string prefix = "Mua " + MinQuantity + "+: ";
            if (kind == SupplierDealKind.Gift)
            {
                if (giftItem == null)
                    return prefix + "tặng (chưa chọn item)";

                return prefix + "tặng " + giftItem.Label + " x" + GiftCount;
            }

            return prefix + "giảm " + DiscountPercent + "%";
        }
    }
}

[Serializable]
public class SupplierOffer
{
    public const int MaxQuantity = 99;

    public ItemData item;

    [Tooltip("Bật để bán theo giá riêng của supplier này. Tắt thì dùng coinPrice trên Item.")]
    public bool overridePrice;

    [Min(0)]
    public int coinPrice;

    [TextArea(2, 8)]
    [Tooltip("Mỗi lần xuống dòng là một dòng trên UI. Để trống sẽ dùng description của Item.")]
    public string description;

    [Tooltip("Mỗi dòng là một mức mua số lượng lớn: giảm giá hoặc quà tặng. Giảm giá lấy mức cao nhất đạt được. Quà tặng cộng mọi mức đã đạt.")]
    public List<SupplierDeal> deals = new();

    public int UnitPrice
    {
        get
        {
            if (overridePrice)
                return Mathf.Max(0, coinPrice);

            if (item == null)
                return 0;

            return Mathf.Max(0, item.coinPrice);
        }
    }

    public string Description
    {
        get
        {
            if (!string.IsNullOrEmpty(description))
                return description;

            if (item == null)
                return "";

            return item.description ?? "";
        }
    }

    public string DisplayName
    {
        get
        {
            if (item == null)
                return "";

            return item.Label;
        }
    }

    public SupplierDeal ActiveDiscount(int quantity)
    {
        if (deals == null)
            return null;

        SupplierDeal best = null;
        int bestQty = -1;
        int bestIndex = -1;
        for (int i = 0; i < deals.Count; i++)
        {
            SupplierDeal deal = deals[i];
            if (deal == null || !deal.IsDiscount || quantity < deal.MinQuantity)
                continue;

            int qty = deal.MinQuantity;
            if (best == null || qty > bestQty || (qty == bestQty && i > bestIndex))
            {
                best = deal;
                bestQty = qty;
                bestIndex = i;
            }
        }

        return best;
    }

    public SupplierQuote Quote(int quantity)
    {
        quantity = Mathf.Clamp(quantity, 1, MaxQuantity);
        int unit = UnitPrice;
        long total = (long)unit * quantity;
        SupplierDeal discount = ActiveDiscount(quantity);
        int percent = discount != null ? discount.DiscountPercent : 0;
        if (percent > 0)
            total = total * (100 - percent) / 100;

        if (total > int.MaxValue)
            total = int.MaxValue;

        SupplierQuote quote = new SupplierQuote
        {
            quantity = quantity,
            unitPrice = unit,
            totalPrice = (int)Mathf.Max(0, total),
            discountPercent = percent
        };

        if (deals == null)
            return quote;

        for (int i = 0; i < deals.Count; i++)
        {
            SupplierDeal deal = deals[i];
            if (deal == null || !deal.IsGift || quantity < deal.MinQuantity)
                continue;

            int count = deal.GiftCount;
            int found = -1;
            for (int g = 0; g < quote.giftItems.Count; g++)
            {
                if (quote.giftItems[g] != deal.giftItem)
                    continue;

                found = g;
                break;
            }

            if (found >= 0)
                quote.giftAmounts[found] += count;
            else
            {
                quote.giftItems.Add(deal.giftItem);
                quote.giftAmounts.Add(count);
            }
        }

        return quote;
    }
}

public sealed class SupplierQuote
{
    public int quantity;
    public int unitPrice;
    public int totalPrice;
    public int discountPercent;
    public readonly List<ItemData> giftItems = new();
    public readonly List<int> giftAmounts = new();
}
