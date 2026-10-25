using System.Collections.Generic;
using UnityEngine;

public class NPCMove : MonoBehaviour
{
    [SerializeField] float moveSpeed = 3f;
    [SerializeField] Animator animator;
    [SerializeField] Rigidbody2D rb;
    [SerializeField] float arriveDistance = 0.15f;
    [SerializeField] Transform[] waypoints;

    Vector2[] fallbackPoints;
    int index;
    Vector2? overrideTarget;
    bool routeBound;

    public bool HasReachedEnd { get; private set; }

    void Start()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (!routeBound)
            BindRouteForScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void BindNamedPoints(string[] names)
    {
        if (names == null || names.Length == 0)
            return;

        var found = new List<Vector2>(names.Length);
        for (int i = 0; i < names.Length; i++)
        {
            GameObject point = GameObject.Find(names[i]);
            if (point == null)
            {
                Debug.LogWarning("NPC khong thay diem di '" + names[i] + "'.");
                continue;
            }

            found.Add(point.transform.position);
        }

        if (found.Count == 0)
            return;

        routeBound = true;
        waypoints = null;
        fallbackPoints = found.ToArray();
        index = 0;
        overrideTarget = null;
        HasReachedEnd = false;
    }

    public void BindRouteForScene(string sceneName)
    {
        overrideTarget = null;
        HasReachedEnd = false;
        index = 0;

        NPCRoute route = NPCRoute.FindForScene(sceneName);
        if (route != null && route.HasPoints)
        {
            waypoints = route.Points;
            fallbackPoints = null;
            return;
        }

        waypoints = null;
        fallbackPoints = DefaultPointsFor(sceneName);
    }

    public void SetTarget(Vector2 worldPosition)
    {
        overrideTarget = worldPosition;
        HasReachedEnd = false;
    }

    public void ClearTarget()
    {
        overrideTarget = null;
    }

    void Update()
    {
        Vector2 move = GetDesiredMove();
        float speed = move.magnitude;
        bool isMoving = speed > 0.01f;

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetFloat("Run", isMoving ? speed : 0f);
            animator.SetFloat("Idle", isMoving ? 0f : 1f);
        }

        SetDirection(move.x);
    }

    void FixedUpdate()
    {
        if (rb == null)
            return;

        Vector2 move = GetDesiredMove();
        rb.linearVelocity = move * moveSpeed;
    }

    Vector2 GetDesiredMove()
    {
        if (!TryGetDestination(out Vector2 destination))
            return Vector2.zero;

        Vector2 to = destination - (Vector2)transform.position;
        if (to.magnitude <= arriveDistance)
        {
            Advance();
            return Vector2.zero;
        }

        return to.normalized;
    }

    bool TryGetDestination(out Vector2 destination)
    {
        if (overrideTarget.HasValue)
        {
            destination = overrideTarget.Value;
            return true;
        }

        if (waypoints != null && index < waypoints.Length && waypoints[index] != null)
        {
            destination = waypoints[index].position;
            return true;
        }

        if (fallbackPoints != null && index < fallbackPoints.Length)
        {
            destination = fallbackPoints[index];
            return true;
        }

        destination = default;
        return false;
    }

    void Advance()
    {
        if (overrideTarget.HasValue)
        {
            overrideTarget = null;
            HasReachedEnd = true;
            return;
        }

        index++;
        int count = waypoints != null ? waypoints.Length
            : fallbackPoints != null ? fallbackPoints.Length
            : 0;

        if (index >= count)
            HasReachedEnd = true;
    }

    void SetDirection(float moveX)
    {
        if (Mathf.Abs(moveX) <= 0.01f)
            return;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * Mathf.Sign(moveX);
        transform.localScale = scale;
    }

    static Vector2[] DefaultPointsFor(string sceneName)
    {
        if (sceneName == "BaseStore")
        {
            return new[]
            {
                new Vector2(2f, -28.58f),
                new Vector2(6f, -28.4f),
                new Vector2(9.5f, -28.12f)
            };
        }

        if (sceneName == "InStore")
        {
            return new[]
            {
                new Vector2(-2.26f, -6.5f),
                new Vector2(-8f, -4.5f),
                new Vector2(-12f, -1.5f)
            };
        }

        return null;
    }
}
