using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Lanza una red que inmoviliza al enemigo (tecla E o clic derecho).
public class NetAbility : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] NetProjectile netPrefab;
    [SerializeField] Transform firePoint;

    [Header("Stats (la tienda los mejora)")]
    public float rootDuration = 2.5f;
    public float damage = 10f;
    public float range = 12f;
    public float speed = 18f;
    public float cooldown = 8f;

    [Header("Control")]
    public bool usePlayerInput = true; // Apágalo en los bots

    public event Action OnFired;

    public Unit Owner { get; private set; }
    public bool IsReady => Time.time >= nextReadyTime;
    public float CooldownPercent => cooldown <= 0f ? 0f : Mathf.Clamp01((nextReadyTime - Time.time) / cooldown);

    float nextReadyTime;
    PlayerController player;

    void Awake()
    {
        Owner = GetComponent<Unit>();
        player = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (!usePlayerInput || player == null) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

        bool pressed = (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                    || (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame);
        if (pressed) TryFire(player.AimPoint);
    }

    public bool TryFire(Vector3 targetPoint)
    {
        if (!IsReady || netPrefab == null) return false;
        if (Owner != null && !Owner.Status.CanAct) return false;
        if (Owner != null) Owner.Status.EndPhase(); // Lanzar corta la Fase

        Vector3 dir = targetPoint - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
        dir.Normalize();
        transform.rotation = Quaternion.LookRotation(dir);

        Vector3 start = firePoint != null ? firePoint.position : transform.position + Vector3.up * 0.5f;
        NetProjectile net = Instantiate(netPrefab, start, Quaternion.LookRotation(dir));
        net.Launch(this, dir);

        nextReadyTime = Time.time + cooldown;
        OnFired?.Invoke();
        AudioManager.Play(Sfx.Red);
        return true;
    }
}
