using UnityEngine;

public class CheckoutTable : MonoBehaviour
{
    public const string ShelfPointName = "WP_Shelf";
    public const string CounterPointName = "WP_CheckoutCounter";
    public const string TableObjectName = "Table_Checkout";

    ItemData item;
    string placedBy;
    SpriteRenderer icon;

    public ItemData Item => item;

    public static Transform FindPoint(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return null;

        GameObject point = GameObject.Find(objectName.Trim());
        return point != null ? point.transform : null;
    }

    public static CheckoutTable FindTable()
    {
        Transform table = FindPoint(TableObjectName);
        if (table == null)
            return null;

        CheckoutTable checkout = table.GetComponent<CheckoutTable>();
        if (checkout == null)
            checkout = table.gameObject.AddComponent<CheckoutTable>();

        return checkout;
    }

    public void Place(ItemData next, string npcId)
    {
        item = next;
        placedBy = npcId;
        EnsureIcon();
        icon.sprite = next != null ? next.icon : null;
        icon.enabled = icon.sprite != null;
    }

    public void ClearOwned(string npcId)
    {
        if (string.IsNullOrEmpty(npcId) || placedBy != npcId)
            return;

        Clear();
    }

    public void Clear()
    {
        item = null;
        placedBy = null;
        if (icon == null)
            return;

        icon.sprite = null;
        icon.enabled = false;
    }

    void EnsureIcon()
    {
        if (icon != null)
            return;

        Transform child = transform.Find("Checkout_Item");
        GameObject go = child != null ? child.gameObject : new GameObject("Checkout_Item");
        if (child == null)
        {
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
        }

        icon = go.GetComponent<SpriteRenderer>();
        if (icon == null)
            icon = go.AddComponent<SpriteRenderer>();

        if (go.GetComponent<SortY>() == null)
            go.AddComponent<SortY>();
    }
}
