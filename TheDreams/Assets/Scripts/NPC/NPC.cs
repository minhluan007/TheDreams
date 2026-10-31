using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(NPCMove))]
[RequireComponent(typeof(NPCMapTraveler))]
public class NPC : MonoBehaviour
{
    public const string TagName = "NPC";

    [SerializeField] string npcId = "customer_01";

    NPCMove move;
    NPCMapTraveler traveler;
    NPCActor actor;
    bool handedOff;
    static NPCScheduleEntry spawning;
    static NPCActor spawningActor;
    static bool spawningResumed;

    public string NpcId => npcId;
    public NPCMove Move => move;
    public NPCMapTraveler Traveler => traveler;
    public NPCActor Actor => actor;

    void Reset()
    {
        EnsureHierarchy();
    }

    void Awake()
    {
        EnsureHierarchy();
        move = GetComponent<NPCMove>();
        traveler = GetComponent<NPCMapTraveler>();
        ApplySpawningEntry();

        if (!NPCManager.Register(this))
        {
            handedOff = true;
            Destroy(gameObject);
            return;
        }
    }

    void OnDestroy()
    {
        if (actor != null && !handedOff && traveler != null)
            traveler.WriteSnapshot(actor);

        if (actor != null && actor.body == this)
            actor.body = null;

        NPCManager.Unregister(this);
    }

    public void MarkHandedOff()
    {
        handedOff = true;
    }

    public void LeaveMap()
    {
        handedOff = true;
        Destroy(gameObject);
    }

    public void EnsureHierarchy()
    {
        gameObject.tag = TagName;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D box = GetComponent<BoxCollider2D>();
        box.isTrigger = false;
        if (box.size == Vector2.zero)
            box.size = new Vector2(1f, 0.4f);
        if (box.offset == Vector2.zero)
            box.offset = new Vector2(0f, 0.2f);

        Transform sprite = transform.Find("Sprite_NPC");
        if (sprite == null)
        {
            GameObject child = new GameObject("Sprite_NPC");
            child.transform.SetParent(transform, false);
            child.AddComponent<SpriteRenderer>();
            child.AddComponent<SortY>();
            child.AddComponent<Animator>();
            child.AddComponent<NPCPlaceholderVisual>();
        }
    }

    public static NPC CreateDefault(Vector3 position)
    {
        GameObject root = new GameObject("NPC");
        root.transform.position = position;
        NPC npc = root.AddComponent<NPC>();
        npc.EnsureHierarchy();
        return npc;
    }

    public static NPC CreateForActor(NPCActor life, Vector3 position, bool resumed)
    {
        if (life == null || life.Entry == null)
            return null;

        spawning = life.Entry;
        spawningActor = life;
        spawningResumed = resumed;
        try
        {
            return CreateFromSchedule(life.Entry, position);
        }
        finally
        {
            spawning = null;
            spawningActor = null;
            spawningResumed = false;
        }
    }

    public static NPC CreateFromSchedule(NPCScheduleEntry entry, Vector3 position)
    {
        if (entry == null)
            return null;

        bool ownSpawn = spawning == null;
        if (ownSpawn)
            spawning = entry;

        try
        {
            if (entry.prefab != null)
            {
                GameObject spawned = Instantiate(entry.prefab, position, Quaternion.identity);
                NPC npc = spawned.GetComponent<NPC>();
                if (npc != null)
                    return npc;

                Destroy(spawned);
            }

            return CreateDefault(position);
        }
        finally
        {
            if (ownSpawn)
                spawning = null;
        }
    }

    void ApplySpawningEntry()
    {
        if (spawning == null && spawningActor == null)
            return;

        actor = spawningActor;
        NPCScheduleEntry entry = spawning != null ? spawning : actor.Entry;
        if (entry == null)
            return;

        if (!string.IsNullOrWhiteSpace(entry.npcId))
        {
            npcId = entry.npcId.Trim();
            gameObject.name = npcId;
        }

        if (actor != null && traveler != null)
            traveler.Begin(actor, spawningResumed);
    }
}
