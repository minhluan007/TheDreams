using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    public float moveSpeed = 5f;
    public Animator animator;
    public Rigidbody2D rb;
    public InputActionAsset inputActions;

    InputAction moveAction;
    bool externalControl;
    Vector2 externalMove;

    void Awake()
    {
        InputActionMap playerMap = inputActions != null
            ? inputActions.FindActionMap("PlayerInput")
            : InputSystem.actions.FindActionMap("PlayerInput");

        moveAction = playerMap.FindAction("Move");
    }

    void OnEnable()
    {
        moveAction?.Enable();
    }

    void OnDisable()
    {
        moveAction?.Disable();
    }

    void Start()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public void SetExternalControl(bool enabled)
    {
        externalControl = enabled;
        if (!enabled)
            externalMove = Vector2.zero;
    }

    public void ApplyExternalMotion(Vector2 direction)
    {
        externalMove = direction;
        float speed = direction.magnitude;
        if (animator != null)
        {
            SetAnimation("Run", speed);
            SetAnimation("Idle", 1f - speed);
        }

        SetDirection(direction.x);
    }

    void Update()
    {
        if (externalControl)
            return;

        Vector2 move = GetMove();
        float speed = move.magnitude;

        if (animator != null)
        {
            SetAnimation("Run", speed);
            SetAnimation("Idle", 1f - speed);
        }

        SetDirection(move.x);
    }

    void FixedUpdate()
    {
        if (externalControl)
            return;

        Vector2 move = GetMove();
        rb.linearVelocity = move * moveSpeed;
    }

    Vector2 GetMove()
    {
        return moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
    }

    void SetAnimation(string animationName, float value)
    {
        if (animator != null)
            animator.SetFloat(animationName, value);
    }

    void SetDirection(float moveX)
    {
        if (Mathf.Abs(moveX) > 0.01f)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * Mathf.Sign(moveX);
            transform.localScale = scale;
        }
    }
}
