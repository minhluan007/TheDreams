using UnityEngine;

public class NPCMapTraveler : MonoBehaviour
{
    [SerializeField] float spawnIgnoreSeconds = 0.5f;

    NPC npc;
    NPCMove move;
    NPCActor actor;
    float ignoreUntil;
    bool seekingExit;
    bool seekingShelf;
    bool seekingCounter;
    bool pendingAdvance;
    bool pendingComplete;
    string lastWanderName;

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
        seekingShelf = false;
        seekingCounter = false;
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
        move.ReleaseHold();
        seekingExit = false;
        seekingShelf = false;
        seekingCounter = false;
        string action = actor.Action;
        if (actor.itemPlaced && action != "doi" && action != "tra")
            ReleaseCheckoutItem();

        if (action == "di" || action == "xem")
        {
            move.SetPaused(false);
            if (actor.fillingTime)
            {
                move.dwellOnArrival = action == "xem"
                    ? NPCTiming.BrowsePause
                    : Random.Range(0.5f, 1.6f);
                if (!SeekRandomWalkPoint())
                    pendingAdvance = true;
                return;
            }

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

        if (action == "mua")
        {
            BeginPickup();
            return;
        }

        if (action == "doi")
        {
            BeginCheckout();
            return;
        }

        if (action == "tra")
        {
            move.StopAndStay();
            if (!actor.paid)
                Pay();
            return;
        }

        move.StopAndStay();
        if (action == "chon" && !actor.looked)
            ChooseItem();
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
                if (actor.timer >= WaitSeconds(NPCTiming.StandSeconds))
                    NPCManager.AdvanceAction(actor);
                break;
            case "chon":
                actor.timer += Time.deltaTime;
                if (actor.timer >= WaitSeconds(NPCTiming.ChooseSeconds))
                    NPCManager.AdvanceAction(actor);
                break;
            case "mua":
                if (actor.heldItem == null)
                {
                    if (seekingShelf && !move.HasReachedEnd)
                        break;

                    TakeItem();
                    actor.timer = 0f;
                    seekingShelf = false;
                    move.StopAndStay();
                    if (actor.heldItem == null)
                    {
                        NPCManager.AdvanceAction(actor);
                        break;
                    }
                }

                actor.timer += Time.deltaTime;
                if (actor.timer >= NPCTiming.BuySeconds)
                    NPCManager.AdvanceAction(actor);
                break;
            case "doi":
                if (actor.heldItem == null)
                {
                    NPCManager.AdvanceAction(actor);
                    break;
                }

                if (!actor.itemPlaced)
                {
                    if (!seekingCounter || move.HasReachedEnd)
                        PlaceItem();
                    break;
                }

                actor.timer += Time.deltaTime;
                if (actor.timer >= NPCTiming.PayWaitSeconds)
                    NPCManager.AdvanceAction(actor);
                break;
            case "tra":
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
        ShelfInventory shelf = ShelfInventory.FindForNpc(transform.position);
        if (shelf != null)
            move.FaceTowards(shelf.transform.position);

        if (actor.fillingTime)
            return;

        actor.wantsItem = false;
        actor.shelfSlot = -1;
        actor.shelfName = null;
        if (shelf == null || !shelf.TryChooseSlot(out int slot, out _))
            return;

        actor.wantsItem = true;
        actor.shelfSlot = slot;
        actor.shelfName = shelf.gameObject.name;
    }

    void BeginPickup()
    {
        if (actor.heldItem != null)
        {
            move.StopAndStay();
            return;
        }

        if (!actor.wantsItem)
        {
            pendingAdvance = true;
            return;
        }

        move.SetPaused(false);
        move.dwellOnArrival = 0f;
        seekingShelf = SeekShelf();
        if (seekingShelf)
            return;

        move.StopAndStay();
        TakeItem();
        if (actor.heldItem == null)
            pendingAdvance = true;
    }

    void BeginCheckout()
    {
        if (actor.heldItem == null || actor.paid)
        {
            pendingAdvance = true;
            return;
        }

        if (actor.itemPlaced)
        {
            move.StopAndStay();
            ShowOnTable();
            return;
        }

        move.SetPaused(false);
        move.dwellOnArrival = 0f;
        seekingCounter = SeekCounter();
        if (!seekingCounter)
            PlaceItem();
    }

    bool SeekShelf()
    {
        Transform point = CheckoutTable.FindPoint(CheckoutTable.ShelfPointName);
        if (point == null)
        {
            ShelfInventory shelf = ShelfInventory.FindNamed(actor.shelfName);
            if (shelf == null)
                shelf = ShelfInventory.FindForNpc(transform.position);
            point = shelf != null ? shelf.transform : null;
        }

        if (point == null)
            return false;

        if (Vector2.Distance(transform.position, point.position) <= 0.35f)
            return false;

        move.SetTarget(point.position);
        return true;
    }

    bool SeekCounter()
    {
        Transform point = CheckoutTable.FindPoint(CheckoutTable.CounterPointName);
        if (point == null)
        {
            Debug.LogWarning("NPC khong thay " + CheckoutTable.CounterPointName + ".");
            return false;
        }

        if (Vector2.Distance(transform.position, point.position) <= 0.35f)
            return false;

        move.SetTarget(point.position);
        return true;
    }

    void TakeItem()
    {
        if (actor.heldItem != null)
            return;

        ShelfInventory shelf = ShelfInventory.FindNamed(actor.shelfName);
        if (shelf == null)
            shelf = ShelfInventory.FindForNpc(transform.position);

        if (shelf == null || !shelf.TryTakeSlot(actor.shelfSlot, out ItemData item))
        {
            actor.wantsItem = false;
            return;
        }

        actor.heldItem = item;
        actor.wantsItem = true;
        move.FaceTowards(shelf.transform.position);
        Debug.Log("NPC " + actor.id + " lay " + item.Label + " o ke.");
    }

    void PlaceItem()
    {
        if (actor.itemPlaced || actor.heldItem == null)
            return;

        actor.itemPlaced = true;
        actor.timer = 0f;
        seekingCounter = false;
        move.StopAndStay();
        ShowOnTable();
        Debug.Log("NPC " + actor.id + " dat " + actor.heldItem.Label + " len ban.");
    }

    void ShowOnTable()
    {
        CheckoutTable table = CheckoutTable.FindTable();
        if (table == null)
        {
            Debug.LogWarning("NPC khong thay " + CheckoutTable.TableObjectName + ".");
            return;
        }

        table.Place(actor.heldItem, actor.id);
        move.FaceTowards(table.transform.position);
    }

    public void ReleaseCheckoutItem()
    {
        if (actor == null || !actor.itemPlaced)
            return;

        CheckoutTable table = CheckoutTable.FindTable();
        if (table != null)
            table.ClearOwned(actor.id);

        actor.itemPlaced = false;
    }

    void Pay()
    {
        if (actor.paid)
            return;

        actor.paid = true;
        ItemData item = actor.heldItem;
        // Coin cong ngay khi tra. Scene player bam ban se thay cho buoc nay sau.
        int coins = PlayerCoin.Earn(item);
        string label = item != null ? item.Label : "mon";
        Debug.Log("NPC " + actor.id + " thanh toan " + label + " (+" + coins + " coin).");
    }

    float WaitSeconds(float scripted)
    {
        if (actor != null && actor.fillingTime && actor.fillDuration > 0f)
            return actor.fillDuration;

        return scripted;
    }

    bool SeekRandomWalkPoint()
    {
        string[] names = actor.Entry != null ? actor.Entry.WalkPointNames : null;
        if (names == null || names.Length == 0)
            return false;

        int offset = Random.Range(0, names.Length);
        Transform fallback = null;
        string fallbackName = null;
        for (int n = 0; n < names.Length; n++)
        {
            string name = names[(offset + n) % names.Length];
            GameObject point = GameObject.Find(name);
            if (point == null)
                continue;

            if (fallback == null)
            {
                fallback = point.transform;
                fallbackName = name;
            }

            if (names.Length > 1 && name == lastWanderName)
                continue;

            lastWanderName = name;
            move.SetTarget(point.transform.position);
            return true;
        }

        if (fallback == null)
            return false;

        lastWanderName = fallbackName;
        move.SetTarget(fallback.position);
        return true;
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
