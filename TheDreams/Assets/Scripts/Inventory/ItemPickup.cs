using System.Collections.Generic;
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
    private string takenKey;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        ApplyVisual();

        if (item == null)
            return;

        takenKey = PickupMemory.Key(gameObject.scene.name, transform.position, item);
        if (!PickupMemory.IsTaken(takenKey))
            return;

        Suppress();
        Destroy(gameObject);
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
        if (amount > 0)
            return;

        if (!string.IsNullOrEmpty(takenKey))
            PickupMemory.Take(takenKey);

        Destroy(gameObject);
    }

    public static void HideTaken()
    {
        ItemPickup[] pickups = FindObjectsByType<ItemPickup>(FindObjectsSortMode.None);
        for (int i = 0; i < pickups.Length; i++)
        {
            ItemPickup pickup = pickups[i];
            if (pickup == null || pickup.item == null)
                continue;

            string key = PickupMemory.Key(
                pickup.gameObject.scene.name,
                pickup.transform.position,
                pickup.item);

            if (!PickupMemory.IsTaken(key))
                continue;

            pickup.Suppress();
            Destroy(pickup.gameObject);
        }
    }

    void Suppress()
    {
        playerInRange = false;
        enabled = false;
        Collider2D body = GetComponent<Collider2D>();
        if (body != null)
            body.enabled = false;
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

public static class PickupMemory
{
    static readonly HashSet<string> taken = new HashSet<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetSession()
    {
        taken.Clear();
    }

    public static string Key(string sceneName, Vector3 position, ItemData item)
    {
        int x = Mathf.RoundToInt(position.x * 100f);
        int y = Mathf.RoundToInt(position.y * 100f);
        return sceneName + "|" + x + "|" + y + "|" + InventoryManager.ItemKey(item);
    }

    public static bool IsTaken(string key)
    {
        return !string.IsNullOrEmpty(key) && taken.Contains(key);
    }

    public static void Take(string key)
    {
        if (!string.IsNullOrEmpty(key))
            taken.Add(key);
    }

    public static void Clear()
    {
        taken.Clear();
    }

    public static string[] Export()
    {
        string[] keys = new string[taken.Count];
        taken.CopyTo(keys);
        return keys;
    }

    public static void Replace(string[] keys)
    {
        taken.Clear();
        if (keys == null)
            return;

        for (int i = 0; i < keys.Length; i++)
        {
            if (!string.IsNullOrEmpty(keys[i]))
                taken.Add(keys[i]);
        }
    }
}
