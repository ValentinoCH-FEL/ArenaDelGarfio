using System;
using UnityEngine;

// El oro de una unidad. Se gana con kills y al terminar cada ronda.
public class Wallet : MonoBehaviour
{
    [SerializeField] int startingGold = 0;

    public int Gold { get; private set; }
    public event Action<int> OnGoldChanged;

    void Awake()
    {
        Gold = startingGold;
    }

    public void Add(int amount)
    {
        Gold += amount;
        OnGoldChanged?.Invoke(Gold);
    }

    public bool TrySpend(int amount)
    {
        if (Gold < amount) return false;
        Gold -= amount;
        OnGoldChanged?.Invoke(Gold);
        return true;
    }
}
