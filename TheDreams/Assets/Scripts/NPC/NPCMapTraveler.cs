using UnityEngine;

public class NPCMapTraveler : MonoBehaviour
{
    [SerializeField] float spawnIgnoreSeconds = 0.5f;

    NPC npc;
    NPCMove move;
    NPCActor actor;
    float ignoreUntil;
    bool seekingExit;
    bool pendingAdvance;
    bool pendingComplete;

    void Awake()
    {
        Cache();
    }

    void Cache()
    {
        if (npc == null)
            npc = GetComponent<NPC>();
        if (move == null)
            move = GetComponent<NPCMove>();
    }

    public void Begin(NPCActor life, bool resumed)
    {
        Cache();
        actor = life;
        pendingAdvance = false;
        pendingComplete = false;
        seekingExit = false;
        ignoreUntil = Time.time + spawnIgnoreSeconds;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.position = transform.position;

        RefreshAction(resumed);
    }

    public void RefreshAction(bool resumed)
    {
        Cache();
        if (actor == null || move == null)
            return;

        move.LockRoute();
        seekingExit = false;
        string action = actor.Action;

        if (action == "di" || action == "xem")
        {
            move.SetPaused(false);
            move.dwellOnArrival = action == "xem" ? NPCTiming.BrowsePause : 0f;
            if (!move.BindNamedPoints(actor.Entry.WalkPointNames, actor.pointIndex))
                pendingAdvance = true;
            return;
        }

        if (action == "ra")
        {
            move.SetPaused(false);
            move.dwellOnArrival = 0f;
            return;
        }

        move.StopAndStay();
        if (action == "chon" && !actor.looked)
            ChooseItem();
        else if (action == "mua" && !actor.purchaseDone)
            BuyItem();
        else if (action != "dung")
            pendingAdvance = true;
    }

    public void WriteSnapshot(NPCActor snapshot)
    {
        if (snapshot == null)
            return;

        Cache();
        snapshot.savedPosition = transform.position;
        snapshot.savedMap = gameObject.scene.name;
        snapshot.hasSavedPosition = true;
        if (move != null)
        {
            snapshot.pointIndex = move.Index;
            snapshot.reachedEnd = move.HasReachedEnd;
            move.SetPaused(true);
        }

        snapshot.frozen = true;
    }

    void Update()
    {
        if (actor == null)
        {
            UpdateUnscheduled();
            return;
        }

        if (actor.frozen || actor.body != npc)
        {
            move.SetPaused(true);
            return;
        }

        if (pendingComplete)
        {
            pendingComplete = false;
            NPCManager.CompleteVisit(actor);
            return;
        }

        if (pendingAdvance)
        {
            pendingAdvance = false;
            NPCManager.AdvanceAction(actor);
            return;
        }

        if (move.IsHolding)
            return;

        switch (actor.Action)
        {
            case "di":
            case "xem":
                if (move.HasReachedEnd)
                    NPCManager.AdvanceAction(actor);
                break;
            case "dung":
                actor.timer += Time.deltaTime;
                if (actor.timer >= NPCTiming.StandSeconds)
                    NPCManager.AdvanceAction(actor);
                break;
            case "chon":
                actor.timer += Time.deltaTime;
                if (actor.timer >= NPCTiming.ChooseSeconds)
                    NPCManager.AdvanceAction(actor);
                break;
            case "mua":
                actor.timer += Time.deltaTime;
                if (actor.timer >= NPCTiming.BuySeconds)
                    NPCManager.AdvanceAction(actor);
                break;
            case "ra":
                if (!seekingExit)
                {
                    seekingExit = true;
                    if (!SeekExit())
                        pendingComplete = true;
                }

                if (pendingComplete)
                {
                    pendingComplete = false;
                    NPCManager.CompleteVisit(actor);
                    return;
                }

                if (move.HasReachedEnd)
                    NPCManager.CompleteVisit(actor);
                break;
        }
    }

    void ChooseItem()
    {
        actor.looked = true;
        actor.wantsItem = false;
        actor.shelfSlot = -1;
        actor.shelfName = null;

        ShelfInventory shelf = ShelfInventory.FindForNpc(transform.position);
        if (shelf == null || !shelf.TryChooseSlot(out int slot, out _))
            return;

        actor.wantsItem = true;
        actor.shelfSlot = slot;
        actor.shelfName = shelf.gameObject.name;
        move.FaceTowards(shelf.transform.position);
    }

    void BuyItem()
    {
        actor.purchaseDone = true;
        ShelfInventory shelf = ShelfInventory.FindNamed(actor.shelfName);
        if (shelf == null)
            shelf = ShelfInventory.FindForNpc(transform.position);

        if (shelf == null || !shelf.TryTakeSlot(actor.shelfSlot, out string itemName))
        {
            actor.wantsItem = false;
            return;
        }

        Debug.Log("NPC " + actor.id + " mua " + itemName + ".");
    }

    bool SeekExit()
    {
        Transform point = FindDestroyPoint();
        if (point == null)
            return false;

        move.SetTarget(point.position);
        return true;
    }

    Transform FindDestroyPoint()
    {
        string destroyName = actor.Entry != null ? actor.Entry.destroyPointName : null;
        if (!string.IsNullOrWhiteSpace(destroyName))
        {
            GameObject named = GameObject.Find(destroyName.Trim());
            if (named != null)
                return named.transform;
        }

        MapExit exit = FindExitOnCurrentMap();
        return exit != null ? exit.transform : null;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (actor == null || actor.body != npc || actor.frozen)
            return;

        if (Time.time < ignoreUntil || actor.Action != "ra")
            return;

        if (other.GetComponent<MapExit>() == null)
            return;

        NPCManager.CompleteVisit(actor);
    }

    void UpdateUnscheduled()
    {
        if (move.HasReachedEnd && !seekingExit)
        {
            seekingExit = true;
            Transform point = FindLooseExit();
            if (point == null)
            {
                npc.LeaveMap();
                return;
            }

            move.SetTarget(point.position);
        }

        if (seekingExit && move.HasReachedEnd)
            npc.LeaveMap();
    }

    Transform FindLooseExit()
    {
        MapExit exit = FindExitOnCurrentMap();
        return exit != null ? exit.transform : null;
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
