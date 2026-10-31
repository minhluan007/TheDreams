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
    public bool frozen;
    public bool completing;
    public bool wantsItem;
    public bool looked;
    public bool purchaseDone;
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
        purchaseDone = false;
        looked = false;
    }

    public void ResetVisit()
    {
        actionIndex = 0;
        leaveRequested = false;
        frozen = false;
        wantsItem = false;
        shelfName = null;
        shelfSlot = -1;
        BeginAction();
    }
}
