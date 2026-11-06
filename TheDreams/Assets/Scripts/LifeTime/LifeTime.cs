using System;
using UnityEngine;

public class LifeTime : MonoBehaviour
{
    public static LifeTime Instance { get; private set; }

    public static event Action OnDayChanged;

    [Tooltip("Gio luc vao game (0-24). 8.75 = 08:45.")]
    [SerializeField] float dayStartHour = 8.75f;

    [Tooltip("Giay that cho 1 gio trong game.")]
    [SerializeField] float realSecondsPerGameHour = 1f;

    [SerializeField] int daysPerMonth = 28;
    [SerializeField] int monthsPerYear = 12;

    float hour;
    int day = 1;
    int month = 1;
    int year = 1;
    bool started;
    bool ticked;

    public float Hour => hour;
    public int Day => day;
    public int Month => month;
    public int Year => year;
    public int DaysPerMonth => Mathf.Max(1, daysPerMonth);
    public int MonthsPerYear => Mathf.Max(1, monthsPerYear);
    public float DayStartHour => Mathf.Clamp(dayStartHour, 0f, 24f);
    public float RealSecondsPerGameHour =>
        realSecondsPerGameHour <= 0.01f ? 1f : realSecondsPerGameHour;

    /// <summary>So ngay tuyet doi tu ngay 1/1 nam thu 1 (0-based).</summary>
    public int AbsoluteDay => ToAbsoluteDay(year, month, day);

    public string ClockText => FormatHour(hour);
    public string DateText => FormatDate(day, month, year);

    public static float HourNow => Ensure().Hour;
    public static int DayNow => Ensure().Day;
    public static int MonthNow => Ensure().Month;
    public static int YearNow => Ensure().Year;
    public static int AbsoluteDayNow => Ensure().AbsoluteDay;

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

    public static void SetCalendar(int yearValue, int monthValue, int dayValue, float hourValue)
    {
        LifeTime clock = Ensure();
        clock.ApplyCalendar(yearValue, monthValue, dayValue);
        clock.hour = Mathf.Clamp(hourValue, 0f, 24f);
        clock.started = true;
        clock.ticked = true;
    }

    public static void AdvanceDay()
    {
        Ensure().StepDay();
    }

    public static void ResetCalendar()
    {
        LifeTime clock = Ensure();
        clock.year = 1;
        clock.month = 1;
        clock.day = 1;
        clock.hour = clock.DayStartHour;
        clock.started = true;
        clock.ticked = true;
        OnDayChanged?.Invoke();
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

    public static string FormatDate(int dayValue, int monthValue, int yearValue)
    {
        return "Ngay " + dayValue + "  Thang " + monthValue + "  Nam thu " + yearValue;
    }

    public static string FormatDay(int dayValue) => "Ngay " + dayValue;
    public static string FormatMonth(int monthValue) => "Thang " + monthValue;
    public static string FormatYear(int yearValue) => "Nam thu " + yearValue;

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

    public int ToAbsoluteDay(int yearValue, int monthValue, int dayValue)
    {
        int y = Mathf.Max(1, yearValue) - 1;
        int m = Mathf.Clamp(monthValue, 1, MonthsPerYear) - 1;
        int d = Mathf.Clamp(dayValue, 1, DaysPerMonth) - 1;
        return y * MonthsPerYear * DaysPerMonth + m * DaysPerMonth + d;
    }

    void ApplyCalendar(int yearValue, int monthValue, int dayValue)
    {
        year = Mathf.Max(1, yearValue);
        month = Mathf.Clamp(monthValue, 1, MonthsPerYear);
        day = Mathf.Clamp(dayValue, 1, DaysPerMonth);
    }

    void StepDay()
    {
        day++;
        if (day > DaysPerMonth)
        {
            day = 1;
            month++;
            if (month > MonthsPerYear)
            {
                month = 1;
                year++;
            }
        }

        OnDayChanged?.Invoke();
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
        if (step <= 0f)
            return;

        hour += step;
        ticked = true;
        while (hour >= 24f)
        {
            hour -= 24f;
            StepDay();
        }
    }
}
