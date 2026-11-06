using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LifeTimeUI : MonoBehaviour
{
    [Tooltip("Gio luc vao game (0-24). 8.75 = 08:45.")]
    [SerializeField] float dayStartHour = 8.75f;

    [Tooltip("Giay that cho 1 gio trong game.")]
    [SerializeField] float realSecondsPerGameHour = 1f;

    [SerializeField] TMP_Text timeLabel;
    [SerializeField] TMP_Text dayLabel;
    [SerializeField] TMP_Text monthLabel;
    [SerializeField] TMP_Text yearLabel;

    string lastStamp;

    void Awake()
    {
        LifeTime.Ensure().ApplySettings(dayStartHour, realSecondsPerGameHour);
        EnsureLabels();
        Refresh(true);
    }

    void Update()
    {
        Refresh(false);
    }

    void EnsureLabels()
    {
        GameObject panel = GameObject.Find("LifeTimePanel");
        if (panel == null)
            return;

        RectTransform panelRect = panel.transform as RectTransform;
        if (panelRect != null)
        {
            panelRect.sizeDelta = new Vector2(150f, 58f);
            panelRect.anchoredPosition = new Vector2(-80f, -34f);
        }

        if (timeLabel == null)
            timeLabel = FindChildText(panel.transform, "TimeText");

        if (dayLabel == null)
            dayLabel = EnsureChildText(panel.transform, "DayText", new Vector2(0f, 18f), 12);

        if (monthLabel == null)
            monthLabel = EnsureChildText(panel.transform, "MonthText", new Vector2(0f, 6f), 12);

        if (yearLabel == null)
            yearLabel = EnsureChildText(panel.transform, "YearText", new Vector2(0f, -6f), 12);

        if (timeLabel != null)
        {
            RectTransform timeRect = timeLabel.rectTransform;
            timeRect.anchorMin = new Vector2(0f, 0f);
            timeRect.anchorMax = new Vector2(1f, 0f);
            timeRect.pivot = new Vector2(0.5f, 0f);
            timeRect.anchoredPosition = new Vector2(0f, 2f);
            timeRect.sizeDelta = new Vector2(0f, 16f);
            timeLabel.fontSize = 14;
            timeLabel.alignment = TextAlignmentOptions.Center;
        }

        Image background = panel.GetComponent<Image>();
        if (background != null)
            background.color = new Color(1f, 1f, 1f, 0.92f);
    }

    static TMP_Text FindChildText(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    static TMP_Text EnsureChildText(Transform parent, string childName, Vector2 anchoredPos, float fontSize)
    {
        Transform existing = parent.Find(childName);
        GameObject go;
        if (existing == null)
        {
            go = new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
        }
        else
        {
            go = existing.gameObject;
        }

        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(0f, 14f);

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        if (text.font == null)
        {
            TMP_Text sample = parent.GetComponentInChildren<TMP_Text>(true);
            if (sample != null)
            {
                text.font = sample.font;
                text.fontSharedMaterial = sample.fontSharedMaterial;
            }
        }

        return text;
    }

    void Refresh(bool force)
    {
        LifeTime clock = LifeTime.Ensure();
        string stamp = clock.Day + "|" + clock.Month + "|" + clock.Year + "|" + LifeTime.FormatHour(clock.Hour);
        if (!force && stamp == lastStamp)
            return;

        lastStamp = stamp;

        if (dayLabel != null)
            dayLabel.text = LifeTime.FormatDay(clock.Day);

        if (monthLabel != null)
            monthLabel.text = LifeTime.FormatMonth(clock.Month);

        if (yearLabel != null)
            yearLabel.text = LifeTime.FormatYear(clock.Year);

        if (timeLabel != null)
            timeLabel.text = LifeTime.FormatHour(clock.Hour);
    }
}
