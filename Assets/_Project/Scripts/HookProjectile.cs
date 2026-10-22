using System.Collections.Generic;
using UnityEngine;

// El garfio en el aire: va recto, si toca a alguien le hace daño y lo jala de vuelta.
// Ignora la capa "MuroRio", por eso cruza el río.
[RequireComponent(typeof(LineRenderer))]
public class HookProjectile : MonoBehaviour
{
    public static readonly List<HookProjectile> Active = new List<HookProjectile>();

    [SerializeField] float hitRadius = 0.6f;
    [SerializeField] GameObject hitEffectPrefab;

    public HookAbility Ability { get; private set; }
    public Vector3 Direction { get; private set; }
    public bool IsReturning { get; private set; }

    LineRenderer line;
    int hitMask;
    float travelled;
    Unit grabbed;
    Rune grabbedRune;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = true;
        hitMask = ~LayerMask.GetMask("MuroRio", "Arbusto", "Ignore Raycast");
    }

    void OnEnable() { Active.Add(this); }

    void OnDestroy()
    {
        Active.Remove(this);
        Release();
        if (grabbedRune != null) Destroy(grabbedRune.gameObject); // El dueño murió antes de traerla
    }

    public void Launch(HookAbility ability, Vector3 direction)
    {
        Ability = ability;
        Direction = direction;
    }

    void Update()
    {
        if (Ability == null || Ability.Owner == null || !Ability.Owner.IsAlive || MatchOver())
        {
            Destroy(gameObject);
            return;
        }

        float step = Ability.speed * Time.deltaTime;

        if (!IsReturning)
        {
            CheckHit(step);
            if (!IsReturning)
            {
                transform.position += Direction * step;
                travelled += step;
                if (travelled >= Ability.range) IsReturning = true;
            }
        }
        else
        {
            Vector3 home = Ability.ChainOrigin;
            transform.position = Vector3.MoveTowards(transform.position, home, step * Ability.returnSpeedMultiplier);

            if (grabbedRune != null) grabbedRune.transform.position = transform.position;

            if (grabbed != null)
            {
                if (!grabbed.IsAlive) Release();
                else
                {
                    Vector3 p = transform.position;
                    p.y = grabbed.transform.position.y;
                    grabbed.transform.position = p;
                }
            }

            if (Vector3.Distance(transform.position, home) < 1f)
            {
                // Llegó hasta el dueño: el enemigo jalado muere al instante
                if (grabbed != null && grabbed.IsAlive && Ability.killOnPull && grabbed.IsEnemyOf(Ability.Owner))
                    grabbed.Health.TakeDamage(grabbed.Health.Current, Ability.gameObject);

                if (grabbedRune != null)
                {
                    grabbedRune.ApplyTo(Ability.Owner);
                    grabbedRune = null;
                }

                Destroy(gameObject);
                return;
            }
        }

        line.SetPosition(0, Ability.ChainOrigin);
        line.SetPosition(1, transform.position);
    }

    // Si la ronda o la partida terminó, no puede quedar ningún garfio en el aire
    static bool MatchOver()
    {
        GameManager gm = GameManager.Instance;
        return gm != null && (gm.State == GameState.FinRonda || gm.State == GameState.FinPartida);
    }

    void CheckHit(float step)
    {
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, hitRadius, Direction, step, hitMask, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            // Runa del río: se engancha y se trae
            Rune rune = hit.collider.GetComponentInParent<Rune>();
            if (rune != null)
            {
                if (rune.IsGrabbed) continue;
                IsReturning = true;
                transform.position += Direction * hit.distance;
                grabbedRune = rune;
                rune.Grab();
                AudioManager.Play(Sfx.GarfioImpacto);
                return;
            }

            Unit unit = hit.collider.GetComponentInParent<Unit>();
            if (unit != null && unit == Ability.Owner) continue; // No engancharse a uno mismo
            if (unit != null && (!unit.IsAlive || !unit.Status.IsTargetable)) continue; // En fase lo atraviesa
            HandleHit(hit, unit);
            return;
        }
    }

    void HandleHit(RaycastHit hit, Unit target)
    {
        IsReturning = true;
        transform.position += Direction * hit.distance;
        if (hitEffectPrefab != null) Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);

        if (target == null) // Chocó con una pared, roca o árbol
        {
            AudioManager.Play(Sfx.GarfioPared);
            return;
        }

        AudioManager.Play(Sfx.GarfioImpacto);
        if (Ability.Owner.isPlayer || target.isPlayer) CameraFollow.Shake(0.25f, 0.15f);

        // Al enemigo le hace daño (x2 con la runa de doble daño); al aliado solo lo jala (para salvarlo)
        if (target.IsEnemyOf(Ability.Owner))
            target.Health.TakeDamage(Ability.damage * Ability.Owner.Status.DamageMultiplier, Ability.gameObject);

        if (target.IsAlive && !target.Status.IsPulled)
        {
            grabbed = target;
            grabbed.Status.BeginPull();
        }
    }

    void Release()
    {
        if (grabbed != null)
        {
            grabbed.Status.EndPull();
            grabbed = null;
        }
    }
}
