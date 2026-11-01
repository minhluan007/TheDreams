using TMPro;
using UnityEngine;

public class PlayerCoinUI : MonoBehaviour
{
    [SerializeField] TMP_Text label;

    void Awake()
    {
        if (label == null)
            label = FindOrCreateLabel();

        Refresh();
    }

    void OnEnable()
    {
        PlayerCoin.Changed += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        PlayerCoin.Changed -= Refresh;
    }

    void Refresh()
    {
        if (label == null)
            return;

        string text = PlayerCoin.Amount.ToString();
        if (label.text == text)
            return;

        label.text = text;
    }

    TMP_Text FindOrCreateLabel()
    {
        Transform canvas = transform.parent;
        Transform panel = canvas != null ? canvas.Find("PlayerCoinPanel") : null;
        if (panel == null)
        {
            GameObject found = GameObject.Find("PlayerCoinPanel");
            if (found != null)
                panel = found.transform;
        }

        if (panel == null)
            return null;

        Transform existing = panel.Find("CoinText");
        if (existing != null)
        {
            TMP_Text current = existing.GetComponent<TMP_Text>();
            if (current != null)
                return current;
        }

        return CreateLabel(panel);
    }

    static TMP_Text CreateLabel(Transform panel)
    {
        GameObject textObject = new GameObject(
            "CoinText",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.layer = panel.gameObject.layer;
        textObject.transform.SetParent(panel, false);

        RectTransform rect = (RectTransform)textObject.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(4f, 1f);
        rect.offsetMax = new Vector2(-4f, -1f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = 8f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(1f, 0.9f, 0.45f, 1f);
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        if (TMP_Settings.defaultFontAsset != null)
            text.font = TMP_Settings.defaultFontAsset;

        return text;
    }
}
