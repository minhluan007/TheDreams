using UnityEngine;

[CreateAssetMenu(
    fileName = "Item_",
    menuName = "The Dreams/Inventory/Item"
)]

public class ItemData : ScriptableObject
{
   public string itemId;
   public string itemName;
   public Sprite icon;
   public int maxStack = 99;

   [Min(0)]
   public int coinPrice;

   public string Label => string.IsNullOrEmpty(itemName) ? itemId : itemName;
}
