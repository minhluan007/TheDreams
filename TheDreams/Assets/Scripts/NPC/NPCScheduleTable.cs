// Cot "hanh vi": nhieu action, cach nhau dau phay. De trong thi mac dinh: di, ra.
// Moi action nhap dung tu khoa, hoac nhap like (bat dau bang tu khoa).
// Khong phan biet hoa thuong. Dau _ duoc doi thanh dau cach truoc khi so.
// Vi du like: "xem hang" = xem, "mua do" = mua, "thanh toan" = tra, "roi di" = ra.
// mua / doi / tra bi bo qua neu khong chon duoc mon, hoac ke khong con hang.
//
// di    Di lan luot cac diem o cot "di", khong dung lai. Het diem thi sang action sau.
//       Dung: di. Like: "di ..." (phai co dau cach) hoac bat dau bang "đi". Vi du: di dao, đi dạo.
//       "dia" khong tinh la di, vi thieu dau cach sau "di".
// xem   Cung lo trinh voi di, nhung dung khoang 1.5s o moi diem de xem.
//       Dung: xem. Like: bat dau bang "xem". Vi du: xem hang, xem ke.
// chon  Dung truoc ke, chon 1 mon neu ke con hang. Chua lay mon, chua cong coin. Dung khoang 2s.
//       Dung: chon. Like: bat dau bang "chon" hoac "chọn". Vi du: chon mon, chọn hàng.
// mua   Toi ke, lay mon da chon khoi ke. Chua cong coin. Dung khoang 1.1s. Khong co mon thi bo qua.
//       Dung: mua. Like: bat dau bang "mua". Vi du: mua hang, mua do.
// doi   Toi quay thanh toan, dat mon len ban, dung doi khoang 3s. Khong cam mon thi bo qua.
//       Dung: doi. Like: bat dau bang "doi" hoac "đợi". Vi du: doi tra, đợi tính tiền.
// tra   Thanh toan va cong coin. Dung khoang 1.1s. Sang action khac thi lay mon khoi Table_Checkout.
//       Dung: tra. Like: bat dau bang "tra", "trả", hoac "thanh". Vi du: tra tien, trả tiền, thanh toan.
// dung  Dung yen tai cho khoang 1.4s.
//       Dung: dung. Like: bat dau bang "dung" hoac "đứng". Vi du: dung lai, đứng im.
// ra    Di toi diem destroy roi bien mat, ket thuc luot.
//       Dung: ra. Like: "ra ..." (phai co dau cach), hoac bat dau bang "roi" / "rời".
//       Vi du: ra cua, roi di, rời đi. "ranh" khong tinh la ra.
//
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

    [TextArea(8, 20)]
    [Tooltip("Moi dong 1 chang cua NPC. Cung id, nhieu dong = mot ngay, dung thu tu.\nCot cach nhau bang |. Dong bat dau bang # bi bo qua.\n\nid | prefab | map | spawn | di | destroy | xuat hien | roi di | hanh vi\n\nHanh vi: di, xem, chon, mua, doi, tra, dung, ra. Cach nhau dau phay.\nmua: lay mon o ke, chua cong coin. doi: toi WP_CheckoutCounter, dat mon len Table_Checkout, dung 3 giay. tra: thanh toan, cong coin. Sang action khac thi lay mon khoi ban.\nKhong co mua/doi/tra, hoac khong chon duoc mon, thi khach chi xem roi ve.\nDiem di cach nhau dau phay. Gio de trong hoac - neu di tiep ngay.\nNeu co gio roi di, xong hanh vi ma chua toi gio thi NPC di dao walkPoint ngau nhien, xen xem / dung / chon, toi gio moi ra cua.\nSau khi sua text, bam Nap text vao bang.")]
    [SerializeField] string sheet;

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

    [Tooltip("Gio roi map. De trong / - thi xong hanh vi moi ra cua. Con gio thi di dao, xem, dung, chon cho den gio do moi ra.")]
    public string leaveTime = "";

    [Tooltip("di, xem, chon, mua, doi, tra, dung, ra. Trong thi mac dinh di, ra. mua lay do. doi cho thanh toan. tra cong coin, sang action khac thi lay mon khoi ban. Bo qua mua/doi/tra neu khong chon mon.")]
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

    public bool CanAppearNow()
    {
        float appear = AppearHour;
        if (appear < 0f)
            return true;

        return LifeTime.HourNow + 0.001f >= appear;
    }

    public bool MustLeaveNow()
    {
        float leave = LeaveHour;
        return leave >= 0f && LifeTime.HourNow >= leave;
    }

    public bool IsInsideWindow(float? hour)
    {
        float value = hour ?? LifeTime.HourNow;
        float appear = AppearHour;
        float leave = LeaveHour;
        if (appear >= 0f && value < appear)
            return false;

        if (leave >= 0f && value >= leave)
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
        if (value.StartsWith("doi") || value.StartsWith("đợi"))
            return "doi";
        if (value.StartsWith("tra") || value.StartsWith("trả") || value.StartsWith("thanh"))
            return "tra";
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

    public static bool IsPurchaseAction(string action)
    {
        return action == "mua" || action == "doi" || action == "tra";
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
    static bool showActionNotes = true;
    static GUIStyle noteBody;

    static readonly string[][] ActionNotes =
    {
        new[]
        {
            "di",
            "Di lan luot cac diem o cot \"di\", khong dung lai. Het diem thi sang action sau.",
            "Dung: di. Like: \"di ...\" (phai co dau cach) hoac bat dau bang \"đi\". Vi du: di dao, đi dạo. \"dia\" khong tinh."
        },
        new[]
        {
            "xem",
            "Cung lo trinh voi di, nhung dung khoang 1.5s o moi diem de xem.",
            "Dung: xem. Like: bat dau bang \"xem\". Vi du: xem hang, xem ke."
        },
        new[]
        {
            "chon",
            "Dung truoc ke, chon 1 mon neu ke con hang. Chua lay mon, chua cong coin. Dung khoang 2s.",
            "Dung: chon. Like: bat dau bang \"chon\" hoac \"chọn\". Vi du: chon mon, chọn hàng."
        },
        new[]
        {
            "mua",
            "Toi ke, lay mon da chon khoi ke. Chua cong coin. Dung khoang 1.1s. Khong co mon thi bo qua.",
            "Dung: mua. Like: bat dau bang \"mua\". Vi du: mua hang, mua do."
        },
        new[]
        {
            "doi",
            "Toi quay thanh toan, dat mon len ban, dung doi khoang 3s. Khong cam mon thi bo qua.",
            "Dung: doi. Like: bat dau bang \"doi\" hoac \"đợi\". Vi du: doi tra, đợi tính tiền."
        },
        new[]
        {
            "tra",
            "Thanh toan va cong coin. Dung khoang 1.1s. Sang action khac thi lay mon khoi Table_Checkout.",
            "Dung: tra. Like: bat dau bang \"tra\", \"trả\", hoac \"thanh\". Vi du: tra tien, trả tiền, thanh toan."
        },
        new[]
        {
            "dung",
            "Dung yen tai cho khoang 1.4s.",
            "Dung: dung. Like: bat dau bang \"dung\" hoac \"đứng\". Vi du: dung lai, đứng im."
        },
        new[]
        {
            "ra",
            "Di toi diem destroy roi bien mat, ket thuc luot.",
            "Dung: ra. Like: \"ra ...\" (phai co dau cach), hoac bat dau bang \"roi\" / \"rời\". Vi du: ra cua, roi di, rời đi. \"ranh\" khong tinh."
        }
    };

    public override void OnInspectorGUI()
    {
        DrawActionNotes();
        EditorGUILayout.Space();
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

    static void DrawActionNotes()
    {
        showActionNotes = EditorGUILayout.Foldout(showActionNotes, "Ghi chu hanh vi", true);
        if (!showActionNotes)
            return;

        if (noteBody == null)
        {
            noteBody = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                fontSize = 11,
                richText = true
            };
        }

        EditorGUILayout.HelpBox(
            "Cot hanh vi: nhieu action, cach nhau dau phay. De trong = di, ra.\n" +
            "Nhap dung tu khoa, hoac nhap like (bat dau bang tu khoa).\n" +
            "Khong phan biet hoa thuong. Dau _ doi thanh dau cach.\n" +
            "Vi du like: \"xem hang\" = xem, \"mua do\" = mua, \"thanh toan\" = tra, \"roi di\" = ra.\n" +
            "mua / doi / tra bi bo qua neu khong chon duoc mon, hoac ke khong con hang.",
            MessageType.Info);

        for (int i = 0; i < ActionNotes.Length; i++)
        {
            string[] note = ActionNotes[i];
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(note[0], EditorStyles.boldLabel);
            EditorGUILayout.LabelField(note[1], noteBody);
            EditorGUILayout.LabelField(note[2], noteBody);
            EditorGUILayout.EndVertical();
        }
    }
}
#endif
