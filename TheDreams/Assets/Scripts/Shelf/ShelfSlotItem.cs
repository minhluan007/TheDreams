using TMPro;
using UnityEngine;

public class ShelfSlotItem : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;

    [Tooltip("Object con hiện giá trên kệ. Gắn SpriteRenderer vào đây khi thêm nền.")]
    [SerializeField] Transform priceRoot;

    TextMeshPro priceLabel;

    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        EnsurePrice();
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

    public void Show(Sprite icon, int price, bool hasItem)
    {
        UpdateSprite(hasItem ? icon : null);
        SetPrice(price, hasItem);
    }

    public void SetPrice(int price, bool visible)
    {
        EnsurePrice();
        if (priceRoot == null)
            return;

        if (visible && priceLabel != null)
            priceLabel.text = Mathf.Max(0, price).ToString();

        if (priceRoot.gameObject.activeSelf != visible)
            priceRoot.gameObject.SetActive(visible);

        if (visible)
            ApplySorting();
    }

    void EnsurePrice()
    {
        if (priceRoot == null)
        {
            Transform found = transform.Find("Price");
            if (found != null)
                priceRoot = found;
        }

        if (priceRoot == null)
        {
            var child = new GameObject("Price");
            child.transform.SetParent(transform, false);
            child.transform.localPosition = new Vector3(0f, -0.55f, 0f);
            child.SetActive(false);
            priceRoot = child.transform;
        }

        if (priceLabel == null)
            priceLabel = priceRoot.GetComponent<TextMeshPro>();

        if (priceLabel != null)
            return;

        priceLabel = priceRoot.gameObject.AddComponent<TextMeshPro>();
        if (TMP_Settings.defaultFontAsset != null)
            priceLabel.font = TMP_Settings.defaultFontAsset;

        priceLabel.alignment = TextAlignmentOptions.Center;
        priceLabel.fontSize = 2f;
        priceLabel.color = new Color(1f, 0.93f, 0.62f, 1f);
        priceLabel.outlineWidth = 0.2f;
        priceLabel.outlineColor = new Color32(42, 26, 16, 255);
        priceLabel.textWrappingMode = TextWrappingModes.NoWrap;
        priceLabel.overflowMode = TextOverflowModes.Overflow;
        priceLabel.rectTransform.sizeDelta = new Vector2(1.8f, 0.55f);
        priceLabel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
    }

    void ApplySorting()
    {
        int layerId = spriteRenderer != null ? spriteRenderer.sortingLayerID : 0;
        int order = spriteRenderer != null ? spriteRenderer.sortingOrder : 0;

        if (priceRoot != null)
        {
            SpriteRenderer[] backgrounds = priceRoot.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < backgrounds.Length; i++)
            {
                backgrounds[i].sortingLayerID = layerId;
                backgrounds[i].sortingOrder = order + 1;
            }
        }

        if (priceLabel == null)
            return;

        MeshRenderer mesh = priceLabel.GetComponent<MeshRenderer>();
        if (mesh == null)
            return;

        mesh.sortingLayerID = layerId;
        mesh.sortingOrder = order + 2;
    }
}
