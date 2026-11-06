using UnityEngine;

[CreateAssetMenu(
    fileName = "Pot_",
    menuName = "The Dreams/Inventory/Pot"
)]
public class PotItemData : ItemData
{
    [Tooltip("Prefab PlantBed (kind = Pot) se spawn khi dat.")]
    public GameObject potPrefab;

    [Tooltip("Chi dung de dat / trong, khong ban tren ke neu can.")]
    public bool plantingOnly = true;
}
