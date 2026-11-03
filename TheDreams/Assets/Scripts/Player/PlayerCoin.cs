using System;
using UnityEngine;

public static class PlayerCoin
{
    public const int StartingAmount = 1000;

    static int amount = StartingAmount;

    public static int Amount => amount;

    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetSession()
    {
        amount = StartingAmount;
        Changed = null;
    }

    public static void BeginNewGame()
    {
        Set(StartingAmount);
    }

    public static void Set(int value)
    {
        value = Mathf.Max(0, value);
        if (amount == value)
            return;

        amount = value;
        Changed?.Invoke();
    }

    public static void Add(int value)
    {
        if (value <= 0)
            return;

        long next = (long)amount + value;
        if (next > int.MaxValue)
            next = int.MaxValue;

        Set((int)next);
    }

    public static bool CanAfford(int value)
    {
        return value <= 0 || amount >= value;
    }

    public static bool TrySpend(int value)
    {
        if (value <= 0)
            return true;

        if (amount < value)
            return false;

        Set(amount - value);
        return true;
    }

    public static int Earn(ItemData item, int sellPrice = -1)
    {
        int price = sellPrice >= 0 ? Mathf.Max(0, sellPrice) : SellPriceOf(item);
        if (price > 0)
            Add(price);
        return price;
    }

    public static int SellPriceOf(ItemData item)
    {
        if (item == null)
            return 0;

        return Mathf.Max(0, item.coinPriceSell);
    }

    public static int PriceOf(ItemData item)
    {
        if (item == null)
            return 0;

        return Mathf.Max(0, item.coinPrice);
    }
}
