using System;
using UnityEngine;

// El oro de una unidad. Se gana con kills y al terminar cada ronda.
public class Wallet : MonoBehaviour
{
    [SerializeField] int startingGold = 0;

    public int Gold { get; private set; }
    public int TotalEarned { get; private set; } // Para las estadísticas del final
    public event Action<int> OnGoldChanged;

    void Awake()
    {
        Gold = startingGold;
    }

    public void Add(int amount)
    {
        Gold += amount;
        if (amount > 0) TotalEarned += amount;
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
