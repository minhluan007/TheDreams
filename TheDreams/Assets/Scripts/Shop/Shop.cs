using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Shop : MonoBehaviour
{
    [SerializeField] private string shopTitle = "Cửa hàng";
    [SerializeField] private List<ShopOffer> offers = new();

    private bool playerInRange;

    public string Title =>
        string.IsNullOrWhiteSpace(shopTitle) ? "Cửa hàng" : shopTitle;

    public IReadOnlyList<ShopOffer> Offers => offers;

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

        if (ShopUI.Instance == null)
            return;

        ShopUI.Instance.Toggle(this);
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

        if (ShopUI.Instance != null && ShopUI.Instance.IsShowing(this))
            ShopUI.Instance.Close();
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

[Serializable]
public class ShopOffer
{
    public ItemData item;

    [Tooltip("Bật để bán theo giá riêng của shop này. Tắt thì dùng coinPrice trên Item.")]
    public bool overridePrice;

    [Min(0)]
    public int coinPrice;

    [TextArea(2, 8)]
    [Tooltip("Mỗi lần xuống dòng là một dòng trên UI. Để trống sẽ dùng description của Item.")]
    public string description;

    public int Price
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
}
