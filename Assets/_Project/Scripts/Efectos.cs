using TMPro;
using UnityEngine;

// Efectos en pixel art: polvo al caminar, salpicadura en el río, destello y número de daño
// al recibir un golpe, y chispas al reflejar un garfio con el escudo.
public static class Efectos
{
    public static void Polvo(Vector3 pos)
    {
        Vector3 v = new Vector3(Random.Range(-0.4f, 0.4f), 0f, Random.Range(0.1f, 0.6f));
        Particula.Crear(PixelSprites.Polvo, pos, v, 0.45f, 0.9f, 1.6f, PixelArt.DepthOrder(pos.z) - 1);
    }

    public static void Salpicadura(Vector3 pos)
    {
        Particula.Crear(PixelSprites.Onda, pos, Vector3.zero, 0.7f, 1f, 4f, 2000);
        for (int i = 0; i < 10; i++)
        {
            Vector2 d = Random.insideUnitCircle * 4f;
            Particula.Crear(PixelSprites.Gota, pos, new Vector3(d.x, 0f, d.y), Random.Range(0.35f, 0.6f), 1.2f, 0.4f, 2900);
        }
        AudioManager.Play(Sfx.GarfioPared);
    }

    public static void Golpe(Vector3 pos, float damage)
    {
        Particula.Crear(PixelSprites.Destello, pos, Vector3.zero, 0.18f, 1.8f, 2.8f, 3000);
        NumeroDeDano(pos + new Vector3(Random.Range(-0.3f, 0.3f), 0f, 0.9f), damage);
        AudioManager.Play(Sfx.Golpe);
    }

    public static event System.Action<Vector3> AlSacarChispas; // En línea se avisa al invitado

    public static void Chispas(Vector3 pos)
    {
        AlSacarChispas?.Invoke(pos);
        for (int i = 0; i < 14; i++)
        {
            Vector2 d = Random.insideUnitCircle.normalized * Random.Range(4f, 9f);
            Particula.Crear(PixelSprites.Chispa, pos, new Vector3(d.x, 0f, d.y), Random.Range(0.2f, 0.4f), 2f, 0.6f, 3050);
        }
    }

    static void NumeroDeDano(Vector3 pos, float damage)
    {
        var go = new GameObject("NumeroDeDano");
        var text = go.AddComponent<TextMeshPro>();
        text.text = Mathf.CeilToInt(damage).ToString();
        text.fontSize = 8f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(1f, 0.9f, 0.3f);
        text.rectTransform.sizeDelta = new Vector2(4f, 2f);
        go.GetComponent<MeshRenderer>().sortingOrder = 3100;
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(90f, 0f, 0f));
        go.AddComponent<Particula>().Iniciar(new Vector3(0f, 0f, 1.6f), 0.8f, 1f, 1f, null, text);
    }
}

// Una partícula: se mueve, cambia de tamaño, se desvanece y se borra sola.
public class Particula : MonoBehaviour
{
    Vector3 velocity;
    float life, age, startScale, endScale;
    SpriteRenderer sprite;
    TMP_Text label;

    public static void Crear(Sprite image, Vector3 pos, Vector3 velocity, float life, float startScale, float endScale, int order)
    {
        SpriteRenderer sr = PixelArt.CreateFlat(null, "Efecto", image, order);
        sr.transform.position = pos;
        sr.transform.localScale = Vector3.one * startScale;
        sr.gameObject.AddComponent<Particula>().Iniciar(velocity, life, startScale, endScale, sr, null);
    }

    public void Iniciar(Vector3 v, float seconds, float from, float to, SpriteRenderer sr, TMP_Text text)
    {
        velocity = v;
        life = seconds;
        startScale = from;
        endScale = to;
        sprite = sr;
        label = text;
    }

    void Update()
    {
        age += Time.deltaTime;
        float k = age / life;
        if (k >= 1f) { Destroy(gameObject); return; }

        transform.position += velocity * Time.deltaTime;
        velocity *= 1f - 2.5f * Time.deltaTime; // Se va frenando
        transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, k);

        float alpha = 1f - k * k;
        if (sprite != null) sprite.color = new Color(1f, 1f, 1f, alpha);
        if (label != null) label.alpha = alpha;
    }
}
