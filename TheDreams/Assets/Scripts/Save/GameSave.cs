using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class SavedSlot
{
    public string itemId;
    public int amount;
    public int coinPriceSell;
    public bool sellPriceSet;
}

[Serializable]
public class SavedShelf
{
    public string key;
    public SavedSlot[] slots;
}

[Serializable]
public class GameSaveData
{
    public int version = 2;
    public string sceneName;
    public bool hasPlayer;
    public float x;
    public float y;
    public float z;
    public float hour;
    public int coins;
    public SavedSlot[] inventory;
    public SavedShelf[] shelves;
    public string[] pickups;
}

public static class GameSave
{
    static GameSaveData pending;

    public static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    public static bool HasSave => File.Exists(SavePath);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetSession()
    {
        pending = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Hook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public static void Save()
    {
        ShelfInventory.RememberLoaded();
        InventoryManager.CaptureActive();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        GameSaveData data = new GameSaveData
        {
            version = 2,
            sceneName = SceneManager.GetActiveScene().name,
            hasPlayer = player != null,
            hour = LifeTime.HourNow,
            coins = PlayerCoin.Amount,
            inventory = InventoryManager.ExportActive(),
            shelves = ShelfStock.Export(),
            pickups = PickupMemory.Export()
        };

        if (player != null)
        {
            Vector3 position = player.transform.position;
            data.x = position.x;
            data.y = position.y;
            data.z = position.z;
        }

        try
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
        }
        catch (Exception exception)
        {
            Debug.LogError("Không lưu được game: " + exception.Message);
            return;
        }

        Debug.Log("Đã lưu game.");
    }

    public static bool Load()
    {
        if (!TryRead(out GameSaveData data))
            return false;

        if (data.sceneName == SceneManager.GetActiveScene().name)
        {
            Apply(data);
            Debug.Log("Đã tải game.");
            return true;
        }

        if (!SceneInBuild(data.sceneName))
        {
            Debug.LogWarning("Scene save không có trong Build Settings: " + data.sceneName);
            return false;
        }

        if (MapTransitionManager.Instance != null && MapTransitionManager.Instance.IsLoading)
        {
            Debug.Log("Đang chuyển map.");
            return false;
        }

        pending = data;
        InventoryManager.Store(data.inventory);
        PickupMemory.Replace(data.pickups);
        LifeTime.SetHour(data.hour);
        ApplyCoins(data);
        ShelfStock.Invalidate();

        if (MapTransitionManager.Instance != null)
            MapTransitionManager.Instance.LoadMap(data.sceneName, null);
        else
            SceneManager.LoadScene(data.sceneName);

        Debug.Log("Đã tải game.");
        return true;
    }

    // Scene menu sau này gọi hàm này trước khi vào map bắt đầu.
    public static void BeginNewGame()
    {
        pending = null;
        PickupMemory.Clear();
        ShelfStock.Clear();
        InventoryManager.PrepareNewGame();
        PlayerCoin.BeginNewGame();

        LifeTime clock = LifeTime.Ensure();
        LifeTime.SetHour(clock.DayStartHour);
    }

    public static void DeleteSave()
    {
        if (!File.Exists(SavePath))
            return;

        File.Delete(SavePath);
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (pending == null)
            return;

        GameSaveData data = pending;
        pending = null;
        Apply(data);
    }

    static void Apply(GameSaveData data)
    {
        InventoryManager.Store(data.inventory);
        PickupMemory.Replace(data.pickups);
        ItemPickup.HideTaken();
        ShelfStock.Import(data.shelves, ResolveItem);
        ShelfInventory.ApplyRemembered();
        LifeTime.SetHour(data.hour);
        ApplyCoins(data);

        if (data.hasPlayer)
            PlacePlayer(new Vector3(data.x, data.y, data.z));

        if (ShelfUI.Instance != null && ShelfUI.Instance.IsOpen)
            ShelfUI.Instance.Refresh();
    }

    static void PlacePlayer(Vector3 position)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        player.transform.position = position;
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (body == null)
            return;

        body.position = position;
        body.linearVelocity = Vector2.zero;
    }

    static void ApplyCoins(GameSaveData data)
    {
        if (data.version < 2)
            PlayerCoin.Set(PlayerCoin.StartingAmount);
        else
            PlayerCoin.Set(data.coins);
    }

    static ItemData ResolveItem(string itemId)
    {
        if (InventoryManager.Instance == null)
            return null;

        return InventoryManager.Instance.FindItem(itemId);
    }

    static bool TryRead(out GameSaveData data)
    {
        data = null;
        if (!File.Exists(SavePath))
        {
            Debug.Log("Chưa có file save.");
            return false;
        }

        try
        {
            data = JsonUtility.FromJson<GameSaveData>(File.ReadAllText(SavePath));
        }
        catch (Exception exception)
        {
            Debug.LogError("Không đọc được file save: " + exception.Message);
            return false;
        }

        if (data == null || string.IsNullOrEmpty(data.sceneName))
        {
            Debug.Log("File save không hợp lệ.");
            data = null;
            return false;
        }

        return true;
    }

    static bool SceneInBuild(string sceneName)
    {
        int count = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (Path.GetFileNameWithoutExtension(path) == sceneName)
                return true;
        }

        return false;
    }
}
