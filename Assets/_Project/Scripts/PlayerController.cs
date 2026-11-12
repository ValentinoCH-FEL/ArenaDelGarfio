using UnityEngine;

// Mueve a un jugador. Jugador 1: WASD y mira hacia el mouse. Jugador 2: flechas (o mando) y mira hacia donde se mueve.
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public int playerIndex = 0;           // 0 = jugador 1, 1 = jugador 2
    public float moveSpeed = 6f;          // La tienda lo mejora
    [SerializeField] float gravity = -20f;

    CharacterController controller;
    StatusEffects status;
    Camera cam;
    float verticalVelocity;

    public bool CanMove { get; set; } = true;
    public Vector3 AimPoint { get; private set; }      // El garfio y la red apuntan aquí
    public Vector3 MoveDirection { get; private set; }

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        status = GetComponent<StatusEffects>();
    }

    void Start()
    {
        cam = Camera.main;
        AimPoint = transform.position + transform.forward * 12f;
    }

    void Update()
    {
        if (!controller.enabled) return; // Lo están jalando con un garfio
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

        Move();
        if (status == null || status.CanAct) // Atrapado en la red ni siquiera gira
        {
            if (playerIndex == 0) LookAtMouse();
            else LookByDirection();
        }
    }

    void Move()
    {
        bool canMove = CanMove && (status == null || status.CanMove);
        Vector2 input = canMove ? InputMap.Move(playerIndex) : Vector2.zero;

        Vector3 dir = new Vector3(input.x, 0f, input.y);
        MoveDirection = dir;

        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;

        float speed = moveSpeed * (status != null ? status.SpeedMultiplier : 1f); // Runa de velocidad
        Vector3 velocity = dir * speed + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    void LookAtMouse()
    {
        if (UnityEngine.InputSystem.Mouse.current == null || cam == null) return;

        Ray ray = cam.ScreenPointToRay(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
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

    // Jugador 2: apunta con la palanca derecha del mando o hacia donde camina
    void LookByDirection()
    {
        Vector2 stick = InputMap.AimStick(playerIndex);
        Vector3 dir = stick.magnitude > 0.3f ? new Vector3(stick.x, 0f, stick.y) : MoveDirection;

        if (dir.sqrMagnitude > 0.05f)
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), 900f * Time.deltaTime);

        AimPoint = transform.position + transform.forward * 12f;
    }
}
