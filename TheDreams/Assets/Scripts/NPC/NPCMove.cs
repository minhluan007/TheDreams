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
    bool paused;
    float holdUntil;
    Vector2 currentMove;

    public bool HasReachedEnd { get; private set; }
    public int Index => index;
    public float dwellOnArrival;
    public bool IsHolding => Time.time < holdUntil;

    void Awake()
    {
        Cache();
    }

    void Start()
    {
        Cache();

        if (routeBound)
            return;

        NPC npc = GetComponent<NPC>();
        if (npc != null && npc.Actor != null)
            return;

        BindRouteForScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    void Cache()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    public void LockRoute()
    {
        routeBound = true;
    }

    public bool BindNamedPoints(string[] names, int startIndex)
    {
        routeBound = true;
        paused = false;
        HasReachedEnd = false;
        overrideTarget = null;
        waypoints = null;

        if (names == null || names.Length == 0)
            return false;

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
            return false;

        fallbackPoints = found.ToArray();
        if (startIndex >= found.Count)
        {
            index = found.Count;
            HasReachedEnd = true;
        }
        else
        {
            index = Mathf.Max(0, startIndex);
        }

        return true;
    }

    public void BindNamedPoints(string[] names)
    {
        BindNamedPoints(names, 0);
    }

    public void BindRouteForScene(string sceneName)
    {
        overrideTarget = null;
        HasReachedEnd = false;
        index = 0;
        routeBound = true;

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
        routeBound = true;
        overrideTarget = worldPosition;
        HasReachedEnd = false;
        paused = false;
    }

    public void ClearTarget()
    {
        overrideTarget = null;
    }

    public void StopAndStay()
    {
        routeBound = true;
        paused = true;
        overrideTarget = null;
        dwellOnArrival = 0f;
        currentMove = Vector2.zero;
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    public void SetPaused(bool value)
    {
        paused = value;
        if (paused)
        {
            currentMove = Vector2.zero;
            if (rb != null)
                rb.linearVelocity = Vector2.zero;
        }
    }

    public void Hold(float seconds)
    {
        if (seconds <= 0f)
            return;

        holdUntil = Time.time + seconds;
        currentMove = Vector2.zero;
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    public void ReleaseHold()
    {
        holdUntil = 0f;
    }

    public void FaceTowards(Vector2 world)
    {
        float dx = world.x - transform.position.x;
        if (Mathf.Abs(dx) <= 0.01f)
            return;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * Mathf.Sign(dx);
        transform.localScale = scale;
    }

    void Update()
    {
        float speed = currentMove.magnitude;
        bool isMoving = speed > 0.01f;

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetFloat("Run", isMoving ? speed : 0f);
            animator.SetFloat("Idle", isMoving ? 0f : 1f);
        }

        SetDirection(currentMove.x);
    }

    void FixedUpdate()
    {
        if (rb == null)
            return;

        currentMove = GetDesiredMove();
        rb.linearVelocity = currentMove * moveSpeed;
    }

    Vector2 GetDesiredMove()
    {
        if (paused || IsHolding)
            return Vector2.zero;

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
            if (dwellOnArrival > 0f)
            {
                Hold(dwellOnArrival);
                dwellOnArrival = 0f;
            }

            return;
        }

        index++;
        int count = waypoints != null ? waypoints.Length
            : fallbackPoints != null ? fallbackPoints.Length
            : 0;

        if (dwellOnArrival > 0f)
            Hold(dwellOnArrival);

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
