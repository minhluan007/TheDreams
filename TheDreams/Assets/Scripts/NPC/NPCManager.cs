using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NPCManager : MonoBehaviour
{
    public static NPCManager Instance { get; private set; }

    readonly List<NPC> npcs = new List<NPC>();
    bool spawnDemoIfMissing = true;
    float? reportedHour;

    [Tooltip("Gio vao (0-24). Am = spawn ngay khi vao map. Chi chan khi da co ReportHour.")]
    [SerializeField] float spawnFromHour = -1f;
    [Tooltip("Gio ngung vao (0-24). Am = khong gioi han. Chi chan khi da co ReportHour.")]
    [SerializeField] float spawnUntilHour = -1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null)
            return;

        GameObject go = new GameObject("------ NPCManager ------");
        go.AddComponent<NPCManager>();
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
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        SpawnForScene(SceneManager.GetActiveScene());
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SpawnForScene(scene);
    }

    public static void ReportHour(float hour)
    {
        if (Instance == null)
            Bootstrap();

        Instance.reportedHour = hour;
        Instance.SpawnForScene(SceneManager.GetActiveScene());
        for (int i = 0; i < Instance.npcs.Count; i++)
        {
            NPC npc = Instance.npcs[i];
            if (npc != null)
                npc.Traveler.ReportHour(hour);
        }
    }

    public static bool Register(NPC npc)
    {
        if (Instance == null)
            Bootstrap();

        for (int i = 0; i < Instance.npcs.Count; i++)
        {
            NPC existing = Instance.npcs[i];
            if (existing != null && existing != npc && existing.NpcId == npc.NpcId
                && existing.gameObject.scene == npc.gameObject.scene)
                return false;
        }

        if (!Instance.npcs.Contains(npc))
            Instance.npcs.Add(npc);

        return true;
    }

    public static void Unregister(NPC npc)
    {
        if (Instance == null)
            return;

        Instance.npcs.Remove(npc);
    }

    void SpawnForScene(Scene scene)
    {
        for (int i = npcs.Count - 1; i >= 0; i--)
        {
            NPC npc = npcs[i];
            if (npc == null || npc.gameObject.scene != scene)
                npcs.RemoveAt(i);
        }

        NPCScheduleTable[] tables = NPCScheduleTable.LoadAll();
        if (tables.Length == 0)
        {
            SpawnDemo(scene);
            return;
        }

        for (int t = 0; t < tables.Length; t++)
        {
            IReadOnlyList<NPCScheduleEntry> rows = tables[t].Entries;
            for (int i = 0; i < rows.Count; i++)
            {
                NPCScheduleEntry entry = rows[i];
                if (entry == null || !entry.MatchesMap(scene.name))
                    continue;

                if (!entry.IsInsideWindow(reportedHour))
                    continue;

                if (HasNpc(entry.npcId, scene))
                    continue;

                SpawnEntry(entry);
            }
        }
    }

    void SpawnEntry(NPCScheduleEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.spawnPointName))
        {
            Debug.LogWarning("NPC '" + entry.npcId + "' thieu diem spawn.");
            return;
        }

        GameObject point = GameObject.Find(entry.spawnPointName.Trim());
        if (point == null)
        {
            Debug.LogWarning("NPC '" + entry.npcId + "' khong thay diem spawn '" + entry.spawnPointName + "' trong " + entry.mapName + ".");
            return;
        }

        NPC.CreateFromSchedule(entry, point.transform.position);
    }

    bool HasNpc(string npcId, Scene scene)
    {
        for (int i = 0; i < npcs.Count; i++)
        {
            NPC npc = npcs[i];
            if (npc != null && npc.NpcId == npcId && npc.gameObject.scene == scene)
                return true;
        }

        return false;
    }

    void SpawnDemo(Scene scene)
    {
        if (!spawnDemoIfMissing || !WithinSpawnWindow() || npcs.Count > 0)
            return;

        if (scene.name != "BaseStore" && scene.name != "InStore")
            return;

        Vector3 position = scene.name == "InStore"
            ? new Vector3(-2.26f, -6.5f, 0f)
            : new Vector3(2f, -28.58f, 0f);

        NPC.CreateDefault(position);
    }

    bool WithinSpawnWindow()
    {
        if (!reportedHour.HasValue)
            return true;

        if (spawnFromHour >= 0f && reportedHour.Value < spawnFromHour)
            return false;

        if (spawnUntilHour >= 0f && reportedHour.Value >= spawnUntilHour)
            return false;

        return true;
    }
}
