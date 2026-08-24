using UnityEngine;

public class LoopDrift : MonoBehaviour
{
    public enum DriftDirection
    {
        RightToLeft,
        LeftToRight
    }

    [Header("Vùng lặp")]
    [Tooltip("Cùng BoxCollider2D confiner với CameraMove. Để trống thì tự lấy từ CameraMove.")]
    [SerializeField] Collider2D confiner;

    [Header("Chuyển động")]
    [Tooltip("RightToLeft: phải → trái. LeftToRight: trái → phải.")]
    [SerializeField] DriftDirection direction = DriftDirection.RightToLeft;
    [SerializeField] float speed = 0.6f;

    [Header("Wrap")]
    [Tooltip("Thêm khoảng trống ngoài collider trước khi teleport sang phía kia.")]
    [SerializeField] float wrapPadding = 0f;

    SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        ResolveConfiner();
    }

    void Update()
    {
        if (speed <= 0f)
            return;

        float sign = direction == DriftDirection.RightToLeft ? -1f : 1f;
        transform.position += Vector3.right * sign * speed * Time.deltaTime;

        WrapIfOutside();
    }

    void ResolveConfiner()
    {
        if (confiner != null)
            return;

        CameraMove cameraMove = FindFirstObjectByType<CameraMove>();
        if (cameraMove != null)
            confiner = cameraMove.confiner;
    }

    void WrapIfOutside()
    {
        if (confiner == null)
            return;

        Bounds area = confiner.bounds;
        GetVisualX(out float visualMinX, out float visualMaxX);

        if (direction == DriftDirection.RightToLeft)
        {
            if (visualMaxX < area.min.x - wrapPadding)
                ShiftX((area.max.x + wrapPadding) - visualMinX);
        }
        else if (visualMinX > area.max.x + wrapPadding)
        {
            ShiftX((area.min.x - wrapPadding) - visualMaxX);
        }
    }

    void GetVisualX(out float minX, out float maxX)
    {
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            Bounds b = spriteRenderer.bounds;
            minX = b.min.x;
            maxX = b.max.x;
            return;
        }

        minX = transform.position.x;
        maxX = transform.position.x;
    }

    void ShiftX(float delta)
    {
        Vector3 position = transform.position;
        position.x += delta;
        transform.position = position;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (confiner == null)
            return;

        Bounds b = confiner.bounds;
        Gizmos.color = new Color(0.4f, 0.75f, 1f, 0.9f);
        Gizmos.DrawWireCube(b.center, b.size);
    }
#endif
}
