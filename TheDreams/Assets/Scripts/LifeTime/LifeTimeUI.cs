using TMPro;
using UnityEngine;

public class LifeTimeUI : MonoBehaviour
{
    [Tooltip("Gio luc vao game (0-24). 8.75 = 08:45.")]
    [SerializeField] float dayStartHour = 8.75f;

    [Tooltip("Giay that cho 1 gio trong game.")]
    [SerializeField] float realSecondsPerGameHour = 1f;

    [SerializeField] TMP_Text timeLabel;

    void Awake()
    {
        LifeTime.Ensure().ApplySettings(dayStartHour, realSecondsPerGameHour);
        if (timeLabel == null)
        {
            GameObject panel = GameObject.Find("LifeTimePanel");
            if (panel != null)
                timeLabel = panel.GetComponentInChildren<TMP_Text>(true);
        }

        Refresh();
    }

    void Update()
    {
        Refresh();
    }

    void Refresh()
    {
        if (timeLabel == null)
            return;

        string text = LifeTime.FormatHour(LifeTime.HourNow);
        if (timeLabel.text == text)
            return;

        timeLabel.text = text;
    }
}
