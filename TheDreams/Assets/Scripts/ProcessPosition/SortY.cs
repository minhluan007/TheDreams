using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SortY : MonoBehaviour
{
    [SerializeField] private int offset = 0;
    [SerializeField] private int multiplier = 100;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        spriteRenderer.sortingOrder =
            Mathf.RoundToInt(-transform.position.y * multiplier) + offset;
    }
}