using UnityEngine;

public class ShelfSlotItem : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;

    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void UpdateSprite(Sprite newSprite)
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
            return;

        spriteRenderer.sprite = newSprite;
        spriteRenderer.enabled = newSprite != null;
    }
}
