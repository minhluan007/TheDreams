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

public class NPCActor
{
    public string id;
    public readonly List<NPCScheduleEntry> visits = new List<NPCScheduleEntry>();
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
    public ItemData heldItem;
    public bool itemPlaced;
    public bool paid;
    public string shelfName;
    public int shelfSlot = -1;
    public NPC body;
    public bool warned;

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
                return "ra";

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
        heldItem = null;
        itemPlaced = false;
        paid = false;
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
            fillAction = store && roll < 4 ? "chon" : "dung";
        }
        else if (!store)
        {
            fillAction = roll < 3 ? "dung" : "di";
        }
        else if (roll < 5)
        {
            fillAction = "di";
        }
        else if (roll < 7)
        {
            fillAction = "xem";
        }
        else if (roll < 9)
        {
            fillAction = "dung";
        }
        else
        {
            fillAction = "chon";
        }

        if (fillAction == "dung")
            fillDuration = Random.Range(1.4f, 3.4f);
        else if (fillAction == "chon")
            fillDuration = Random.Range(1.6f, 3f);
        else if (fillAction == "xem")
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
            if (name == "xem" || name == "chon" || name == "mua" || name == "doi" || name == "tra")
                return true;
        }

        return false;
    }
}
