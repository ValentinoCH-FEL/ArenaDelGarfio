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
