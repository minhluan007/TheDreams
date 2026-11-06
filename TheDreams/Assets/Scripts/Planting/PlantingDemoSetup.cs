using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlantingDemoSetup : MonoBehaviour
{
    [SerializeField] PotItemData potItem;
    [SerializeField] SeedItemData seedItem;
    [SerializeField] GameObject potPrefab;
    [SerializeField] bool buildIfMissing = true;
    [SerializeField] bool giveStartingItems = true;
    [SerializeField] Key advanceDayKey = Key.N;

    bool built;

    void Start()
    {
        if (!buildIfMissing || built)
            return;

        if (SceneManager.GetActiveScene().name != "Town")
            return;

        built = true;
        EnsurePlayerPut();
        EnsureItemsRegistered();
        EnsurePotPrefabLink();
        BuildLayout();
        GiveItems();
    }

    void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current[advanceDayKey].wasPressedThisFrame)
        {
            LifeTime.AdvanceDay();
            Debug.Log("Demo: sang " + LifeTime.Ensure().DateText);
        }
    }

    void EnsurePlayerPut()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        if (player.GetComponent<ItemPut>() == null)
            player.AddComponent<ItemPut>();
    }

    void EnsureItemsRegistered()
    {
        if (InventoryManager.Instance == null)
            return;

        InventoryManager.Instance.RegisterItem(potItem);
        InventoryManager.Instance.RegisterItem(seedItem);
    }

    void EnsurePotPrefabLink()
    {
        if (potPrefab == null && potItem != null && potItem.potPrefab != null)
            potPrefab = potItem.potPrefab;

        if (potPrefab == null)
            potPrefab = BuildPotPrefabTemplate();

        PlantWorld.SetPotPrefabFallback(potPrefab);
    }

    void BuildLayout()
    {
        Transform root = transform;
        Vector3 origin = new Vector3(6.5f, -25.1f, 0f);

        CreateSnap("PutSnap_Demo_1", root, origin + new Vector3(-1.2f, 0f, 0f));
        CreateSnap("PutSnap_Demo_2", root, origin + new Vector3(-0.2f, 0.8f, 0f));
        CreateSoil("PlantBed_Soil_Demo", root, origin + new Vector3(1.4f, 0.2f, 0f));

        if (potItem != null)
            CreatePickup("Pickup_EmptyPot_Demo", root, origin + new Vector3(0.6f, -1.1f, 0f), potItem, 2);

        if (seedItem != null)
            CreatePickup("Pickup_TomatoSeed_Demo", root, origin + new Vector3(1.6f, -1.1f, 0f), seedItem, 5);
    }

    void GiveItems()
    {
        if (!giveStartingItems || InventoryManager.Instance == null)
            return;

        if (potItem != null)
            InventoryManager.Instance.AddItem(potItem, 1);

        if (seedItem != null)
            InventoryManager.Instance.AddItem(seedItem, 3);
    }

    static PutSnapPoint CreateSnap(string objectName, Transform parent, Vector3 position)
    {
        Transform existing = parent.Find(objectName);
        if (existing != null)
            return existing.GetComponent<PutSnapPoint>();

        GameObject go = new GameObject(objectName);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        return go.AddComponent<PutSnapPoint>();
    }

    static PlantBed CreateSoil(string objectName, Transform parent, Vector3 position)
    {
        Transform existing = parent.Find(objectName);
        if (existing != null)
            return existing.GetComponent<PlantBed>();

        GameObject go = new GameObject(objectName);
        go.transform.SetParent(parent, false);
        go.transform.position = position;

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.color = new Color(0.45f, 0.28f, 0.12f, 1f);
        renderer.sprite = CreateBlockSprite();
        go.AddComponent<SortY>();

        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(1.2f, 1.2f);

        PlantBed bed = go.AddComponent<PlantBed>();
        bed.ConfigurePlacement(PlantBedKind.Soil, false, "");
        return bed;
    }

    static void CreatePickup(string objectName, Transform parent, Vector3 position, ItemData item, int amount)
    {
        if (parent.Find(objectName) != null)
            return;

        GameObject go = new GameObject(objectName);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.AddComponent<SpriteRenderer>();
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(1f, 1f);
        go.AddComponent<SortY>();

        ItemPickup pickup = go.AddComponent<ItemPickup>();
        pickup.Setup(item, amount);
    }

    GameObject BuildPotPrefabTemplate()
    {
        GameObject go = new GameObject("PlantBed_Pot_Runtime");
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        Sprite potSprite = potItem != null ? potItem.icon : null;
        renderer.sprite = potSprite != null ? potSprite : CreateBlockSprite();
        renderer.color = Color.white;
        go.AddComponent<SortY>();

        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(1.1f, 1.1f);

        PlantBed bed = go.AddComponent<PlantBed>();
        bed.ConfigurePlacement(PlantBedKind.Pot, true, "");
        bed.ApplyPotLook(potSprite);
        go.SetActive(false);
        DontDestroyOnLoad(go);
        potPrefab = go;
        return go;
    }

    static Sprite CreateBlockSprite()
    {
        Texture2D texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[64];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;

        texture.SetPixels(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Point;
        return Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f), 8f);
    }
}
