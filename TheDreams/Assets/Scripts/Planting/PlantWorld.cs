using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class SavedPlantBed
{
    public string key;
    public string kind;
    public float x;
    public float y;
    public float z;
    public string seedId;
    public int plantedAbsoluteDay;
    public int stage;
    public bool playerPlaced;
    public string snapKey;
    public string potItemId;
}

public static class PlantWorld
{
    static readonly List<PlantBed> live = new List<PlantBed>();
    static readonly Dictionary<string, SavedPlantBed> memory = new Dictionary<string, SavedPlantBed>();
    static GameObject potPrefabFallback;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetSession()
    {
        live.Clear();
        memory.Clear();
        potPrefabFallback = null;
    }

    public static void SetPotPrefabFallback(GameObject prefab)
    {
        if (prefab != null)
            potPrefabFallback = prefab;
    }

    public static void Clear()
    {
        memory.Clear();
    }

    public static void Register(PlantBed bed)
    {
        if (bed == null || live.Contains(bed))
            return;

        live.Add(bed);
    }

    public static void Unregister(PlantBed bed)
    {
        live.Remove(bed);
    }

    public static string BedKey(string sceneName, Vector3 position, PlantBedKind kind)
    {
        int x = Mathf.RoundToInt(position.x * 100f);
        int y = Mathf.RoundToInt(position.y * 100f);
        return sceneName + "|bed|" + kind + "|" + x + "|" + y;
    }

    public static string SnapKey(string sceneName, Vector3 position)
    {
        int x = Mathf.RoundToInt(position.x * 100f);
        int y = Mathf.RoundToInt(position.y * 100f);
        return sceneName + "|snap|" + x + "|" + y;
    }

    public static void Remember(PlantBed bed)
    {
        if (bed == null)
            return;

        SavedPlantBed saved = ToSaved(bed);
        memory[saved.key] = saved;
    }

    public static void RememberAllLive()
    {
        for (int i = 0; i < live.Count; i++)
        {
            if (live[i] != null)
                Remember(live[i]);
        }
    }

    public static SavedPlantBed[] Export()
    {
        RememberAllLive();
        SavedPlantBed[] saved = new SavedPlantBed[memory.Count];
        int index = 0;
        foreach (KeyValuePair<string, SavedPlantBed> pair in memory)
            saved[index++] = pair.Value;

        return saved;
    }

    public static void Import(SavedPlantBed[] saved, Func<string, ItemData> resolve)
    {
        memory.Clear();
        if (saved == null)
            return;

        for (int i = 0; i < saved.Length; i++)
        {
            SavedPlantBed bed = saved[i];
            if (bed == null || string.IsNullOrEmpty(bed.key))
                continue;

            memory[bed.key] = bed;
        }

        ApplyScene(resolve);
    }

    public static void ApplyScene(Func<string, ItemData> resolve)
    {
        string sceneName = SceneManager.GetActiveScene().name;

        PlantBed[] beds = UnityEngine.Object.FindObjectsByType<PlantBed>(FindObjectsSortMode.None);
        for (int i = 0; i < beds.Length; i++)
        {
            PlantBed bed = beds[i];
            if (bed == null)
                continue;

            if (bed.PlayerPlaced)
            {
                UnityEngine.Object.Destroy(bed.gameObject);
                continue;
            }

            if (!memory.TryGetValue(bed.BedKey, out SavedPlantBed saved))
            {
                bed.ClearPlant(false);
                continue;
            }

            SeedItemData seed = resolve != null ? resolve(saved.seedId) as SeedItemData : null;
            if (seed == null)
                bed.ClearPlant(false);
            else
                bed.Restore(seed, saved.plantedAbsoluteDay, saved.stage);
        }

        foreach (KeyValuePair<string, SavedPlantBed> pair in memory)
        {
            SavedPlantBed saved = pair.Value;
            if (saved == null || !saved.playerPlaced)
                continue;

            if (!saved.key.StartsWith(sceneName + "|"))
                continue;

            SpawnSavedPot(saved, resolve);
        }

        PutSnapPoint[] snaps = UnityEngine.Object.FindObjectsByType<PutSnapPoint>(FindObjectsSortMode.None);
        for (int i = 0; i < snaps.Length; i++)
        {
            PutSnapPoint snap = snaps[i];
            if (snap == null)
                continue;

            PlantBed occupant = null;
            for (int b = 0; b < live.Count; b++)
            {
                PlantBed bed = live[b];
                if (bed != null && bed.SnapKey == snap.SnapKey)
                {
                    occupant = bed;
                    break;
                }
            }

            if (occupant != null)
                snap.Occupy(occupant);
            else
                snap.Clear();
        }
    }

    public static PutSnapPoint FindNearestSnap(Vector3 position, float range, PlantBedKind kind)
    {
        PutSnapPoint[] snaps = UnityEngine.Object.FindObjectsByType<PutSnapPoint>(FindObjectsSortMode.None);
        PutSnapPoint best = null;
        float bestDist = range;
        for (int i = 0; i < snaps.Length; i++)
        {
            PutSnapPoint snap = snaps[i];
            if (snap == null || !snap.CanAccept(kind))
                continue;

            float dist = Vector2.Distance(position, snap.transform.position);
            if (dist > bestDist)
                continue;

            bestDist = dist;
            best = snap;
        }

        return best;
    }

    public static PutSnapPoint FindSnap(string key)
    {
        if (string.IsNullOrEmpty(key))
            return null;

        PutSnapPoint[] snaps = UnityEngine.Object.FindObjectsByType<PutSnapPoint>(FindObjectsSortMode.None);
        for (int i = 0; i < snaps.Length; i++)
        {
            if (snaps[i] != null && snaps[i].SnapKey == key)
                return snaps[i];
        }

        return null;
    }

    public static PlantBed FindNearestEmpty(Vector3 position, float range)
    {
        PlantBed best = null;
        float bestDist = range;
        for (int i = 0; i < live.Count; i++)
        {
            PlantBed bed = live[i];
            if (bed == null || !bed.IsEmpty)
                continue;

            float dist = Vector2.Distance(position, bed.transform.position);
            if (dist > bestDist)
                continue;

            bestDist = dist;
            best = bed;
        }

        return best;
    }

    public static PlantBed PlacePot(PotItemData pot, PutSnapPoint snap, Vector3 fallbackPosition)
    {
        if (pot == null)
            return null;

        GameObject prefab = pot.potPrefab != null ? pot.potPrefab : potPrefabFallback;
        if (prefab == null)
        {
            Debug.LogWarning("PotItemData thieu potPrefab.");
            return null;
        }

        Vector3 position = snap != null ? snap.transform.position : fallbackPosition;
        string snapKey = snap != null ? snap.SnapKey : "";

        GameObject instance = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);
        instance.SetActive(true);
        PlantBed bed = instance.GetComponent<PlantBed>();
        if (bed == null)
            bed = instance.AddComponent<PlantBed>();

        string potId = InventoryManager.ItemKey(pot);
        bed.ConfigurePlacement(PlantBedKind.Pot, true, snapKey, potId);
        bed.ApplyPotLook(ResolvePotSprite(pot));
        if (snap != null)
            snap.Occupy(bed);

        SetPotPrefabFallback(prefab);
        Remember(bed);
        return bed;
    }

    static Sprite ResolvePotSprite(PotItemData pot)
    {
        if (pot == null)
            return null;

        if (pot.icon != null)
            return pot.icon;

        if (pot.potPrefab == null)
            return null;

        SpriteRenderer renderer = pot.potPrefab.GetComponent<SpriteRenderer>();
        return renderer != null ? renderer.sprite : null;
    }

    static void SpawnSavedPot(SavedPlantBed saved, Func<string, ItemData> resolve)
    {
        ItemData potItem = resolve != null ? resolve(saved.potItemId) : null;
        PotItemData pot = potItem as PotItemData;
        GameObject prefab = pot != null && pot.potPrefab != null ? pot.potPrefab : potPrefabFallback;
        if (prefab == null)
        {
            Debug.LogWarning("Khong spawn duoc chau da luu: thieu prefab.");
            return;
        }

        Vector3 position = new Vector3(saved.x, saved.y, saved.z);
        GameObject instance = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);
        instance.SetActive(true);
        PlantBed bed = instance.GetComponent<PlantBed>();
        if (bed == null)
            bed = instance.AddComponent<PlantBed>();

        bed.ConfigurePlacement(PlantBedKind.Pot, true, saved.snapKey, saved.potItemId);
        bed.ApplyPotLook(ResolvePotSprite(pot));
        SeedItemData seed = resolve != null ? resolve(saved.seedId) as SeedItemData : null;
        if (seed != null)
            bed.Restore(seed, saved.plantedAbsoluteDay, saved.stage);

        if (!string.IsNullOrEmpty(saved.snapKey))
        {
            PutSnapPoint snap = FindSnap(saved.snapKey);
            if (snap != null)
                snap.Occupy(bed);
        }

        Remember(bed);
    }

    static SavedPlantBed ToSaved(PlantBed bed)
    {
        Vector3 position = bed.transform.position;
        return new SavedPlantBed
        {
            key = bed.BedKey,
            kind = bed.Kind.ToString(),
            x = position.x,
            y = position.y,
            z = position.z,
            seedId = bed.Planted != null ? InventoryManager.ItemKey(bed.Planted) : "",
            plantedAbsoluteDay = bed.PlantedAbsoluteDay,
            stage = bed.Stage,
            playerPlaced = bed.PlayerPlaced,
            snapKey = bed.SnapKey ?? "",
            potItemId = bed.SourcePotItemId ?? ""
        };
    }
}
