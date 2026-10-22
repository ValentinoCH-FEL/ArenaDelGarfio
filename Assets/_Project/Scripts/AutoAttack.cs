using System;
using UnityEngine;

// Golpe automático cuerpo a cuerpo cuando un enemigo está muy cerca
// (por ejemplo, después de jalarlo con el garfio).
public class AutoAttack : MonoBehaviour
{
    public float range = 1.8f;
    public float damage = 15f;
    public float interval = 1f;

    public event Action OnAttack;

    float nextAttackTime;
    Unit owner;

    void Awake()
    {
        owner = GetComponent<Unit>();
    }

    void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
        if (owner == null || !owner.IsAlive || !owner.Status.CanAct) return;
        if (Time.time < nextAttackTime) return;

        Unit best = null;
        float bestDist = range;
        foreach (var u in Unit.All)
        {
            if (u == null || !u.IsAlive || !owner.IsEnemyOf(u) || !u.Status.IsTargetable) continue;
            Vector3 d = u.transform.position - transform.position;
            d.y = 0f;
            float dist = d.magnitude;
            if (dist <= bestDist) { best = u; bestDist = dist; }
        }

        if (best == null) return;

        nextAttackTime = Time.time + interval;
        Vector3 look = best.transform.position - transform.position;
        look.y = 0f;
        if (look.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(look);

        OnAttack?.Invoke();
        AudioManager.Play(Sfx.Golpe);
        best.Health.TakeDamage(damage * owner.Status.DamageMultiplier, gameObject);
    }
}
