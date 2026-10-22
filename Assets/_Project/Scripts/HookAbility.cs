using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Lanza el garfio. Lo usan el jugador (tecla Q o clic izquierdo) y los bots (TryFire).
public class HookAbility : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] HookProjectile hookPrefab;
    [SerializeField] Transform firePoint;

    [Header("Stats (la tienda los mejora)")]
    public float damage = 35f;
    public float range = 15f;
    public float speed = 16f;                  // Velocidad de ida (lenta: da tiempo a reaccionar con la Fase)
    public float returnSpeedMultiplier = 1.6f; // La vuelta es más rápida
    public float cooldown = 3f;
    public bool killOnPull = true; // El enemigo que llega jalado hasta ti muere al instante

    [Header("Control")]
    public bool usePlayerInput = true; // Apágalo en los bots

    public event Action OnFired;

    public Unit Owner { get; private set; }
    public bool IsReady => Time.time >= nextReadyTime && activeHook == null;
    public float CooldownPercent => cooldown <= 0f ? 0f : Mathf.Clamp01((nextReadyTime - Time.time) / cooldown);
    public Vector3 ChainOrigin => firePoint != null ? firePoint.position : transform.position + Vector3.up * 0.5f;

    float nextReadyTime;
    HookProjectile activeHook;
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

        bool pressed = (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
                    || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
        if (pressed) TryFire(player.AimPoint);
    }

    public bool TryFire(Vector3 targetPoint)
    {
        if (!IsReady || hookPrefab == null) return false;
        if (Owner != null && !Owner.Status.CanAct) return false;
        if (Owner != null) Owner.Status.EndPhase(); // Lanzar corta la Fase

        Vector3 dir = targetPoint - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
        dir.Normalize();
        transform.rotation = Quaternion.LookRotation(dir);

        activeHook = Instantiate(hookPrefab, ChainOrigin, Quaternion.LookRotation(dir));
        activeHook.Launch(this, dir);

        nextReadyTime = Time.time + cooldown;
        OnFired?.Invoke();
        AudioManager.Play(Sfx.GarfioLanzar);
        return true;
    }
}
