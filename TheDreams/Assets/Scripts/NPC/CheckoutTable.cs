using System.Collections.Generic;
using UnityEngine;

public class CheckoutTable : MonoBehaviour
{
    public const string ShelfPointName = "WP_Shelf";
    public const string CounterPointName = "WP_CheckoutCounter";
    public const string TableObjectName = "Table_Checkout";

    const float IconGap = 0.42f;

    readonly List<ItemData> shown = new List<ItemData>();
    readonly List<SpriteRenderer> icons = new List<SpriteRenderer>();
    string placedBy;

    public ItemData Item => shown.Count > 0 ? shown[0] : null;

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

    public void Show(List<ItemData> next, string npcId)
    {
        shown.Clear();
        if (next != null)
        {
            for (int i = 0; i < next.Count; i++)
            {
                if (next[i] != null)
                    shown.Add(next[i]);
            }
        }

        placedBy = npcId;
        while (icons.Count < shown.Count)
            icons.Add(CreateIcon(icons.Count));

        float origin = shown.Count > 1 ? -IconGap * (shown.Count - 1) * 0.5f : 0f;
        for (int i = 0; i < icons.Count; i++)
        {
            bool visible = i < shown.Count && shown[i].icon != null;
            icons[i].enabled = visible;
            icons[i].sprite = visible ? shown[i].icon : null;
            if (visible)
                icons[i].transform.localPosition = new Vector3(origin + IconGap * i, 0f, 0f);
        }
    }

    public void ClearOwned(string npcId)
    {
        if (string.IsNullOrEmpty(npcId) || placedBy != npcId)
            return;

        Clear();
    }

    public void Clear()
    {
        shown.Clear();
        placedBy = null;
        for (int i = 0; i < icons.Count; i++)
        {
            icons[i].sprite = null;
            icons[i].enabled = false;
        }
    }

    SpriteRenderer CreateIcon(int index)
    {
        string name = index == 0 ? "Checkout_Item" : "Checkout_Item_" + (index + 1);
        Transform child = transform.Find(name);
        GameObject go = child != null ? child.gameObject : new GameObject(name);
        if (child == null)
        {
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
        }

        SpriteRenderer icon = go.GetComponent<SpriteRenderer>();
        if (icon == null)
            icon = go.AddComponent<SpriteRenderer>();

        if (go.GetComponent<SortY>() == null)
            go.AddComponent<SortY>();

        return icon;
    }
}
