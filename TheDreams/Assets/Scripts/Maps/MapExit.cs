using UnityEngine;

public class MapExit : MonoBehaviour
{
    [SerializeField] private string targetScene;
    [SerializeField] private string targetSpawnPoint;

    private bool canTransition = false;
    private bool isTransitioning = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!canTransition)
            return;

        if (isTransitioning)
            return;

        if (!other.CompareTag("Player"))
            return;

        isTransitioning = true;

        MapTransitionManager.Instance.LoadMap(
            targetScene,
            targetSpawnPoint
        );
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        canTransition = true;
    }
}