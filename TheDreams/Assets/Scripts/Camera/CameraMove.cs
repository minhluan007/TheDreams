using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMove : MonoBehaviour
{
    public float moveSpeed = 8f;
    public float smoothTime = 0.15f;

    private float targetX;
    private float velocityX;

    void Start()
    {
        targetX = transform.position.x;
    }

    void Update()
    {
        if (Keyboard.current.leftArrowKey.isPressed)
        {
            targetX -= moveSpeed * Time.deltaTime;
        }

        if (Keyboard.current.rightArrowKey.isPressed)
        {
            targetX += moveSpeed * Time.deltaTime;
        }

        float newX = Mathf.SmoothDamp(
            transform.position.x,
            targetX,
            ref velocityX,
            smoothTime
        );

        transform.position = new Vector3(
            newX,
            transform.position.y,
            transform.position.z
        );
    }
}