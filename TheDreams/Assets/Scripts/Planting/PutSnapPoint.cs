using UnityEngine;

public class PutSnapPoint : MonoBehaviour
{
    [SerializeField] PlantBedKind accepts = PlantBedKind.Pot;
    [SerializeField] bool occupied;

    PlantBed occupant;
    SpriteRenderer highlight;
    bool lit;
    bool hovered;

    public PlantBedKind Accepts => accepts;
    public bool IsOccupied => occupied && occupant != null;
    public PlantBed Occupant => occupant;
    public string SnapKey => PlantWorld.SnapKey(gameObject.scene.name, transform.position);

    public bool CanAccept(PlantBedKind kind)
    {
        return !IsOccupied && accepts == kind;
    }

    public void Occupy(PlantBed bed)
    {
        occupant = bed;
        occupied = bed != null;
        if (occupied)
            SetHighlighted(false, false);
    }

    public void Clear()
    {
        occupant = null;
        occupied = false;
    }

    public void SetHighlighted(bool on, bool hover)
    {
        lit = on;
        hovered = hover;
        EnsureHighlight();
        if (highlight == null)
            return;

        highlight.enabled = on;
        if (!on)
            return;

        float pulse = 0.75f + Mathf.Sin(Time.time * 6f) * 0.15f;
        Color color = hover
            ? new Color(1f, 0.95f, 0.35f, 0.95f)
            : new Color(0.35f, 1f, 0.55f, 0.85f);
        highlight.color = color;
        float scale = hover ? 1.35f : 1.1f * pulse;
        highlight.transform.localScale = new Vector3(scale, scale, 1f);
    }

    void LateUpdate()
    {
        if (lit)
            SetHighlighted(true, hovered);

        if (highlight != null && highlight.enabled)
            highlight.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f) + 20;
    }

    void EnsureHighlight()
    {
        if (highlight != null)
            return;

        Transform existing = transform.Find("SnapHighlight");
        if (existing != null)
        {
            highlight = existing.GetComponent<SpriteRenderer>();
            if (highlight != null)
                return;
        }

        GameObject go = new GameObject("SnapHighlight");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        highlight = go.AddComponent<SpriteRenderer>();
        highlight.sprite = PlantHighlight.RingSprite();
        highlight.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f) + 20;
        highlight.enabled = false;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = IsOccupied ? new Color(1f, 0.4f, 0.2f, 0.7f) : new Color(0.2f, 0.9f, 0.4f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, 0.25f);
    }
}
