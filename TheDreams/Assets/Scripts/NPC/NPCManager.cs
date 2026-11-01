using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NPCManager : MonoBehaviour
{
    public static NPCManager Instance { get; private set; }

    readonly List<NPC> npcs = new List<NPC>();
    readonly List<NPCActor> actors = new List<NPCActor>();
    bool spawnDemoIfMissing = true;
    bool built;

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
        BuildActors();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    void Update()
    {
        if (!built)
            BuildActors();

        LifeTime.Ensure();
        Evaluate();
        SimulateHidden(Time.deltaTime);
        RefreshPresence(SceneManager.GetActiveScene());
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        for (int i = 0; i < actors.Count; i++)
            actors[i].frozen = false;

        Evaluate();
        RefreshPresence(scene);
    }

    public static void ReportHour(float hour)
    {
        LifeTime.SetHour(hour);
        if (Instance == null)
            Bootstrap();

        Instance.Evaluate();
    }

    public static void StashActive()
    {
        if (Instance == null)
            return;

        Scene scene = SceneManager.GetActiveScene();
        for (int i = 0; i < Instance.actors.Count; i++)
        {
            NPCActor actor = Instance.actors[i];
            NPC body = actor.body;
            if (body == null || body.gameObject.scene != scene)
                continue;

            body.Traveler.WriteSnapshot(actor);
            body.MarkHandedOff();
            actor.body = null;
        }
    }

    public static void AdvanceAction(NPCActor actor)
    {
        if (actor == null || actor.completing || actor.presence != NPCPresence.Active)
            return;

        if (actor.fillingTime)
        {
            if (actor.Entry != null && actor.Entry.MustLeaveNow())
            {
                if (Instance != null)
                    Instance.RequestLeave(actor);
                return;
            }

            if (ShouldFill(actor))
            {
                actor.PickFillAction();
                actor.BeginAction();
                if (actor.body != null)
                    actor.body.Traveler.RefreshAction(false);
                return;
            }

            actor.fillingTime = false;
            CompleteVisit(actor);
            return;
        }

        actor.actionIndex++;
        bool skippedPurchase = false;
        while (actor.actionIndex < actor.ActionCount && SkipWithoutGoods(actor))
        {
            skippedPurchase = true;
            actor.actionIndex++;
        }

        if (skippedPurchase)
            Debug.Log("NPC " + actor.id + " khong mua, chi xem.");

        bool scriptDone = actor.actionIndex >= actor.ActionCount;
        bool exitStep = !scriptDone && actor.Action == "ra";
        if (scriptDone || exitStep)
        {
            if (ShouldFill(actor))
            {
                actor.BeginFill();
                if (actor.body != null)
                    actor.body.Traveler.RefreshAction(false);
                return;
            }

            if (scriptDone && actor.Entry != null && actor.Entry.LeaveHour >= 0f)
            {
                if (Instance != null)
                    Instance.RequestLeave(actor);
                else
                    CompleteVisit(actor);
                return;
            }
        }

        if (scriptDone)
        {
            CompleteVisit(actor);
            return;
        }

        actor.BeginAction();
        if (actor.body != null)
            actor.body.Traveler.RefreshAction(false);
    }

    public static void CompleteVisit(NPCActor actor)
    {
        if (Instance == null || actor == null || actor.completing || actor.presence == NPCPresence.Done)
            return;

        if (ShouldFill(actor))
        {
            actor.BeginFill();
            if (actor.body != null)
                actor.body.Traveler.RefreshAction(false);
            return;
        }

        actor.completing = true;
        if (actor.body != null)
            actor.body.Traveler.ReleaseCheckoutItem();
        Instance.Despawn(actor);
        actor.visitIndex++;
        Instance.SkipExpired(actor);

        if (actor.Entry == null)
        {
            actor.presence = NPCPresence.Done;
            actor.completing = false;
            return;
        }

        if (!Instance.CanStart(actor.Entry))
        {
            actor.presence = NPCPresence.Away;
            actor.ResetVisit();
            actor.completing = false;
            return;
        }

        Instance.Activate(actor);
        actor.completing = false;
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
        if (Instance == null || npc == null)
            return;

        Instance.npcs.Remove(npc);
    }

    void BuildActors()
    {
        actors.Clear();
        NPCScheduleTable[] tables = NPCScheduleTable.LoadAll();
        LifeTime.Ensure();

        for (int t = 0; t < tables.Length; t++)
        {
            IReadOnlyList<NPCScheduleEntry> rows = tables[t].Entries;
            for (int i = 0; i < rows.Count; i++)
            {
                NPCScheduleEntry entry = rows[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.npcId) || string.IsNullOrWhiteSpace(entry.mapName))
                    continue;

                NPCActor actor = FindActor(entry.npcId.Trim());
                if (actor == null)
                {
                    actor = new NPCActor { id = entry.npcId.Trim() };
                    actors.Add(actor);
                }

                actor.visits.Add(entry);
            }
        }

        built = true;
        if (actors.Count == 0)
            SpawnDemo(SceneManager.GetActiveScene());
    }

    NPCActor FindActor(string id)
    {
        for (int i = 0; i < actors.Count; i++)
        {
            if (actors[i].id == id)
                return actors[i];
        }

        return null;
    }

    void Evaluate()
    {
        for (int i = 0; i < actors.Count; i++)
        {
            NPCActor actor = actors[i];
            if (actor.presence == NPCPresence.Done)
                continue;

            if (actor.presence != NPCPresence.Active)
            {
                SkipExpired(actor);
                if (actor.presence == NPCPresence.Done || actor.Entry == null)
                    continue;

                if (CanStart(actor.Entry))
                    Activate(actor);

                continue;
            }

            if (actor.Entry != null && VisitExpired(actor.Entry))
                RequestLeave(actor);
        }
    }

    void Activate(NPCActor actor)
    {
        actor.ResetVisit();
        actor.presence = NPCPresence.Active;
        Debug.Log("NPC " + actor.id + " xuat hien luc " + LifeTime.FormatHour(LifeTime.HourNow)
            + " tai " + actor.Entry.mapName + ".");
        TrySpawn(actor, SceneManager.GetActiveScene());
    }

    void SkipExpired(NPCActor actor)
    {
        while (actor.Entry != null && VisitExpired(actor.Entry))
            actor.visitIndex++;

        if (actor.Entry == null)
            actor.presence = NPCPresence.Done;
    }

    bool CanStart(NPCScheduleEntry entry)
    {
        return entry.CanAppearNow();
    }

    bool VisitExpired(NPCScheduleEntry entry)
    {
        return entry.MustLeaveNow();
    }

    static bool ShouldFill(NPCActor actor)
    {
        if (actor == null || actor.leaveRequested || actor.forcedLeave)
            return false;

        NPCScheduleEntry entry = actor.Entry;
        return entry != null && entry.LeaveHour >= 0f && !entry.MustLeaveNow();
    }

    void RequestLeave(NPCActor actor)
    {
        if (actor.leaveRequested || actor.presence != NPCPresence.Active)
            return;

        bool wasFilling = actor.fillingTime;
        actor.leaveRequested = true;
        actor.fillingTime = false;
        actor.fillAction = null;
        actor.forcedLeave = false;

        int ra = actor.FindAction("ra");
        if (ra >= 0)
        {
            if (!wasFilling && actor.actionIndex == ra)
                return;

            actor.actionIndex = ra;
            actor.BeginAction();
            if (actor.body != null)
                actor.body.Traveler.RefreshAction(false);
            return;
        }

        actor.forcedLeave = true;
        actor.BeginAction();
        if (actor.body != null)
            actor.body.Traveler.RefreshAction(false);
    }

    void SimulateHidden(float dt)
    {
        if (dt <= 0f)
            return;

        Scene scene = SceneManager.GetActiveScene();
        for (int i = 0; i < actors.Count; i++)
        {
            NPCActor actor = actors[i];
            if (actor.presence != NPCPresence.Active || actor.frozen || actor.body != null)
                continue;

            NPCScheduleEntry entry = actor.Entry;
            if (entry == null || entry.MatchesMap(scene.name))
                continue;

            StepHidden(actor, dt);
        }
    }

    void StepHidden(NPCActor actor, float dt)
    {
        if (actor.fillingTime && (actor.Action == "di" || actor.Action == "xem"))
        {
            float need = actor.fillDuration > 0f ? actor.fillDuration : NPCTiming.PointSeconds;
            actor.timer += dt;
            if (actor.timer >= need)
                AdvanceAction(actor);
            return;
        }

        if (actor.reachedEnd && (actor.Action == "di" || actor.Action == "xem"))
        {
            AdvanceAction(actor);
            return;
        }

        switch (actor.Action)
        {
            case "di":
            case "xem":
                int count = actor.Entry.WalkPointNames.Length;
                if (count == 0)
                {
                    AdvanceAction(actor);
                    return;
                }

                float need = NPCTiming.PointSeconds + (actor.Action == "xem" ? NPCTiming.BrowsePause : 0f);
                actor.timer += dt;
                if (actor.timer < need)
                    return;

                actor.timer = 0f;
                actor.hasSavedPosition = false;
                actor.pointIndex++;
                if (actor.pointIndex >= count)
                    AdvanceAction(actor);
                break;
            case "chon":
                actor.timer += dt;
                if (actor.timer < ActionSeconds(actor, NPCTiming.ChooseSeconds))
                    return;

                actor.looked = true;
                if (!actor.fillingTime)
                    actor.wantsItem = ShelfStock.HasStock(actor.Entry.mapName);
                AdvanceAction(actor);
                break;
            case "mua":
                if (actor.heldItem == null)
                {
                    if (actor.wantsItem && ShelfStock.TryTakeAny(actor.Entry.mapName, out ItemData item))
                        actor.heldItem = item;
                    else
                        actor.wantsItem = false;
                }

                if (actor.heldItem == null)
                {
                    AdvanceAction(actor);
                    return;
                }

                actor.timer += dt;
                if (actor.timer >= NPCTiming.BuySeconds)
                    AdvanceAction(actor);
                break;
            case "doi":
                if (actor.heldItem == null || actor.paid)
                {
                    AdvanceAction(actor);
                    return;
                }

                if (!actor.itemPlaced)
                {
                    actor.timer += dt;
                    if (actor.timer < NPCTiming.PointSeconds)
                        return;

                    actor.itemPlaced = true;
                    actor.timer = 0f;
                    return;
                }

                actor.timer += dt;
                if (actor.timer >= NPCTiming.PayWaitSeconds)
                    AdvanceAction(actor);
                break;
            case "tra":
                if (!actor.paid)
                {
                    actor.paid = true;
                    int coins = PlayerCoin.Earn(actor.heldItem);
                    string label = actor.heldItem != null ? actor.heldItem.Label : "mon";
                    Debug.Log("NPC " + actor.id + " thanh toan " + label + " (+" + coins + " coin).");
                }

                actor.timer += dt;
                if (actor.timer >= NPCTiming.BuySeconds)
                    AdvanceAction(actor);
                break;
            case "dung":
                actor.timer += dt;
                if (actor.timer >= ActionSeconds(actor, NPCTiming.StandSeconds))
                    AdvanceAction(actor);
                break;
            case "ra":
                actor.timer += dt;
                if (actor.timer >= NPCTiming.PointSeconds)
                    CompleteVisit(actor);
                break;
            default:
                AdvanceAction(actor);
                break;
        }
    }

    void RefreshPresence(Scene scene)
    {
        for (int i = npcs.Count - 1; i >= 0; i--)
        {
            if (npcs[i] == null)
                npcs.RemoveAt(i);
        }

        for (int i = 0; i < actors.Count; i++)
            TrySpawn(actors[i], scene);
    }

    void TrySpawn(NPCActor actor, Scene scene)
    {
        if (actor.frozen || actor.body != null || actor.presence != NPCPresence.Active)
            return;

        NPCScheduleEntry entry = actor.Entry;
        if (entry == null || !entry.MatchesMap(scene.name))
            return;

        if (!TryResolvePosition(actor, out Vector3 position))
        {
            if (!actor.warned)
            {
                actor.warned = true;
                Debug.LogWarning("NPC '" + actor.id + "' khong thay diem trong " + entry.mapName + ".");
            }

            return;
        }

        bool resumed = actor.hasSavedPosition && actor.savedMap == scene.name;
        NPC body = NPC.CreateForActor(actor, position, resumed);
        actor.body = body;
        if (body == null)
            return;

        actor.frozen = false;
    }

    bool TryResolvePosition(NPCActor actor, out Vector3 position)
    {
        NPCScheduleEntry entry = actor.Entry;
        if (actor.hasSavedPosition && actor.savedMap == entry.mapName)
        {
            position = actor.savedPosition;
            return true;
        }

        if (actor.fillingTime && TryRandomPoint(entry.WalkPointNames, out position))
            return true;

        string[] points = entry.WalkPointNames;
        string action = actor.Action;
        if ((action == "tra" || (action == "doi" && actor.itemPlaced))
            && TryFind(CheckoutTable.CounterPointName, out position))
            return true;

        if ((action == "chon" || action == "mua" || action == "doi" || action == "dung" || actor.reachedEnd) && points.Length > 0)
        {
            if (TryFind(points[points.Length - 1], out position))
                return true;
        }

        if (actor.pointIndex > 0 && points.Length > 0)
        {
            int prev = Mathf.Clamp(actor.pointIndex - 1, 0, points.Length - 1);
            if (TryFind(points[prev], out position))
                return true;
        }

        if (action == "ra" && points.Length > 0 && TryFind(points[points.Length - 1], out position))
            return true;

        return TryFind(entry.spawnPointName, out position);
    }

    static float ActionSeconds(NPCActor actor, float scripted)
    {
        if (actor.fillingTime && actor.fillDuration > 0f)
            return actor.fillDuration;

        return scripted;
    }

    static bool SkipWithoutGoods(NPCActor actor)
    {
        if (actor == null || !NPCScheduleTime.IsPurchaseAction(actor.Action))
            return false;

        return actor.heldItem == null && !actor.wantsItem;
    }

    static bool TryRandomPoint(string[] names, out Vector3 position)
    {
        position = default;
        if (names == null || names.Length == 0)
            return false;

        int start = Random.Range(0, names.Length);
        for (int n = 0; n < names.Length; n++)
        {
            if (TryFind(names[(start + n) % names.Length], out position))
                return true;
        }

        return false;
    }

    static bool TryFind(string objectName, out Vector3 position)
    {
        position = default;
        if (string.IsNullOrWhiteSpace(objectName))
            return false;

        GameObject point = GameObject.Find(objectName.Trim());
        if (point == null)
            return false;

        position = point.transform.position;
        return true;
    }

    void Despawn(NPCActor actor)
    {
        NPC body = actor.body;
        actor.body = null;
        if (body == null)
            return;

        body.MarkHandedOff();
        Unregister(body);
        body.gameObject.SetActive(false);
        Destroy(body.gameObject);
    }

    void SpawnDemo(Scene scene)
    {
        if (!spawnDemoIfMissing || npcs.Count > 0)
            return;

        if (scene.name != "BaseStore" && scene.name != "InStore")
            return;

        Vector3 position = scene.name == "InStore"
            ? new Vector3(-2.26f, -6.5f, 0f)
            : new Vector3(2f, -28.58f, 0f);

        NPC.CreateDefault(position);
    }
}
