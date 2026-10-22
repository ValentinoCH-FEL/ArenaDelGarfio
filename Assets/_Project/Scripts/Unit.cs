using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Toda unidad (jugador o bot) tiene este script: su equipo, su vida, su oro y dónde reaparece.
public enum TeamId { Azul = 0, Rojo = 1 }

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(StatusEffects))]
public class Unit : MonoBehaviour
{
    public static readonly List<Unit> All = new List<Unit>();

    public TeamId team = TeamId.Azul;
    public bool isPlayer = false;
    public string displayName = "Unidad";

    public Health Health { get; private set; }
    public StatusEffects Status { get; private set; }
    public Wallet Wallet { get; private set; }
    public Vector3 Velocity { get; private set; }
    public Vector3 SpawnPosition { get; private set; }
    public Quaternion SpawnRotation { get; private set; }

    public bool IsAlive => gameObject.activeInHierarchy && !Health.IsDead;

    Vector3 lastPosition;

    void Awake()
    {
        Health = GetComponent<Health>();
        Status = GetComponent<StatusEffects>();
        Wallet = GetComponent<Wallet>();
        SpawnPosition = transform.position;
        SpawnRotation = transform.rotation;
        lastPosition = transform.position;
        if (!All.Contains(this)) All.Add(this);
    }

    void OnDestroy() { All.Remove(this); }

    void Update()
    {
        if (Time.deltaTime > 0f)
            Velocity = (transform.position - lastPosition) / Time.deltaTime;
        lastPosition = transform.position;
    }

    public bool IsEnemyOf(Unit other) => other != null && other.team != team;

    public static Unit FindPlayer()
    {
        foreach (var u in All)
            if (u != null && u.isPlayer) return u;
        return null;
    }

    public void ResetToSpawn() => Revive(SpawnPosition, SpawnRotation);

    // Revive la unidad en una posición (el GameManager elige un punto al azar de su lado del río)
    public void Revive(Vector3 position, Quaternion rotation)
    {
        gameObject.SetActive(true);
        Status.ClearAll();

        var cc = GetComponent<CharacterController>();
        var agent = GetComponent<NavMeshAgent>();

        if (cc != null) cc.enabled = false;
        transform.SetPositionAndRotation(position, rotation);
        if (cc != null) cc.enabled = true;
        if (agent != null)
        {
            agent.enabled = true;
            agent.Warp(position);
        }

        lastPosition = position;
        Velocity = Vector3.zero;
        Health.ResetHealth();
    }
}
