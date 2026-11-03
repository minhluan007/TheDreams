using System.Collections.Generic;
using UnityEngine;

public enum NPCPresence
{
    Away,
    Active,
    Done
}

public static class NPCTiming
{
    public const float PointSeconds = 3.2f;
    public const float BrowsePause = 1.5f;
    public const float ChooseSeconds = 2f;
    public const float BuySeconds = 1.1f;
    public const float StandSeconds = 1.4f;
    public const float PayWaitSeconds = 3f;
}

public class NPCGood
{
    public ItemData item;
    public int sellPrice;
    public bool placed;
    public bool paid;
}

public class NPCActor
{
    public string id;
    public readonly List<NPCScheduleEntry> visits = new List<NPCScheduleEntry>();
    public readonly List<NPCGood> goods = new List<NPCGood>();
    public int visitIndex;
    public int actionIndex;
    public NPCPresence presence;
    public int pointIndex;
    public float timer;
    public bool reachedEnd;
    public bool hasSavedPosition;
    public Vector3 savedPosition;
    public string savedMap;
    public bool leaveRequested;
    public bool fillingTime;
    public bool forcedLeave;
    public string fillAction;
    public float fillDuration;
    public bool frozen;
    public bool completing;
    public bool wantsItem;
    public bool looked;
    public string shelfName;
    public int shelfSlot = -1;
    public NPC body;
    public bool warned;

    public bool HasUnpaid
    {
        get
        {
            for (int i = 0; i < goods.Count; i++)
            {
                if (!goods[i].paid)
                    return true;
            }

            return false;
        }
    }

    public bool UnpaidOnTable
    {
        get
        {
            bool any = false;
            for (int i = 0; i < goods.Count; i++)
            {
                if (goods[i].paid)
                    continue;

                if (!goods[i].placed)
                    return false;

                any = true;
            }

            return any;
        }
    }

    public bool AnyOnTable
    {
        get
        {
            for (int i = 0; i < goods.Count; i++)
            {
                if (goods[i].placed)
                    return true;
            }

            return false;
        }
    }

    public void AddGood(ItemData item, int sellPrice)
    {
        goods.Add(new NPCGood
        {
            item = item,
            sellPrice = sellPrice
        });
    }

    public void PlaceUnpaid()
    {
        for (int i = 0; i < goods.Count; i++)
        {
            if (!goods[i].paid)
                goods[i].placed = true;
        }
    }

    public void CopyPlaced(List<ItemData> into)
    {
        into.Clear();
        for (int i = 0; i < goods.Count; i++)
        {
            if (goods[i].placed && goods[i].item != null)
                into.Add(goods[i].item);
        }
    }

    public void LiftFromTable()
    {
        for (int i = goods.Count - 1; i >= 0; i--)
        {
            if (goods[i].paid)
                goods.RemoveAt(i);
            else
                goods[i].placed = false;
        }
    }

    public int PayUnpaid(out string label)
    {
        int total = 0;
        int count = 0;
        string text = string.Empty;
        for (int i = 0; i < goods.Count; i++)
        {
            NPCGood good = goods[i];
            if (good.paid)
                continue;

            total += PlayerCoin.Earn(good.item, good.sellPrice);
            good.paid = true;
            if (count > 0)
                text += ", ";

            text += good.item != null ? good.item.Label : "mon";
            count++;
        }

        if (count <= 1)
            label = count == 1 ? text : "mon";
        else
            label = count + " mon (" + text + ")";

        return total;
    }

    public NPCScheduleEntry Entry =>
        visitIndex >= 0 && visitIndex < visits.Count ? visits[visitIndex] : null;

    public int ActionCount
    {
        get
        {
            NPCScheduleEntry entry = Entry;
            return entry == null ? 0 : entry.ActionNames.Length;
        }
    }

    public string Action
    {
        get
        {
            if (forcedLeave)
                return NPCScheduleTime.Leave;

            if (fillingTime && !string.IsNullOrEmpty(fillAction))
                return fillAction;

            NPCScheduleEntry entry = Entry;
            if (entry == null || actionIndex < 0 || actionIndex >= entry.ActionNames.Length)
                return string.Empty;

            return entry.ActionNames[actionIndex];
        }
    }

    public int FindAction(string name)
    {
        NPCScheduleEntry entry = Entry;
        if (entry == null)
            return -1;

        string[] actions = entry.ActionNames;
        for (int i = 0; i < actions.Length; i++)
        {
            if (actions[i] == name)
                return i;
        }

        return -1;
    }

    public void BeginAction()
    {
        timer = 0f;
        pointIndex = 0;
        reachedEnd = false;
        hasSavedPosition = false;
        looked = false;
    }

    public void ResetVisit()
    {
        actionIndex = 0;
        leaveRequested = false;
        fillingTime = false;
        forcedLeave = false;
        fillAction = null;
        fillDuration = 0f;
        frozen = false;
        wantsItem = false;
        goods.Clear();
        shelfName = null;
        shelfSlot = -1;
        BeginAction();
    }

    public void BeginFill()
    {
        bool started = !fillingTime;
        fillingTime = true;
        forcedLeave = false;
        actionIndex = 0;
        PickFillAction();
        BeginAction();

        if (started && Entry != null)
        {
            Debug.Log("NPC " + id + " o lai " + Entry.mapName
                + " den " + Entry.leaveTime + ".");
        }
    }

    public void PickFillAction()
    {
        bool store = VisitLooksAroundStock();
        bool canWalk = Entry != null && Entry.WalkPointNames.Length > 0;
        int roll = Random.Range(0, 10);

        if (!canWalk)
        {
            fillAction = store && roll < 4 ? NPCScheduleTime.Choose : NPCScheduleTime.Stand;
        }
        else if (!store)
        {
            fillAction = roll < 3 ? NPCScheduleTime.Stand : NPCScheduleTime.Walk;
        }
        else if (roll < 5)
        {
            fillAction = NPCScheduleTime.Walk;
        }
        else if (roll < 7)
        {
            fillAction = NPCScheduleTime.Look;
        }
        else if (roll < 9)
        {
            fillAction = NPCScheduleTime.Stand;
        }
        else
        {
            fillAction = NPCScheduleTime.Choose;
        }

        if (fillAction == NPCScheduleTime.Stand)
            fillDuration = Random.Range(1.4f, 3.4f);
        else if (fillAction == NPCScheduleTime.Choose)
            fillDuration = Random.Range(1.6f, 3f);
        else if (fillAction == NPCScheduleTime.Look)
            fillDuration = NPCTiming.PointSeconds + NPCTiming.BrowsePause;
        else
            fillDuration = Random.Range(2.4f, 4.2f);
    }

    bool VisitLooksAroundStock()
    {
        NPCScheduleEntry entry = Entry;
        if (entry == null)
            return false;

        string[] names = entry.ActionNames;
        for (int i = 0; i < names.Length; i++)
        {
            string name = names[i];
            if (name == NPCScheduleTime.Look || name == NPCScheduleTime.Choose || name == NPCScheduleTime.Buy
                || name == NPCScheduleTime.Wait || name == NPCScheduleTime.Pay)
                return true;
        }

        return false;
    }
}
