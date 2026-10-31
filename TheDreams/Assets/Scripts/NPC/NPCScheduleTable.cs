using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(
    fileName = "NPCSchedule",
    menuName = "The Dreams/NPC/Schedule Table"
)]
public class NPCScheduleTable : ScriptableObject
{
    public const string ResourcePath = "NPCSchedule";
    public const string AssetPath = "Assets/NPC/DataSO/Resources/NPCSchedule.asset";

    [Tooltip("Gio luc vao game (0-24). NPC chi xuat hien khi dong ho toi cot xuat hien.")]
    [SerializeField] float dayStartHour = 8.75f;

    [Tooltip("Giay that cho 1 gio trong game. 20 = tu 8:00 toi 9:00 mat 20 giay.")]
    [SerializeField] float realSecondsPerGameHour = 20f;

    [TextArea(8, 20)]
    [Tooltip("Moi dong 1 chang cua NPC. Cung id, nhieu dong = mot ngay, dung thu tu.\nCot cach nhau bang |. Dong bat dau bang # bi bo qua.\n\nid | prefab | map | spawn | di | destroy | xuat hien | roi di | hanh vi\n\nHanh vi: di, xem, chon, mua, dung, ra. Cach nhau dau phay.\nDiem di cach nhau dau phay. Gio de trong hoac - neu di tiep ngay.\nSau khi sua text, bam Nap text vao bang.")]
    [SerializeField] string sheet;

    public float DayStartHour => Mathf.Clamp(dayStartHour, 0f, 24f);

    public float RealSecondsPerGameHour =>
        realSecondsPerGameHour <= 0.01f ? 20f : realSecondsPerGameHour;

    [SerializeField] NPCScheduleEntry[] entries = Array.Empty<NPCScheduleEntry>();

    public IReadOnlyList<NPCScheduleEntry> Entries =>
        entries ?? Array.Empty<NPCScheduleEntry>();

    public static NPCScheduleTable[] LoadAll()
    {
        NPCScheduleTable[] tables = Resources.LoadAll<NPCScheduleTable>(string.Empty);
        if (tables != null && tables.Length > 0)
            return tables;

#if UNITY_EDITOR
        NPCScheduleTable table = AssetDatabase.LoadAssetAtPath<NPCScheduleTable>(AssetPath);
        if (table != null)
            return new[] { table };
#endif
        return Array.Empty<NPCScheduleTable>();
    }

    [ContextMenu("Nap text vao bang")]
    public void ApplySheet()
    {
        entries = ParseSheet(sheet, entries);
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    [ContextMenu("Xuat bang ra text")]
    public void CopyEntriesToSheet()
    {
        sheet = BuildSheet(entries);
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    public static string BuildSheet(NPCScheduleEntry[] source)
    {
        if (source == null || source.Length == 0)
            return "# id | prefab | map | spawn | di | destroy | xuat hien | roi di | hanh vi";

        var lines = new List<string>(source.Length + 1)
        {
            "# id | prefab | map | spawn | di | destroy | xuat hien | roi di | hanh vi"
        };

        for (int i = 0; i < source.Length; i++)
        {
            NPCScheduleEntry entry = source[i];
            if (entry == null)
                continue;

            lines.Add(string.Join(" | ", new[]
            {
                entry.npcId,
                entry.prefabName,
                entry.mapName,
                entry.spawnPointName,
                entry.walkPoints,
                entry.destroyPointName,
                entry.appearTime,
                entry.leaveTime,
                entry.actions
            }));
        }

        return string.Join("\n", lines);
    }

    public static NPCScheduleEntry[] ParseSheet(string text, NPCScheduleEntry[] previous)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<NPCScheduleEntry>();

        string[] lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var parsed = new List<NPCScheduleEntry>();

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (line.Length == 0 || line.StartsWith("#"))
                continue;

            string[] cells = line.Split('|');
            if (cells.Length < 4)
                continue;

            string id = Cell(cells, 0);
            if (id.Equals("id", StringComparison.OrdinalIgnoreCase))
                continue;

            if (string.IsNullOrEmpty(id))
                id = "npc_" + (parsed.Count + 1).ToString("00");

            string prefabName = Cell(cells, 1);
            string mapName = Cell(cells, 2);
            if (string.IsNullOrEmpty(mapName))
                continue;

            var entry = new NPCScheduleEntry
            {
                npcId = id,
                prefabName = prefabName,
                mapName = mapName,
                spawnPointName = Cell(cells, 3),
                walkPoints = Cell(cells, 4),
                destroyPointName = Cell(cells, 5),
                appearTime = Cell(cells, 6),
                leaveTime = Cell(cells, 7),
                actions = Cell(cells, 8),
                prefab = FindPrefab(id, mapName, prefabName, previous)
            };
            parsed.Add(entry);
        }

        return parsed.ToArray();
    }

    static string Cell(string[] cells, int index)
    {
        if (index < 0 || index >= cells.Length)
            return string.Empty;

        return cells[index].Trim();
    }

    static GameObject FindPrefab(string npcId, string mapName, string prefabName, NPCScheduleEntry[] previous)
    {
        if (previous != null)
        {
            for (int i = 0; i < previous.Length; i++)
            {
                NPCScheduleEntry entry = previous[i];
                if (entry == null || entry.prefab == null)
                    continue;

                if (entry.npcId == npcId && entry.mapName == mapName)
                    return entry.prefab;
            }
        }

        if (string.IsNullOrEmpty(prefabName) || prefabName == "-")
            return null;

        GameObject resource = Resources.Load<GameObject>(prefabName);
        if (resource != null)
            return resource;

#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets(prefabName + " t:Prefab");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null && prefab.name == prefabName)
                return prefab;
        }
#endif
        return null;
    }
}

[Serializable]
public class NPCScheduleEntry
{
    public string npcId = "customer_01";

    [Tooltip("Prefab NPC. De trong thi spawn NPC mac dinh.")]
    public GameObject prefab;

    [Tooltip("Ten prefab, dung khi nap tu text. Vi du: NPC")]
    public string prefabName = "NPC";

    [Tooltip("Ten scene. Vi du: BaseStore, InStore")]
    public string mapName = "BaseStore";

    [Tooltip("Ten object diem spawn trong scene. Vi du: Spawn_FromSubway")]
    public string spawnPointName = "Spawn_FromSubway";

    [Tooltip("Cac diem di, cach nhau dau phay. Vi du: WP_Street_1, WP_Street_2, WP_Door")]
    public string walkPoints = "WP_Street_1, WP_Street_2, WP_Door";

    [Tooltip("Ten object de NPC toi do roi Destroy. Vi du: Exit_ToStore")]
    public string destroyPointName = "Exit_ToStore";

    [Tooltip("Gio xuat hien. 8, 8:30, hoac de trong / - neu chua co dong ho.")]
    public string appearTime = "";

    [Tooltip("Gio roi map. De trong / - thi xong hanh vi moi di. Toi gio nay thi cat ngang, ra cua.")]
    public string leaveTime = "";

    [Tooltip("di, xem, chon, mua, dung, ra. Trong thi mac dinh di, ra.")]
    public string actions = "";

    public float AppearHour => NPCScheduleTime.ParseHour(appearTime);
    public float LeaveHour => NPCScheduleTime.ParseHour(leaveTime);

    public string[] WalkPointNames => NPCScheduleTime.SplitNames(walkPoints);

    string[] actionCache;
    string actionCacheSource;

    public string[] ActionNames
    {
        get
        {
            if (actionCache == null || actionCacheSource != actions)
            {
                actionCacheSource = actions;
                actionCache = NPCScheduleTime.ActionsOf(actions);
            }

            return actionCache;
        }
    }

    public bool MatchesMap(string sceneName)
    {
        return !string.IsNullOrEmpty(mapName)
            && string.Equals(mapName.Trim(), sceneName, StringComparison.Ordinal);
    }

    public bool IsInsideWindow(float? hour)
    {
        if (!hour.HasValue)
            return true;

        float appear = AppearHour;
        float leave = LeaveHour;
        if (appear >= 0f && hour.Value < appear)
            return false;

        if (leave >= 0f && hour.Value >= leave)
            return false;

        return true;
    }
}

public static class NPCScheduleTime
{
    public static float ParseHour(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return -1f;

        text = text.Trim();
        if (text == "-" || text == "x" || text == "X")
            return -1f;

        int colon = text.IndexOf(':');
        if (colon >= 0)
        {
            if (!float.TryParse(text.Substring(0, colon), NumberStyles.Float, CultureInfo.InvariantCulture, out float hours))
                return -1f;

            float minutes = 0f;
            if (colon + 1 < text.Length)
                float.TryParse(text.Substring(colon + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out minutes);

            return ClampHour(hours + minutes / 60f);
        }

        string normalized = text.Replace(',', '.');
        if (!float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out float hour))
            return -1f;

        return ClampHour(hour);
    }

    public static string[] SplitNames(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Trim() == "-")
            return Array.Empty<string>();

        string[] parts = text.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        var names = new List<string>(parts.Length);
        for (int i = 0; i < parts.Length; i++)
        {
            string name = parts[i].Trim();
            if (name.Length > 0)
                names.Add(name);
        }

        return names.ToArray();
    }

    public static string[] ActionsOf(string text)
    {
        string[] parts = SplitNames(text);
        if (parts.Length == 0)
            return new[] { "di", "ra" };

        var actions = new string[parts.Length];
        for (int i = 0; i < parts.Length; i++)
            actions[i] = NormalizeAction(parts[i]);

        return actions;
    }

    public static string NormalizeAction(string action)
    {
        if (string.IsNullOrWhiteSpace(action))
            return "di";

        string value = action.Trim().ToLowerInvariant().Replace('_', ' ');
        if (value.StartsWith("chon") || value.StartsWith("chọn"))
            return "chon";
        if (value.StartsWith("mua"))
            return "mua";
        if (value.StartsWith("xem"))
            return "xem";
        if (value.StartsWith("dung") || value.StartsWith("đứng"))
            return "dung";
        if (value == "ra" || value.StartsWith("roi") || value.StartsWith("rời") || value.StartsWith("ra "))
            return "ra";
        if (value == "di" || value.StartsWith("di ") || value.StartsWith("đi"))
            return "di";

        return value;
    }

    static float ClampHour(float hour)
    {
        if (hour < 0f)
            return -1f;

        return Mathf.Clamp(hour, 0f, 24f);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(NPCScheduleTable))]
public class NPCScheduleTableEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        NPCScheduleTable table = (NPCScheduleTable)target;

        if (GUILayout.Button("Nap text vao bang"))
        {
            Undo.RecordObject(table, "Nap NPC schedule");
            table.ApplySheet();
            EditorUtility.SetDirty(table);
        }

        if (GUILayout.Button("Xuat bang ra text"))
        {
            Undo.RecordObject(table, "Xuat NPC schedule");
            table.CopyEntriesToSheet();
            EditorUtility.SetDirty(table);
        }
    }
}
#endif
