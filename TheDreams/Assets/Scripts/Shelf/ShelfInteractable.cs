
using UnityEngine;
using UnityEngine.InputSystem;

public class ShelfInteractable : MonoBehaviour
{
    private bool playerInRange;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = false;

        if (ShelfUI.Instance != null)
            ShelfUI.Instance.Close();
    }

    private void Update()
    {
        if (!playerInRange)
            return;

        if (Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            ShelfUI.Instance?.Open(
                GetComponent<ShelfInventory>()
            );
        }
    }
}
