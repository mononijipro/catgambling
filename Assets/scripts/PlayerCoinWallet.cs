using UnityEngine;
using System;

public class PlayerCoinWallet : MonoBehaviour
{
    [SerializeField] private int coinCount;

    public int CoinCount => coinCount;
    public event Action<int> CoinsAdded;
    public event Action<int> CoinsLost;

    public void AddCoins(int amount)
    {
        int addedAmount = Mathf.Max(0, amount);
        if (addedAmount <= 0)
        {
            return;
        }

        coinCount += addedAmount;
        CoinsAdded?.Invoke(addedAmount);
    }

    public int RemoveCoins(int amount)
    {
        int removed = Mathf.Min(Mathf.Max(0, amount), coinCount);
        if (removed <= 0)
        {
            return 0;
        }

        coinCount -= removed;
        CoinsLost?.Invoke(removed);
        return removed;
    }
}
