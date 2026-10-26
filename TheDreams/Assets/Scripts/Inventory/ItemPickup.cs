using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class ItemPickup : MonoBehaviour
{
    [SerializeField] private ItemData item;
    [SerializeField] private int amount = 1;

    private SpriteRenderer spriteRenderer;
    private bool playerInRange;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        ApplyVisual();
    }

    private void ApplyVisual()
    {
        if (spriteRenderer == null)
            return;

        Sprite icon = item != null ? item.icon : null;
        spriteRenderer.sprite = icon;
        spriteRenderer.enabled = icon != null;
    }

    private void OnTriggerEnter2D(Collider2D other)
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
    }

    private void Update()
    {
        if (!playerInRange || item == null || amount <= 0)
            return;

        if (Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame)
            return;

        if (InventoryManager.Instance == null)
            return;

        int added = InventoryManager.Instance.AddItem(item, amount);
        if (added <= 0)
            return;

        amount -= added;
        if (amount <= 0)
            Destroy(gameObject);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (amount < 1)
            amount = 1;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        ApplyVisual();
    }
#endif
}
