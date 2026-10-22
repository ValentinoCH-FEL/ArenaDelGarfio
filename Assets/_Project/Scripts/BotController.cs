using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Inteligencia del bot con 6 niveles de dificultad (el GameManager la sube por arena, por ronda
// y cuando el jugador va ganando). Busca al enemigo más cercano que no esté escondido, se mueve de lado a lado,
// apunta calculando hacia dónde se mueve el objetivo, hace el combo red + garfio, esquiva y compra mejoras.
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Unit))]
public class BotController : MonoBehaviour
{
    //                                        Nivel:  1      2      3      4      5      6
    static readonly float[] AimByLevel      = {    2.0f,  1.5f,  1.0f,  0.6f,  0.3f,  0.15f };
    static readonly float[] ReactionByLevel = {    1.0f,  0.8f,  0.55f, 0.4f,  0.25f, 0.15f };
    static readonly float[] DodgeByLevel    = {    0.1f,  0.25f, 0.45f, 0.6f,  0.75f, 0.85f };
    static readonly float[] NetByLevel      = {    0.1f,  0.2f,  0.35f, 0.5f,  0.6f,  0.7f  };
    static readonly float[] SpeedByLevel    = {    4.5f,  4.8f,  5.0f,  5.4f,  5.8f,  6.2f  };

    [Header("Dificultad (el GameManager la cambia sola)")]
    [Range(1, 6)] public int level = 3;
    [Range(0f, 3f)] public float aimError = 1.0f;    // Más alto = falla más
    public float reactionTime = 0.55f;               // Segundos antes del primer disparo
    [Range(0f, 1f)] public float dodgeChance = 0.45f; // Probabilidad de esquivar un garfio
    [Range(0f, 1f)] public float netChance = 0.35f;   // Probabilidad de usar la red (combo)
    public float thinkInterval = 0.15f;

    [Header("Movimiento")]
    public float moveSpeed = 5f;
    public float speedBonus = 0f;                    // Lo suben las mejoras que compra el bot
    public float strafeDistance = 4f;

    NavMeshAgent agent;
    Unit unit;
    HookAbility hook;
    NetAbility net;
    DashAbility dash;

    Unit target;
    float nextThink, canShootAt, nextStrafe;
    Vector3 strafeOffset;
    readonly Dictionary<ItemData, int> boughtLevels = new Dictionary<ItemData, int>();

    bool AgentReady => agent.enabled && agent.isOnNavMesh;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        unit = GetComponent<Unit>();
        hook = GetComponent<HookAbility>();
        net = GetComponent<NetAbility>();
        dash = GetComponent<DashAbility>();

        if (hook != null) hook.usePlayerInput = false;
        if (net != null) net.usePlayerInput = false;
        if (dash != null) dash.usePlayerInput = false;

        ApplyDifficulty(level);
    }

    void OnEnable() { target = null; }

    public void ApplyDifficulty(int newLevel)
    {
        level = Mathf.Clamp(newLevel, 1, 6);
        int i = level - 1;
        aimError = AimByLevel[i];
        reactionTime = ReactionByLevel[i];
        dodgeChance = DodgeByLevel[i];
        netChance = NetByLevel[i];
        moveSpeed = SpeedByLevel[i];
    }

    void Update()
    {
        if (!AgentReady) return;

        bool playing = GameManager.Instance == null || GameManager.Instance.IsPlaying;
        agent.isStopped = !playing || !unit.Status.CanMove;
        if (!playing) return;

        agent.speed = (moveSpeed + speedBonus) * unit.Status.SpeedMultiplier;

        if (Time.time >= nextThink)
        {
            nextThink = Time.time + thinkInterval;
            Think();
        }
    }

    void Think()
    {
        TryDodge();

        Unit newTarget = FindTarget();
        if (newTarget != target)
        {
            target = newTarget;
            canShootAt = Time.time + reactionTime;
        }

        if (target == null)
        {
            Wander();
            TryHookRune();
            return;
        }

        // Moverse cerca del objetivo con un desplazamiento lateral al azar (el río va a lo largo del eje X)
        if (Time.time >= nextStrafe)
        {
            nextStrafe = Time.time + Random.Range(0.8f, 2.2f);
            strafeOffset = new Vector3(Random.Range(-strafeDistance, strafeDistance), 0f, 0f);
        }
        Vector3 goal = target.transform.position + strafeOffset;
        if (NavMesh.SamplePosition(goal, out NavMeshHit navHit, 6f, NavMesh.AllAreas))
            agent.SetDestination(navHit.position);

        bool targetRooted = target.Status.IsRooted;
        if (!targetRooted && Time.time < canShootAt) return; // Si está atrapado en la red, dispara sin esperar

        float dist = FlatDistance(target.transform.position);

        if (hook != null && hook.IsReady && dist <= hook.range * 0.9f)
        {
            // Combo: primero la red y, cuando el objetivo queda quieto, el garfio
            if (!targetRooted && net != null && net.IsReady && dist <= net.range * 0.85f && Random.value < netChance)
            {
                net.TryFire(PredictPosition(target, net.speed, aimError));
                canShootAt = Time.time + 0.15f;
                return;
            }

            Vector3 aim = targetRooted ? target.transform.position : PredictPosition(target, hook.speed, aimError);
            hook.TryFire(aim);
            canShootAt = Time.time + 0.3f;
            return;
        }

        TryHookRune(); // Si no hay a quién dispararle, intenta traer una runa
    }

    void TryHookRune()
    {
        if (hook == null || !hook.IsReady || Random.value > 0.35f) return;
        foreach (var rune in Rune.Active)
        {
            if (rune == null || rune.IsGrabbed) continue;
            if (FlatDistance(rune.transform.position) > hook.range * 0.9f) continue;
            hook.TryFire(rune.transform.position);
            return;
        }
    }

    Unit FindTarget()
    {
        Unit best = null;
        float bestDist = float.MaxValue;

        foreach (var u in Unit.All)
        {
            if (u == null || !u.IsAlive || !unit.IsEnemyOf(u) || !u.Status.IsTargetable) continue;
            float d = FlatDistance(u.transform.position);
            if (u.Status.IsHidden && d > 2.5f) continue; // No ve a los que están en arbustos
            if (u.Status.IsRooted) d *= 0.5f;            // Prefiere a los que están atrapados
            if (d < bestDist) { bestDist = d; best = u; }
        }
        return best;
    }

    // Calcula dónde va a estar el objetivo cuando llegue el proyectil (dos pasadas para afinar)
    Vector3 PredictPosition(Unit t, float projectileSpeed, float error)
    {
        Vector3 v = t.Velocity;
        v.y = 0f;
        Vector3 p = t.transform.position;
        float speed = Mathf.Max(projectileSpeed, 0.1f);

        for (int i = 0; i < 2; i++)
        {
            float time = FlatDistance(p) / speed;
            p = t.transform.position + v * time;
        }

        Vector2 err = Random.insideUnitCircle * error;
        return p + new Vector3(err.x, 0f, err.y);
    }

    void Wander()
    {
        if (agent.hasPath && agent.remainingDistance > 1f) return;
        Vector3 random = unit.SpawnPosition + new Vector3(Random.Range(-6f, 6f), 0f, Random.Range(-3f, 3f));
        if (NavMesh.SamplePosition(random, out NavMeshHit hit, 4f, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
    }

    void TryDodge()
    {
        if (dash == null || !dash.IsReady) return;

        foreach (var h in HookProjectile.Active)
        {
            if (h == null || h.IsReturning || h.Ability == null || !unit.IsEnemyOf(h.Ability.Owner)) continue;

            Vector3 toMe = transform.position - h.transform.position;
            toMe.y = 0f;
            if (toMe.magnitude > 7f) continue;
            if (Vector3.Dot(toMe.normalized, h.Direction) < 0.9f) continue; // No viene hacia mí

            if (Random.value > dodgeChance) return;

            Vector3 side = Vector3.Cross(Vector3.up, h.Direction);
            if (Random.value < 0.5f) side = -side;
            dash.TryDash(side);
            return;
        }
    }

    // El GameManager lo llama al terminar cada ronda: gasta su oro en mejoras al azar
    public void BuyUpgrades(List<ItemData> items)
    {
        if (items == null || items.Count == 0 || unit.Wallet == null) return;

        for (int tries = 0; tries < 10; tries++)
        {
            List<ItemData> affordable = items.FindAll(i =>
                i != null && i.cost <= unit.Wallet.Gold && GetLevel(i) < i.maxLevel);
            if (affordable.Count == 0) return;

            ItemData item = affordable[Random.Range(0, affordable.Count)];
            if (!unit.Wallet.TrySpend(item.cost)) return;

            item.ApplyTo(unit);
            boughtLevels[item] = GetLevel(item) + 1;
        }
    }

    int GetLevel(ItemData item) => boughtLevels.TryGetValue(item, out int lvl) ? lvl : 0;

    float FlatDistance(Vector3 p)
    {
        Vector3 d = p - transform.position;
        d.y = 0f;
        return d.magnitude;
    }
}
