using UnityEngine;

// Conecta el personaje con su Animator.
// Parámetros del Animator Controller: Speed (Float), Lanzar (Trigger), Atacar (Trigger), Atrapado (Bool).
public class CharacterAnimator : MonoBehaviour
{
    [SerializeField] Animator animator;

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int ThrowHash = Animator.StringToHash("Lanzar");
    static readonly int AttackHash = Animator.StringToHash("Atacar");
    static readonly int RootedHash = Animator.StringToHash("Atrapado");

    Unit unit;
    HookAbility hook;
    NetAbility net;
    AutoAttack melee;

    void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        unit = GetComponent<Unit>();
        hook = GetComponent<HookAbility>();
        net = GetComponent<NetAbility>();
        melee = GetComponent<AutoAttack>();
    }

    void OnEnable()
    {
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
        animator.SetFloat(SpeedHash, unit.Status.IsPulled ? 0f : v.magnitude);
        animator.SetBool(RootedHash, unit.Status.IsRooted);
    }

    void PlayThrow() { if (animator != null) animator.SetTrigger(ThrowHash); }
    void PlayAttack() { if (animator != null) animator.SetTrigger(AttackHash); }
}
