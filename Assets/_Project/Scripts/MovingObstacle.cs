using UnityEngine;

// Bloque que se desliza de un lado a otro (Arena 3). Bloquea el paso y también los garfios.
public class MovingObstacle : MonoBehaviour
{
    public Vector3 direction = Vector3.right;
    public float distance = 7f;   // Cuánto se aleja del centro hacia cada lado
    public float speed = 3f;
    public float phase = 0f;      // Para que no todos se muevan igual

    Vector3 origin;
    Rigidbody body;

    void Awake()
    {
        origin = transform.position;
        body = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        float t = Mathf.Sin((Time.time + phase) * speed / Mathf.Max(distance, 0.1f));
        Vector3 target = origin + direction.normalized * (t * distance);
        if (body != null) body.MovePosition(target);
        else transform.position = target;
    }
}
