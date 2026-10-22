using UnityEngine;
using UnityEngine.InputSystem;

// Mueve al jugador con WASD y lo hace mirar hacia el mouse.
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 6f;          // La tienda lo mejora
    [SerializeField] float gravity = -20f;

    CharacterController controller;
    StatusEffects status;
    Camera cam;
    float verticalVelocity;

    public bool CanMove { get; set; } = true;
    public Vector3 AimPoint { get; private set; }      // El garfio y la red apuntan aquí
    public Vector3 MoveDirection { get; private set; } // El dash usa esta dirección

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        status = GetComponent<StatusEffects>();
    }

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        if (!controller.enabled) return; // Lo están jalando con un garfio
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

        Move();
        if (status == null || status.CanAct) LookAtMouse(); // Atrapado en la red ni siquiera gira
    }

    void Move()
    {
        Vector2 input = Vector2.zero;
        var kb = Keyboard.current;
        bool canMove = CanMove && (status == null || status.CanMove);

        if (kb != null && canMove)
        {
            if (kb.wKey.isPressed) input.y += 1;
            if (kb.sKey.isPressed) input.y -= 1;
            if (kb.dKey.isPressed) input.x += 1;
            if (kb.aKey.isPressed) input.x -= 1;
        }

        Vector3 dir = new Vector3(input.x, 0f, input.y).normalized;
        MoveDirection = dir;

        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;

        float speed = moveSpeed * (status != null ? status.SpeedMultiplier : 1f); // Runa de velocidad
        Vector3 velocity = dir * speed + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    void LookAtMouse()
    {
        if (Mouse.current == null || cam == null) return;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane ground = new Plane(Vector3.up, transform.position);

        if (ground.Raycast(ray, out float distance))
        {
            AimPoint = ray.GetPoint(distance);
            Vector3 lookDir = AimPoint - transform.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }
}
