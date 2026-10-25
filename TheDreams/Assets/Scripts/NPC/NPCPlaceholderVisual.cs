using UnityEngine;

public class NPCPlaceholderVisual : MonoBehaviour
{
    [SerializeField] Color color = new Color(0.85f, 0.35f, 0.7f, 1f);
    [SerializeField] Vector2 pixels = new Vector2(16f, 32f);
    [SerializeField] float pixelsPerUnit = 16f;

    void Awake()
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer == null || renderer.sprite != null)
            return;

        int width = Mathf.Max(1, Mathf.RoundToInt(pixels.x));
        int height = Mathf.Max(1, Mathf.RoundToInt(pixels.y));
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;

        Color[] fill = new Color[width * height];
        for (int i = 0; i < fill.Length; i++)
            fill[i] = color;

        texture.SetPixels(fill);
        texture.Apply();

        renderer.sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 0f),
            pixelsPerUnit
        );
        renderer.spriteSortPoint = SpriteSortPoint.Pivot;
    }
}
