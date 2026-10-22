using System.Collections.Generic;
using UnityEngine;

// La red en el aire: va recta y al tocar a un enemigo lo deja quieto unos segundos.
public class NetProjectile : MonoBehaviour
{
    public static readonly List<NetProjectile> Active = new List<NetProjectile>();

    [SerializeField] float hitRadius = 0.7f;
    [SerializeField] float spinSpeed = 540f;
    [SerializeField] GameObject hitEffectPrefab;

    NetAbility ability;
    Vector3 direction;
    float travelled;
    int hitMask;

    void Awake()
    {
        hitMask = ~LayerMask.GetMask("MuroRio", "Arbusto", "Ignore Raycast");
    }

    void OnEnable() { Active.Add(this); }
    void OnDestroy() { Active.Remove(this); }

    public void Launch(NetAbility netAbility, Vector3 dir)
    {
        ability = netAbility;
        direction = dir;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        bool matchOver = gm != null && (gm.State == GameState.FinRonda || gm.State == GameState.FinPartida);
        if (ability == null || matchOver) { Destroy(gameObject); return; }

        float step = ability.speed * Time.deltaTime;

        RaycastHit[] hits = Physics.SphereCastAll(transform.position, hitRadius, direction, step, hitMask, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            Unit unit = hit.collider.GetComponentInParent<Unit>();
            if (unit != null && (unit == ability.Owner || !unit.IsAlive || !unit.IsEnemyOf(ability.Owner) || !unit.Status.IsTargetable))
                continue; // Atraviesa al dueño, a los aliados, a los muertos y a los que están en fase

            if (unit != null)
            {
                unit.Status.Root(ability.rootDuration);
                unit.Health.TakeDamage(ability.damage, ability.gameObject);
            }
            if (hitEffectPrefab != null) Instantiate(hitEffectPrefab, hit.point, Quaternion.identity);
            Destroy(gameObject);
            return;
        }

        transform.position += direction * step;
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        travelled += step;
        if (travelled >= ability.range) Destroy(gameObject);
    }
}
