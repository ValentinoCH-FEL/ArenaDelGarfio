using UnityEngine;

// Mueve la textura del agua para que el río parezca correr.
public class RiverFlow : MonoBehaviour
{
    public Vector2 speed = new Vector2(0.25f, 0.03f);

    Material mat;
    Vector2 offset;

    void Start()
    {
        var r = GetComponent<Renderer>();
        if (r != null) mat = r.material; // Copia propia para no cambiar el material guardado
    }

    void Update()
    {
        if (mat == null) return;
        offset += speed * Time.deltaTime;
        offset.x %= 1f;
        offset.y %= 1f;
        mat.mainTextureOffset = offset;
    }
}
