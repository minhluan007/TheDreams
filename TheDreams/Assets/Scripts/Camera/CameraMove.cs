using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMove : MonoBehaviour
{
    public Transform target;
    [Tooltip("Càng nhỏ camera càng bám sát khi player ra khỏi vùng chết.")]
    public float smoothTime = 0.05f;
    public Vector3 offset = new Vector3(0f, 0f, -10f);
    [Tooltip("Nửa kích thước vùng chết (world units). Player đi trong vùng này thì camera đứng yên.")]
    public Vector2 deadZone = new Vector2(2f, 1.5f);
    public float pixelsPerUnit = 16f;

    [Header("Giới hạn vùng nhìn")]
    [Tooltip("BoxCollider2D (Is Trigger) ôm sát nền/map. Camera sẽ không chiếu ra ngoài collider này.")]
    public Collider2D confiner;

    [Header("Zoom")]
    [Tooltip("Giữ - / Numpad- để zoom ra xem trời. Giữ + / = / Numpad+ để zoom vào.")]
    public float maxZoom = 16f;
    public float zoomSpeed = 8f;
    public float zoomSmoothTime = 0.12f;

    Camera cam;
    float defaultSize;
    float targetSize;
    float sizeVelocity;
    Vector3 smoothPosition;
    Vector3 velocity;
    bool initialized;

    void Awake()
    {
        cam = GetComponent<Camera>();
        defaultSize = cam != null ? cam.orthographicSize : 8f;
        targetSize = defaultSize;
    }

    void LateUpdate()
    {
        UpdateZoom();

        if (target == null)
            return;

        if (!initialized)
        {
            smoothPosition = target.position + offset;
            velocity = Vector3.zero;
            initialized = true;
        }

        Vector3 focus = smoothPosition - offset;
        Vector3 player = target.position;
        Vector3 desiredFocus = focus;

        float dx = player.x - focus.x;
        float dy = player.y - focus.y;

        // Chỉ kéo camera khi player vượt biên vùng chết.
        if (dx > deadZone.x)
            desiredFocus.x = player.x - deadZone.x;
        else if (dx < -deadZone.x)
            desiredFocus.x = player.x + deadZone.x;

        if (dy > deadZone.y)
            desiredFocus.y = player.y - deadZone.y;
        else if (dy < -deadZone.y)
            desiredFocus.y = player.y + deadZone.y;

        Vector3 desired = desiredFocus + offset;
        // Zoom ra thì phần nhìn thêm nằm phía trên (trời), đáy khung vẫn sát đất.
        desired.y += ZoomLift;

        smoothPosition = Vector3.SmoothDamp(
            smoothPosition,
            desired,
            ref velocity,
            smoothTime
        );

        smoothPosition = ClampToConfiner(smoothPosition);
        transform.position = SnapToPixel(smoothPosition);
    }

    float ZoomLift => cam != null ? Mathf.Max(0f, cam.orthographicSize - defaultSize) : 0f;

    void UpdateZoom()
    {
        if (cam == null || !cam.orthographic)
            return;

        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.minusKey.isPressed || kb.numpadMinusKey.isPressed)
                targetSize += zoomSpeed * Time.deltaTime;

            if (kb.equalsKey.isPressed || kb.numpadPlusKey.isPressed)
                targetSize -= zoomSpeed * Time.deltaTime;
        }

        targetSize = Mathf.Clamp(targetSize, defaultSize, maxZoom);
        cam.orthographicSize = Mathf.SmoothDamp(
            cam.orthographicSize,
            targetSize,
            ref sizeVelocity,
            zoomSmoothTime
        );
    }

    Vector3 ClampToConfiner(Vector3 position)
    {
        if (confiner == null)
            return position;

        if (cam == null)
            cam = GetComponent<Camera>();
        if (cam == null || !cam.orthographic)
            return position;

        Bounds b = confiner.bounds;
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        float minX = b.min.x + halfW;
        float maxX = b.max.x - halfW;
        float minY = b.min.y + halfH;
        float maxY = b.max.y - halfH;

        if (minX > maxX)
            position.x = b.center.x;
        else
            position.x = Mathf.Clamp(position.x, minX, maxX);

        bool seeingSky = halfH > defaultSize + 0.01f;

        // Zoom xem trời: không kẹp cạnh trên, chỉ không cho lộ dưới đất.
        if (seeingSky)
            position.y = Mathf.Max(position.y, minY);
        else if (minY > maxY)
            position.y = b.center.y;
        else
            position.y = Mathf.Clamp(position.y, minY, maxY);

        return position;
    }

    Vector3 SnapToPixel(Vector3 position)
    {
        float unitsPerPixel = 1f / pixelsPerUnit;
        position.x = Mathf.Round(position.x / unitsPerPixel) * unitsPerPixel;
        position.y = Mathf.Round(position.y / unitsPerPixel) * unitsPerPixel;
        return position;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying && initialized
            ? smoothPosition - offset
            : (target != null ? target.position : transform.position - offset);

        Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.35f);
        Gizmos.DrawWireCube(center, new Vector3(deadZone.x * 2f, deadZone.y * 2f, 0f));

        if (confiner != null)
        {
            Bounds b = confiner.bounds;
            Gizmos.color = new Color(1f, 0.55f, 0.1f, 0.9f);
            Gizmos.DrawWireCube(b.center, b.size);
        }
    }
#endif
}
