using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PlantBed : MonoBehaviour
{
    const int PlantSortBonus = 8;
    const int HighlightSortBonus = 10;

    [SerializeField] PlantBedKind kind = PlantBedKind.Soil;
    [SerializeField] string bedId;
    [SerializeField] SpriteRenderer bedRenderer;
    [SerializeField] Sprite emptySprite;
    [SerializeField] SpriteRenderer plantRenderer;
    [SerializeField] Vector2 plantLocalOffset = new Vector2(0f, 0.55f);
    [SerializeField] bool playerPlaced;
    [SerializeField] string snapKey;
    [SerializeField] string sourcePotItemId;

    SeedItemData planted;
    int plantedAbsoluteDay = -1;
    int stage;
    SpriteRenderer highlight;
    Color baseColor = Color.white;
    bool lit;
    bool hovered;

    public PlantBedKind Kind => kind;
    public bool IsEmpty => planted == null;
    public bool PlayerPlaced => playerPlaced;
    public string SnapKey => snapKey;
    public string SourcePotItemId => sourcePotItemId;
    public SeedItemData Planted => planted;
    public int PlantedAbsoluteDay => plantedAbsoluteDay;
    public int Stage => stage;

    public string BedKey
    {
        get
        {
            if (!string.IsNullOrEmpty(bedId))
                return bedId;

            return PlantWorld.BedKey(gameObject.scene.name, transform.position, kind);
        }
    }

    void Awake()
    {
        if (bedRenderer == null)
            bedRenderer = GetComponent<SpriteRenderer>();

        if (bedRenderer != null)
            baseColor = bedRenderer.color;

        if (emptySprite == null && bedRenderer != null)
            emptySprite = bedRenderer.sprite;

        EnsurePlantVisual();
        LifeTime.OnDayChanged += RefreshGrowth;
    }

    void OnDestroy()
    {
        LifeTime.OnDayChanged -= RefreshGrowth;
        if (!string.IsNullOrEmpty(snapKey))
        {
            PutSnapPoint snap = PlantWorld.FindSnap(snapKey);
            if (snap != null && snap.Occupant == this)
                snap.Clear();
        }
    }

    void OnEnable()
    {
        PlantWorld.Register(this);
        RefreshGrowth();
    }

    void OnDisable()
    {
        PlantWorld.Unregister(this);
    }

    public bool TryPlant(SeedItemData seed)
    {
        if (seed == null || planted != null)
            return false;

        planted = seed;
        plantedAbsoluteDay = LifeTime.AbsoluteDayNow;
        stage = 0;
        ApplyPlantVisual();
        SetHighlighted(false, false);
        PlantWorld.Remember(this);
        return true;
    }

    public void ConfigurePlacement(PlantBedKind bedKind, bool fromPlayer, string snap, string potItemId = "")
    {
        kind = bedKind;
        playerPlaced = fromPlayer;
        snapKey = snap ?? "";
        sourcePotItemId = potItemId ?? "";
    }

    public void ApplyPotLook(Sprite potSprite)
    {
        if (bedRenderer == null)
            bedRenderer = GetComponent<SpriteRenderer>();

        if (bedRenderer == null)
            return;

        if (potSprite != null)
        {
            emptySprite = potSprite;
            bedRenderer.sprite = potSprite;
            bedRenderer.color = Color.white;
            baseColor = Color.white;
        }
        else if (emptySprite != null)
        {
            bedRenderer.sprite = emptySprite;
        }

        bedRenderer.enabled = bedRenderer.sprite != null;
        kind = PlantBedKind.Pot;
    }

    public void Restore(SeedItemData seed, int absoluteDay, int stageIndex)
    {
        planted = seed;
        plantedAbsoluteDay = absoluteDay;
        stage = Mathf.Max(0, stageIndex);
        ApplyPlantVisual();
    }

    public void RestoreMeta(bool fromPlayer, string snap, string potItemId)
    {
        playerPlaced = fromPlayer;
        snapKey = snap ?? "";
        sourcePotItemId = potItemId ?? "";
    }

    public void ClearPlant(bool remember = true)
    {
        planted = null;
        plantedAbsoluteDay = -1;
        stage = 0;
        ApplyPlantVisual();
        if (remember)
            PlantWorld.Remember(this);
    }

    public void RefreshGrowth()
    {
        if (planted == null || plantedAbsoluteDay < 0)
        {
            ApplyPlantVisual();
            return;
        }

        int daysGrown = Mathf.Max(0, LifeTime.AbsoluteDayNow - plantedAbsoluteDay);
        int next = planted.ResolveStage(daysGrown);
        if (next != stage)
            stage = next;

        ApplyPlantVisual();
    }

    public void SetHighlighted(bool on, bool hover)
    {
        lit = on;
        hovered = hover;
        EnsureHighlight();

        if (bedRenderer != null)
        {
            if (!on)
                bedRenderer.color = baseColor;
            else if (hover)
                bedRenderer.color = Color.Lerp(baseColor, new Color(1f, 0.95f, 0.4f, 1f), 0.55f);
            else
                bedRenderer.color = Color.Lerp(baseColor, new Color(0.45f, 1f, 0.55f, 1f), 0.45f);
        }

        if (highlight == null)
            return;

        highlight.enabled = on;
        if (!on)
            return;

        float pulse = 0.8f + Mathf.Sin(Time.time * 6f) * 0.15f;
        highlight.color = hover
            ? new Color(1f, 0.92f, 0.3f, 0.95f)
            : new Color(0.4f, 1f, 0.55f, 0.8f);
        float scale = hover ? 1.5f : 1.2f * pulse;
        highlight.transform.localScale = new Vector3(scale, scale, 1f);
    }

    void LateUpdate()
    {
        if (lit)
            SetHighlighted(true, hovered);

        SyncChildSorting();
    }

    void SyncChildSorting()
    {
        int bedOrder = bedRenderer != null
            ? bedRenderer.sortingOrder
            : Mathf.RoundToInt(-transform.position.y * 100f);

        int layerId = bedRenderer != null ? bedRenderer.sortingLayerID : 0;

        if (plantRenderer != null)
        {
            plantRenderer.sortingLayerID = layerId;
            plantRenderer.sortingOrder = bedOrder + PlantSortBonus;
        }

        if (highlight != null)
        {
            highlight.sortingLayerID = layerId;
            highlight.sortingOrder = bedOrder + HighlightSortBonus;
        }
    }

    void EnsureHighlight()
    {
        if (highlight != null)
            return;

        Transform existing = transform.Find("BedHighlight");
        if (existing != null)
        {
            highlight = existing.GetComponent<SpriteRenderer>();
            if (highlight != null)
                return;
        }

        GameObject go = new GameObject("BedHighlight");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        highlight = go.AddComponent<SpriteRenderer>();
        highlight.sprite = PlantHighlight.RingSprite();
        highlight.enabled = false;
    }

    void EnsurePlantVisual()
    {
        if (plantRenderer != null)
        {
            StripSortY(plantRenderer.gameObject);
            return;
        }

        Transform existing = transform.Find("PlantVisual");
        if (existing != null)
        {
            plantRenderer = existing.GetComponent<SpriteRenderer>();
            if (plantRenderer == null)
                plantRenderer = existing.gameObject.AddComponent<SpriteRenderer>();

            StripSortY(existing.gameObject);
            return;
        }

        GameObject plant = new GameObject("PlantVisual");
        plant.transform.SetParent(transform, false);
        plant.transform.localPosition = plantLocalOffset;
        plantRenderer = plant.AddComponent<SpriteRenderer>();
    }

    static void StripSortY(GameObject target)
    {
        SortY sort = target.GetComponent<SortY>();
        if (sort != null)
            Destroy(sort);
    }

    void ApplyPlantVisual()
    {
        EnsurePlantVisual();

        if (bedRenderer != null)
        {
            if (emptySprite != null && bedRenderer.sprite != emptySprite)
                bedRenderer.sprite = emptySprite;

            bedRenderer.enabled = bedRenderer.sprite != null;
        }

        if (plantRenderer == null)
            return;

        if (planted == null)
        {
            plantRenderer.enabled = false;
            plantRenderer.sprite = null;
            return;
        }

        Sprite stageSprite = planted.StageSprite(stage);
        plantRenderer.sprite = stageSprite;
        plantRenderer.enabled = stageSprite != null;
        if (stageSprite == null)
            return;

        float scale = planted.plantScale > 0.01f ? planted.plantScale : 1f;
        plantRenderer.transform.localScale = new Vector3(scale, scale, 1f);

        Vector2 offset = planted.plantOffset;
        if (Mathf.Approximately(offset.x, 0f) && Mathf.Approximately(offset.y, 0f))
            offset = plantLocalOffset;

        // Dat tam cay tren mieng chau/dat, khong bi sprite chau che.
        float lift = stageSprite.bounds.extents.y * scale;
        float baseY = offset.y > 0.01f ? offset.y : plantLocalOffset.y;
        plantRenderer.transform.localPosition = new Vector3(offset.x, baseY + lift * 0.35f, 0f);
        SyncChildSorting();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (string.IsNullOrEmpty(bedId))
            bedId = name;
    }
#endif
}
