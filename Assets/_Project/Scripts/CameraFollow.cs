using UnityEngine;

// La cámara sigue al jugador desde arriba y puede temblar (golpes, muertes).
public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Vector3 offset = new Vector3(0f, 14f, -10f);
    [SerializeField] float smooth = 8f;
    [SerializeField] float angleX = 55f;

    static CameraFollow instance;
    float shakeUntil;
    float shakeStrength;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        // Vista 2D: cámara ortográfica mirando derecho hacia abajo
        offset = new Vector3(0f, 30f, 0f);
        angleX = 90f;
        var cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = GameSession.Mode == GameMode.Versus ? 15.5f : 9.5f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 60f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.11f, 0.09f, 0.07f);
        }

        // Con 2 jugadores la cámara queda fija y muestra toda la arena
        if (GameSession.Mode == GameMode.Versus)
        {
            target = null;
            transform.SetPositionAndRotation(new Vector3(0f, 30f, 0f), Quaternion.Euler(90f, 0f, 0f));
            return;
        }

        if (target == null)
        {
            Unit player = Unit.FindPlayer();
            if (player != null) target = player.transform;
        }
        transform.rotation = Quaternion.Euler(angleX, 0f, 0f);
        if (target != null) transform.position = target.position + offset;
    }

    // Cualquier script puede llamar: CameraFollow.Shake(fuerza, segundos);
    public static void Shake(float strength, float duration)
    {
        if (instance == null) return;
        instance.shakeStrength = Mathf.Max(instance.shakeStrength, strength);
        instance.shakeUntil = Mathf.Max(instance.shakeUntil, Time.unscaledTime + duration);
    }

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, smooth * Time.deltaTime);

        if (Time.unscaledTime < shakeUntil && Time.timeScale > 0f)
            transform.position += Random.insideUnitSphere * shakeStrength;
        else
            shakeStrength = 0f;
    }
}
