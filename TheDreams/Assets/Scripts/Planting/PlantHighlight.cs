using UnityEngine;

public static class PlantHighlight
{
    static Sprite ringSprite;

    public static void ClearAll()
    {
        PutSnapPoint[] snaps = Object.FindObjectsByType<PutSnapPoint>(FindObjectsSortMode.None);
        for (int i = 0; i < snaps.Length; i++)
        {
            if (snaps[i] != null)
                snaps[i].SetHighlighted(false, false);
        }

        PlantBed[] beds = Object.FindObjectsByType<PlantBed>(FindObjectsSortMode.None);
        for (int i = 0; i < beds.Length; i++)
        {
            if (beds[i] != null)
                beds[i].SetHighlighted(false, false);
        }
    }

    public static void ShowAvailableSnaps(PutSnapPoint hover = null)
    {
        PutSnapPoint[] snaps = Object.FindObjectsByType<PutSnapPoint>(FindObjectsSortMode.None);
        for (int i = 0; i < snaps.Length; i++)
        {
            PutSnapPoint snap = snaps[i];
            if (snap == null)
                continue;

            bool available = snap.CanAccept(PlantBedKind.Pot);
            snap.SetHighlighted(available, available && snap == hover);
        }

        PlantBed[] beds = Object.FindObjectsByType<PlantBed>(FindObjectsSortMode.None);
        for (int i = 0; i < beds.Length; i++)
        {
            if (beds[i] != null)
                beds[i].SetHighlighted(false, false);
        }
    }

    public static void ShowEmptyBeds(PlantBed hover = null)
    {
        PutSnapPoint[] snaps = Object.FindObjectsByType<PutSnapPoint>(FindObjectsSortMode.None);
        for (int i = 0; i < snaps.Length; i++)
        {
            if (snaps[i] != null)
                snaps[i].SetHighlighted(false, false);
        }

        PlantBed[] beds = Object.FindObjectsByType<PlantBed>(FindObjectsSortMode.None);
        for (int i = 0; i < beds.Length; i++)
        {
            PlantBed bed = beds[i];
            if (bed == null)
                continue;

            bool available = bed.IsEmpty;
            bed.SetHighlighted(available, available && bed == hover);
        }
    }

    public static Sprite RingSprite()
    {
        if (ringSprite != null)
            return ringSprite;

        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color solid = Color.white;
        float outer = size * 0.48f;
        float inner = size * 0.34f;
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                texture.SetPixel(x, y, dist <= outer && dist >= inner ? solid : clear);
            }
        }

        texture.Apply();
        ringSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        return ringSprite;
    }
}
