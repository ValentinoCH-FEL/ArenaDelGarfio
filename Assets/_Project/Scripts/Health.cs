using System;
using UnityEngine;

// Vida de una unidad. Avisa cuando cambia y cuando muere (y quién la mató).
public class Health : MonoBehaviour
{
    [SerializeField] float maxHealth = 100f;

    public float Max => maxHealth;
    public float Current { get; private set; }
    public bool IsDead { get; private set; }

    public event Action<float, float> OnChanged;      // vida actual, vida máxima
    public event Action<Health, GameObject> OnDied;   // quién murió, quién lo mató

    void Awake()
    {
        Current = maxHealth;
    }

    public void TakeDamage(float amount, GameObject attacker)
    {
        if (IsDead || amount <= 0f) return;

        Current = Mathf.Max(0f, Current - amount);
        OnChanged?.Invoke(Current, maxHealth);

        if (Current <= 0f)
        {
            IsDead = true;
            OnDied?.Invoke(this, attacker);
        }
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        Current = Mathf.Min(maxHealth, Current + amount);
        OnChanged?.Invoke(Current, maxHealth);
    }

    public void AddMaxHealth(float amount)
    {
        maxHealth += amount;
        Current += amount;
        OnChanged?.Invoke(Current, maxHealth);
    }

    public void ResetHealth()
    {
        IsDead = false;
        Current = maxHealth;
        OnChanged?.Invoke(Current, maxHealth);
    }
}
