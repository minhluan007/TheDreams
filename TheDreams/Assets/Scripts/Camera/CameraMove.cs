using UnityEngine;

public class CameraMove : MonoBehaviour
{
    public Transform target;
    [Tooltip("Càng nhỏ camera càng bám sát player.")]
    public float smoothTime = 0.05f;
    public Vector3 offset = new Vector3(0f, 0f, -10f);
    public float pixelsPerUnit = 16f;

    Vector3 smoothPosition;
    Vector3 velocity;
    bool initialized;

    void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desired = target.position + offset;

        if (!initialized)
        {
            smoothPosition = desired;
            velocity = Vector3.zero;
            initialized = true;
        }

        // Smooth trên vị trí thật (không snap) để camera không bị trễ / giật.
        smoothPosition = Vector3.SmoothDamp(
            smoothPosition,
            desired,
            ref velocity,
            smoothTime
        );

        // Chỉ snap khi gán lên transform để pixel art không bị mờ.
        transform.position = SnapToPixel(smoothPosition);
    }

    Vector3 SnapToPixel(Vector3 position)
    {
        float unitsPerPixel = 1f / pixelsPerUnit;
        position.x = Mathf.Round(position.x / unitsPerPixel) * unitsPerPixel;
        position.y = Mathf.Round(position.y / unitsPerPixel) * unitsPerPixel;
        return position;
    }
}
