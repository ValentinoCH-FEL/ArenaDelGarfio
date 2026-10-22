using System;
using UnityEngine;
using UnityEngine.InputSystem;

// "Fase" (tecla Espacio), como el Phase Shift de Puck en Dota 2:
// el personaje desaparece unos segundos y el garfio, la red y los golpes lo atraviesan.
// No se puede mover mientras está en fase; si lanza el garfio o la red, la fase se corta.
public class DashAbility : MonoBehaviour
{
    public float duration = 1.5f;
    public float cooldown = 6f;
    public bool usePlayerInput = true; // Apágalo en los bots

    public event Action OnDashed;

    public bool IsDashing => owner != null && owner.Status.IsPhased;
    public bool IsReady => Time.time >= nextReadyTime && !IsDashing;
    public float CooldownPercent => cooldown <= 0f ? 0f : Mathf.Clamp01((nextReadyTime - Time.time) / cooldown);

    float nextReadyTime;
    Unit owner;
    PlayerController player;

    void Awake()
    {
        owner = GetComponent<Unit>();
        player = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (!usePlayerInput || player == null) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            TryDash(transform.forward);
    }

    // La dirección ya no se usa (la fase no te mueve); se deja para que los bots la sigan llamando igual
    public bool TryDash(Vector3 direction = default)
    {
        if (!IsReady || owner == null) return false;
        if (!owner.Status.CanMove) return false; // No se puede usar atrapado en la red ni jalado

        owner.Status.BeginPhase(duration);
        nextReadyTime = Time.time + cooldown;
        OnDashed?.Invoke();
        AudioManager.Play(Sfx.Dash);
        return true;
    }
}
