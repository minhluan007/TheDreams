using UnityEngine;

public class CameraMove : MonoBehaviour
{
    public Transform target;
    [Tooltip("Càng nhỏ camera càng bám sát khi player ra khỏi vùng chết.")]
    public float smoothTime = 0.05f;
    public Vector3 offset = new Vector3(0f, 0f, -10f);
    [Tooltip("Nửa kích thước vùng chết (world units). Player đi trong vùng này thì camera đứng yên.")]
    public Vector2 deadZone = new Vector2(2f, 1.5f);
    public float pixelsPerUnit = 16f;

    Vector3 smoothPosition;
    Vector3 velocity;
    bool initialized;

    void LateUpdate()
    {
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

        smoothPosition = Vector3.SmoothDamp(
            smoothPosition,
            desired,
            ref velocity,
            smoothTime
        );

        transform.position = SnapToPixel(smoothPosition);
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
    }
#endif
}
