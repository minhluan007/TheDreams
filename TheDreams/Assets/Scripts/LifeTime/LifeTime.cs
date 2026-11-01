using UnityEngine;

public class LifeTime : MonoBehaviour
{
    public static LifeTime Instance { get; private set; }

    [Tooltip("Gio luc vao game (0-24). 8.75 = 08:45.")]
    [SerializeField] float dayStartHour = 8.75f;

    [Tooltip("Giay that cho 1 gio trong game.")]
    [SerializeField] float realSecondsPerGameHour = 1f;

    float hour;
    bool started;
    bool ticked;

    public float Hour => hour;
    public float DayStartHour => Mathf.Clamp(dayStartHour, 0f, 24f);
    public float RealSecondsPerGameHour =>
        realSecondsPerGameHour <= 0.01f ? 1f : realSecondsPerGameHour;

    public string ClockText => FormatHour(hour);

    public static float HourNow => Ensure().Hour;

    public static LifeTime Ensure()
    {
        if (Instance != null)
            return Instance;

        var go = new GameObject("LifeTime");
        DontDestroyOnLoad(go);
        return go.AddComponent<LifeTime>();
    }

    public static void SetHour(float value)
    {
        LifeTime clock = Ensure();
        clock.hour = Mathf.Clamp(value, 0f, 24f);
        clock.started = true;
        clock.ticked = true;
    }

    public static string FormatHour(float value)
    {
        if (value >= 24f)
            return "24:00";

        value = Mathf.Clamp(value, 0f, 24f);
        int wholeHour = Mathf.FloorToInt(value);
        int minute = Mathf.FloorToInt((value - wholeHour) * 60f + 0.001f);
        if (minute >= 60)
        {
            minute -= 60;
            wholeHour++;
        }

        if (wholeHour >= 24)
            return "24:00";

        return wholeHour.ToString("00") + ":" + minute.ToString("00");
    }

    public void ApplySettings(float startHour, float secondsPerHour)
    {
        if (secondsPerHour > 0.01f)
            realSecondsPerGameHour = secondsPerHour;

        dayStartHour = Mathf.Clamp(startHour, 0f, 24f);
        if (!ticked)
        {
            hour = DayStartHour;
            started = true;
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (!started)
            ApplySettings(dayStartHour, realSecondsPerGameHour);

        float step = Time.deltaTime / RealSecondsPerGameHour;
        if (step > 0f && hour < 24f)
        {
            hour = Mathf.Min(24f, hour + step);
            ticked = true;
        }
    }
}
