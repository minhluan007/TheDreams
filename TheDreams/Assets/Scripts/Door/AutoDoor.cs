using UnityEngine;

public class AutoDoor : MonoBehaviour
{
    [Header("Door Objects")]
    [SerializeField] private Transform leftDoor;
    [SerializeField] private Transform rightDoor;

    [Header("Open Position")]
    [SerializeField] private Vector3 leftOpenOffset = new Vector3(-1.5f, 0f, 0f);
    [SerializeField] private Vector3 rightOpenOffset = new Vector3(1.5f, 0f, 0f);

    [Header("Settings")]
    [SerializeField] private float openSpeed = 3f;

    private Vector3 leftClosedPosition;
    private Vector3 rightClosedPosition;

    private bool isOpen;

    private void Start()
    {
        leftClosedPosition = leftDoor.localPosition;
        rightClosedPosition = rightDoor.localPosition;
    }

    private void Update()
    {
        Vector3 leftTarget = isOpen
            ? leftClosedPosition + leftOpenOffset
            : leftClosedPosition;

        Vector3 rightTarget = isOpen
            ? rightClosedPosition + rightOpenOffset
            : rightClosedPosition;

        leftDoor.localPosition = Vector3.Lerp(
            leftDoor.localPosition,
            leftTarget,
            Time.deltaTime * openSpeed
        );

        rightDoor.localPosition = Vector3.Lerp(
            rightDoor.localPosition,
            rightTarget,
            Time.deltaTime * openSpeed
        );
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isOpen = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isOpen = false;
        }
    }
}