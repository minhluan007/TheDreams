using UnityEngine;

public class NPCMapTraveler : MonoBehaviour
{
    public enum Phase
    {
        Browse,
        Leave
    }

    [SerializeField] bool transferOnMapExit = true;
    [SerializeField] bool walkToExitWhenRouteEnds = true;
    [SerializeField] float spawnIgnoreSeconds = 0.5f;
    [Tooltip("Gio ra (0-24). Am = ra ngay khi het route. Sau nay dong ho goi NPCManager.ReportHour.")]
    [SerializeField] float leaveHour = -1f;
    [SerializeField] string destroyPointName;

    NPC npc;
    NPCMove move;
    MapExit seekingExit;
    float ignoreUntil;
    bool armed;
    bool movingToDestroy;
    bool clockActive;

    public Phase CurrentPhase { get; private set; } = Phase.Browse;
    public float LeaveHour => leaveHour;

    void Awake()
    {
        npc = GetComponent<NPC>();
        move = GetComponent<NPCMove>();
    }

    void Start()
    {
        ArmAfterSpawn();
    }

    public void ArmAfterSpawn()
    {
        armed = false;
        seekingExit = null;
        CurrentPhase = Phase.Browse;
        ignoreUntil = Time.time + spawnIgnoreSeconds;
        move.ClearTarget();
    }

    public void Configure(float hour, string destroyName)
    {
        leaveHour = hour;
        destroyPointName = string.IsNullOrWhiteSpace(destroyName) ? null : destroyName.Trim();
    }

    public void ReportHour(float hour)
    {
        clockActive = true;
        if (leaveHour >= 0f && hour >= leaveHour)
            BeginLeave();
    }

    public void BeginLeave()
    {
        if (CurrentPhase == Phase.Leave)
            return;

        CurrentPhase = Phase.Leave;
        SeekExit();
    }

    void Update()
    {
        if (!armed && Time.time >= ignoreUntil)
            armed = true;

        if (CurrentPhase == Phase.Leave)
        {
            if (!movingToDestroy)
                SeekExit();

            if (movingToDestroy && move.HasReachedEnd)
                Depart();
            return;
        }

        if (clockActive && leaveHour >= 0f)
            return;

        if (!walkToExitWhenRouteEnds || !move.HasReachedEnd)
            return;

        BeginLeave();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!transferOnMapExit || !armed)
            return;

        if (CurrentPhase != Phase.Leave && !string.IsNullOrEmpty(destroyPointName))
            return;

        if (clockActive && leaveHour >= 0f && CurrentPhase != Phase.Leave)
            return;

        MapExit exit = other.GetComponent<MapExit>();
        if (exit == null)
            return;

        Depart();
    }

    void SeekExit()
    {
        Transform point = FindDestroyPoint();
        if (point == null)
        {
            Depart();
            return;
        }

        movingToDestroy = true;
        seekingExit = point.GetComponent<MapExit>();
        move.SetTarget(point.position);
    }

    Transform FindDestroyPoint()
    {
        if (!string.IsNullOrEmpty(destroyPointName))
        {
            GameObject named = GameObject.Find(destroyPointName);
            if (named != null)
                return named.transform;
        }

        MapExit exit = FindExitOnCurrentMap();
        return exit != null ? exit.transform : null;
    }

    void Depart()
    {
        seekingExit = null;
        if (npc != null)
            npc.LeaveMap();
    }

    MapExit FindExitOnCurrentMap()
    {
        MapExit[] exits = FindObjectsByType<MapExit>(FindObjectsSortMode.None);
        if (exits.Length == 0)
            return null;

        MapExit best = exits[0];
        float bestDist = Vector2.Distance(transform.position, best.transform.position);
        for (int i = 1; i < exits.Length; i++)
        {
            float dist = Vector2.Distance(transform.position, exits[i].transform.position);
            if (dist < bestDist)
            {
                best = exits[i];
                bestDist = dist;
            }
        }

        return best;
    }
}
