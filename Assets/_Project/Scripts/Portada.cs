using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Portada en pixel art: fondo andino, logo, título, subtítulo, integrantes y un guerrero de cada bando.
// Las imágenes de Resources/Portada se generan con PixelArt/generar_portada.py.
public static class Portada
{
    const string Integrantes =
        "<size=80%><color=#F2BE3C>INTEGRANTES</color></size>\n" +
        "Joel Ángel Ocaño Ore   ·   Felipe Valentino Calderón Hurtado\n" +
        "Javier Josué Gómez   ·   Paulo Miguel Armijo Rojas";

    public static void Construir(Transform menu, TMP_FontAsset font)
    {
        // El título y la franja anteriores se reemplazan
        Ocultar(menu, "FranjaRio");
        Ocultar(menu, "Titulo");
        Ocultar(menu, "Subtitulo");

        Transform oldBg = menu.Find("Fondo");
        int order = oldBg != null ? oldBg.GetSiblingIndex() + 1 : 0;

        RawImage bg = Imagen(menu, "FondoPixel", "fondo");
        if (bg != null)
        {
            UIPixel.Estirar(bg.rectTransform);
            // Cubre toda la pantalla sin deformarse, aunque no sea 16:9
            var fit = bg.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = 16f / 9f;
            bg.transform.SetSiblingIndex(order++);
        }

        RawImage title = Imagen(menu, "TituloPixel", "titulo");
        if (title != null && title.texture != null)
        {
            Vector2 size = new Vector2(title.texture.width, title.texture.height) * 4f;
            UIPixel.Colocar(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(80f, -55f), size);
            title.transform.SetSiblingIndex(order++);

            RawImage logo = Imagen(menu, "Logo", "logo");
            if (logo != null)
            {
                UIPixel.Colocar(logo.rectTransform, new Vector2(0.5f, 1f), new Vector2(80f - size.x / 2f - 110f, -20f), new Vector2(192f, 192f));
                logo.transform.SetSiblingIndex(order++);
            }
        }

        TMP_Text sub = UIPixel.Texto(menu, "SubtituloPixel", "La guerra civil inca · Huáscar contra Atahualpa", 42f,
                                     new Vector2(0f, -215f), new Vector2(1600f, 60f), font);
        sub.fontStyle = FontStyles.Bold;
        sub.outlineWidth = 0.25f;
        sub.outlineColor = Color.black;
        sub.transform.SetSiblingIndex(order++);

        Personaje(menu, "GuerreroCusco", "equilibrado_azul", -580f, false, order++);
        Personaje(menu, "GuerreroQuito", "equilibrado_rojo", 580f, true, order);

        Transform credits = menu.Find("Integrantes");
        if (credits != null)
        {
            TMP_Text t = credits.GetComponent<TMP_Text>();
            if (t != null)
            {
                t.text = Integrantes;
                t.fontSize = 30f;
                t.enableAutoSizing = false;
                t.color = Color.white;
                t.outlineWidth = 0.25f;
                t.outlineColor = Color.black;
            }
            var rt = (RectTransform)credits;
            rt.anchoredPosition = new Vector2(0f, 24f);
            rt.sizeDelta = new Vector2(1700f, 130f);
        }

        foreach (Button b in menu.GetComponentsInChildren<Button>(true)) UIPixel.Estilizar(b);
    }

    static void Ocultar(Transform parent, string name)
    {
        Transform t = parent.Find(name);
        if (t != null) t.gameObject.SetActive(false);
    }

    static RawImage Imagen(Transform parent, string name, string file)
    {
        Texture2D tex = Resources.Load<Texture2D>("Portada/" + file);
        if (tex == null) return null;
        tex.filterMode = FilterMode.Point;
        var img = UIPixel.Nodo(parent, name).gameObject.AddComponent<RawImage>();
        img.texture = tex;
        img.raycastTarget = false;
        return img;
    }

    static void Personaje(Transform menu, string name, string sheet, float x, bool flip, int order)
    {
        Sprite[] frames = PixelArt.Frames("Personajes/" + sheet, 32);
        if (frames == null || frames.Length < 2) return;

        var img = UIPixel.Nodo(menu, name).gameObject.AddComponent<Image>();
        img.raycastTarget = false;
        UIPixel.Colocar(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x, -150f), new Vector2(320f, 320f));
        img.rectTransform.localScale = new Vector3(flip ? -1f : 1f, 1f, 1f);
        img.gameObject.AddComponent<AnimacionUI>().frames = new[] { frames[0], frames[1] };
        img.transform.SetSiblingIndex(order);
    }
}

// Cambia de cuadro un Image (animación de quieto en la portada)
public class AnimacionUI : MonoBehaviour
{
    public Sprite[] frames;
    public float framesPerSecond = 2f;
    Image image;

    void Awake() { image = GetComponent<Image>(); }

    void Update()
    {
        if (frames != null && frames.Length > 0)
            image.sprite = frames[(int)(Time.unscaledTime * framesPerSecond) % frames.Length];
    }
}
