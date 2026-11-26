using UnityEngine;

// Conecta el personaje con su Animator.
// Parámetros del Animator Controller: Speed (Float), RunSpeed (Float), Lanzar (Trigger), Atacar (Trigger), Atrapado (Bool).
public class CharacterAnimator : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] float referenceRunSpeed = 6f; // Velocidad a la que la animación de correr se ve natural

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int RunSpeedHash = Animator.StringToHash("RunSpeed");
    static readonly int ThrowHash = Animator.StringToHash("Lanzar");
    static readonly int AttackHash = Animator.StringToHash("Atacar");
    static readonly int RootedHash = Animator.StringToHash("Atrapado");

    Unit unit;
    HookAbility hook;
    NetAbility net;
    AutoAttack melee;
    float smoothSpeed;
    float smoothRate = 1f;
    bool hasRunSpeed;

    void Awake()
    {
        unit = GetComponent<Unit>();
        hook = GetComponent<HookAbility>();
        net = GetComponent<NetAbility>();
        melee = GetComponent<AutoAttack>();
        if (animator == null) Rebind();
        else CheckParameters();
    }

    // Cuando se cambia el modelo del personaje
    public void Rebind()
    {
        animator = null;
        foreach (var a in GetComponentsInChildren<Animator>(false)) { animator = a; break; }
        CheckParameters();
    }

    void CheckParameters()
    {
        hasRunSpeed = false;
        if (animator == null || animator.runtimeAnimatorController == null) return;
        foreach (var p in animator.parameters)
            if (p.nameHash == RunSpeedHash) hasRunSpeed = true;
    }

    void OnEnable()
    {
        smoothSpeed = 0f;
        smoothRate = 1f;
        if (animator != null)
        {
            // Al reaparecer no debe quedar ninguna animación pendiente
            animator.ResetTrigger(ThrowHash);
            animator.ResetTrigger(AttackHash);
        }
        if (hook != null) hook.OnFired += PlayThrow;
        if (net != null) net.OnFired += PlayThrow;
        if (melee != null) melee.OnAttack += PlayAttack;
    }

    void OnDisable()
    {
        if (hook != null) hook.OnFired -= PlayThrow;
        if (net != null) net.OnFired -= PlayThrow;
        if (melee != null) melee.OnAttack -= PlayAttack;
    }

    void Update()
    {
        if (animator == null || unit == null) return;

        Vector3 v = unit.Velocity;
        v.y = 0f;
        bool frozen = unit.Status.IsPulled || !unit.Status.CanMove; // Jalado, atrapado o en fase: no corre
        float speed = frozen ? 0f : Mathf.Min(v.magnitude, 12f);

        // Suavizado: evita que la animación tiemble entre quieto y correr
        float blend = 1f - Mathf.Exp(-14f * Time.deltaTime);
        smoothSpeed = Mathf.Lerp(smoothSpeed, speed, blend);
        animator.SetFloat(SpeedHash, smoothSpeed);
        animator.SetBool(RootedHash, unit.Status.IsRooted);

        if (hasRunSpeed)
        {
            // La animación va al ritmo de la velocidad real (los pies no patinan)
            // y se reproduce al revés cuando el personaje camina hacia atrás.
            float rate = Mathf.Clamp(smoothSpeed / referenceRunSpeed, 0.75f, 1.5f);
            if (speed > 0.5f && Vector3.Dot(v.normalized, transform.forward) < -0.35f) rate = -rate;
            smoothRate = Mathf.Lerp(smoothRate, rate, blend);
            animator.SetFloat(RunSpeedHash, smoothRate);
        }
    }

    void PlayThrow() { if (animator != null) animator.SetTrigger(ThrowHash); }
    void PlayAttack() { if (animator != null) animator.SetTrigger(AttackHash); }
}
