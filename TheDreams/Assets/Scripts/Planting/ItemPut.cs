using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public enum ItemPlaceMode
{
    None,
    Pot,
    Seed
}

public class ItemPut : MonoBehaviour
{
    public static ItemPut Instance { get; private set; }

    [SerializeField] float clickRadius = 0.85f;
    [SerializeField] float arriveDistance = 0.65f;

    ItemPlaceMode previewMode;
    ItemPlaceMode placeMode;
    ItemData carrying;
    SpriteRenderer ghost;
    PlayerApproach approach;
    PutSnapPoint hoverSnap;
    PlantBed hoverBed;
    bool delivering;

    public bool IsPlacing => placeMode != ItemPlaceMode.None;

    void Awake()
    {
        Instance = this;
        approach = GetComponent<PlayerApproach>();
        if (approach == null)
            approach = gameObject.AddComponent<PlayerApproach>();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (ghost != null)
            Destroy(ghost.gameObject);
    }

    public static ItemPut EnsureOnPlayer()
    {
        if (Instance != null)
            return Instance;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return null;

        ItemPut put = player.GetComponent<ItemPut>();
        if (put == null)
            put = player.AddComponent<ItemPut>();

        return put;
    }

    public void OnInventorySelection(InventorySlotData slot)
    {
        if (IsPlacing)
            return;

        if (slot == null || !slot.HasItem)
        {
            ClearPreview();
            return;
        }

        if (slot.item is PotItemData)
        {
            previewMode = ItemPlaceMode.Pot;
            PlantHighlight.ShowAvailableSnaps();
            return;
        }

        if (slot.item is SeedItemData)
        {
            previewMode = ItemPlaceMode.Seed;
            PlantHighlight.ShowEmptyBeds();
            return;
        }

        ClearPreview();
    }

    public void BeginPlacePot(PotItemData pot)
    {
        if (pot == null || InventoryManager.Instance == null)
            return;

        StartPlace(ItemPlaceMode.Pot, pot);
    }

    public void BeginDeliverSeed(SeedItemData seed)
    {
        if (seed == null || InventoryManager.Instance == null)
            return;

        StartPlace(ItemPlaceMode.Seed, seed);
    }

    public void Cancel()
    {
        delivering = false;
        approach?.Cancel();
        placeMode = ItemPlaceMode.None;
        carrying = null;
        hoverSnap = null;
        hoverBed = null;
        HideGhost();
        RefreshPreviewFromInventory();
    }

    void StartPlace(ItemPlaceMode mode, ItemData item)
    {
        placeMode = mode;
        carrying = item;
        previewMode = mode;
        delivering = false;
        approach?.Cancel();

        if (InventoryUI.Instance != null)
            InventoryUI.Instance.Close();

        EnsureGhost(item != null ? item.icon : null);
        if (mode == ItemPlaceMode.Pot)
            PlantHighlight.ShowAvailableSnaps();
        else
            PlantHighlight.ShowEmptyBeds();
    }

    void Update()
    {
        if (!IsPlacing)
            return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cancel();
            return;
        }

        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            Cancel();
            return;
        }

        if (delivering)
        {
            if (placeMode == ItemPlaceMode.Pot)
                PlantHighlight.ShowAvailableSnaps(hoverSnap);
            else
                PlantHighlight.ShowEmptyBeds(hoverBed);
            return;
        }

        UpdateGhostFollow();
        UpdateHover();

        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        if (IsPointerOverUi())
            return;

        if (placeMode == ItemPlaceMode.Pot)
            TryClickSnap();
        else if (placeMode == ItemPlaceMode.Seed)
            TryClickBed();
    }

    void UpdateGhostFollow()
    {
        if (ghost == null || Camera.main == null || Mouse.current == null)
            return;

        Vector3 screen = Mouse.current.position.ReadValue();
        Vector3 world = Camera.main.ScreenToWorldPoint(screen);
        world.z = 0f;
        ghost.transform.position = world;
        ghost.enabled = true;
    }

    void UpdateHover()
    {
        Vector3 world = GhostWorld();
        if (placeMode == ItemPlaceMode.Pot)
        {
            hoverSnap = PlantWorld.FindNearestSnap(world, clickRadius, PlantBedKind.Pot);
            hoverBed = null;
            PlantHighlight.ShowAvailableSnaps(hoverSnap);
            return;
        }

        hoverSnap = null;
        hoverBed = PlantWorld.FindNearestEmpty(world, clickRadius);
        PlantHighlight.ShowEmptyBeds(hoverBed);
    }

    void TryClickSnap()
    {
        if (hoverSnap == null || !(carrying is PotItemData pot))
            return;

        PutSnapPoint target = hoverSnap;
        delivering = true;
        HideGhost();
        approach.GoTo(target.transform.position, () => FinishPutPot(pot, target), arriveDistance);
    }

    void TryClickBed()
    {
        if (hoverBed == null || !(carrying is SeedItemData seed))
            return;

        PlantBed target = hoverBed;
        delivering = true;
        HideGhost();
        approach.GoTo(target.transform.position, () => FinishDeliverSeed(seed, target), arriveDistance);
    }

    void FinishPutPot(PotItemData pot, PutSnapPoint snap)
    {
        delivering = false;
        if (pot == null || snap == null || !snap.CanAccept(PlantBedKind.Pot))
        {
            Cancel();
            return;
        }

        PlantBed bed = PlantWorld.PlacePot(pot, snap, snap.transform.position);
        if (bed == null)
        {
            Cancel();
            return;
        }

        InventoryManager.Instance?.RemoveItem(pot, 1);
        InventoryUI.Instance?.OnSlotsChanged();
        placeMode = ItemPlaceMode.None;
        carrying = null;
        HideGhost();
        RefreshPreviewFromInventory();
    }

    void FinishDeliverSeed(SeedItemData seed, PlantBed bed)
    {
        delivering = false;
        if (seed == null || bed == null || !bed.IsEmpty)
        {
            Cancel();
            return;
        }

        if (!bed.TryPlant(seed))
        {
            Cancel();
            return;
        }

        InventoryManager.Instance?.RemoveItem(seed, 1);
        InventoryUI.Instance?.OnSlotsChanged();
        placeMode = ItemPlaceMode.None;
        carrying = null;
        HideGhost();
        RefreshPreviewFromInventory();
    }

    void RefreshPreviewFromInventory()
    {
        InventorySlotData selected = InventoryUI.Instance != null ? InventoryUI.Instance.Selected : null;
        OnInventorySelection(selected);
    }

    void ClearPreview()
    {
        previewMode = ItemPlaceMode.None;
        PlantHighlight.ClearAll();
    }

    Vector3 GhostWorld()
    {
        if (Camera.main == null || Mouse.current == null)
            return transform.position;

        Vector3 screen = Mouse.current.position.ReadValue();
        Vector3 world = Camera.main.ScreenToWorldPoint(screen);
        world.z = 0f;
        return world;
    }

    void EnsureGhost(Sprite icon)
    {
        if (ghost == null)
        {
            GameObject go = new GameObject("ItemPutGhost");
            ghost = go.AddComponent<SpriteRenderer>();
        }

        ghost.sprite = icon;
        ghost.color = new Color(1f, 1f, 1f, 0.9f);
        ghost.sortingOrder = 30000;
        if (carrying is SeedItemData seed && seed.plantScale > 0.01f)
            ghost.transform.localScale = Vector3.one * Mathf.Max(1.5f, seed.plantScale);
        else
            ghost.transform.localScale = Vector3.one * 1.25f;

        ghost.enabled = icon != null;
    }

    void HideGhost()
    {
        if (ghost != null)
            ghost.enabled = false;
    }

    static bool IsPointerOverUi()
    {
        if (EventSystem.current == null)
            return false;

        if (EventSystem.current.IsPointerOverGameObject())
            return true;

        if (Mouse.current != null)
            return EventSystem.current.IsPointerOverGameObject(Mouse.current.deviceId);

        return false;
    }
}
