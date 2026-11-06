using System;
using UnityEngine;

[RequireComponent(typeof(PlayerMove))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerApproach : MonoBehaviour
{
    [SerializeField] float defaultStopDistance = 0.55f;

    PlayerMove move;
    Rigidbody2D body;
    Vector2 target;
    float stopDistance;
    Action onArrived;
    bool active;

    public bool IsApproaching => active;

    void Awake()
    {
        move = GetComponent<PlayerMove>();
        body = GetComponent<Rigidbody2D>();
    }

    public void GoTo(Vector2 worldTarget, Action arrived, float stop = -1f)
    {
        target = worldTarget;
        stopDistance = stop > 0f ? stop : defaultStopDistance;
        onArrived = arrived;
        active = true;
        if (move != null)
            move.SetExternalControl(true);
    }

    public void Cancel()
    {
        if (!active)
            return;

        active = false;
        onArrived = null;
        if (move != null)
            move.SetExternalControl(false);

        if (body != null)
            body.linearVelocity = Vector2.zero;
    }

    void FixedUpdate()
    {
        if (!active || body == null)
            return;

        Vector2 pos = body.position;
        Vector2 delta = target - pos;
        float distance = delta.magnitude;
        if (distance <= stopDistance)
        {
            body.linearVelocity = Vector2.zero;
            Finish();
            return;
        }

        float speed = move != null ? move.moveSpeed : 5f;
        body.linearVelocity = delta.normalized * speed;
        if (move != null)
            move.ApplyExternalMotion(delta.normalized);
    }

    void Finish()
    {
        Action callback = onArrived;
        active = false;
        onArrived = null;
        if (move != null)
            move.SetExternalControl(false);

        callback?.Invoke();
    }
}
