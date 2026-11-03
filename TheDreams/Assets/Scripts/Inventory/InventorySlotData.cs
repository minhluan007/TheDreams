using UnityEngine;

[System.Serializable]
public class InventorySlotData
{
    public ItemData item;
    public int amount;

    public int coinPriceSell;
    public bool sellPriceSet;

    public bool HasItem => item != null && amount > 0;

    public int SellPrice
    {
        get
        {
            if (sellPriceSet)
                return Mathf.Max(0, coinPriceSell);

            if (item == null)
                return 0;

            return Mathf.Max(0, item.coinPriceSell);
        }
    }

    public void SetSellPrice(int price)
    {
        coinPriceSell = Mathf.Max(0, price);
        sellPriceSet = true;
    }

    public void Clear()
    {
        item = null;
        amount = 0;
        coinPriceSell = 0;
        sellPriceSet = false;
    }
}
