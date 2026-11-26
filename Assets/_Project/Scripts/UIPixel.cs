using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Piezas de interfaz con estilo pixel (sin esquinas redondeadas y con borde), creadas desde código.
public static class UIPixel
{
    public static readonly Color Azul = new Color(0.16f, 0.32f, 0.58f);
    public static readonly Color Naranja = new Color(0.95f, 0.55f, 0.15f);
    public static readonly Color Gris = new Color(0.3f, 0.32f, 0.38f);
    public static readonly Color Fondo = new Color(0.07f, 0.08f, 0.15f, 0.97f);
    public static readonly Color Borde = new Color(0.13f, 0.1f, 0.09f);

    public static RectTransform Nodo(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    public static void Colocar(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    public static void Estirar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // Fondo que tapa toda la pantalla (y los clics) con una caja centrada
    public static RectTransform Ventana(Transform canvas, string name, Vector2 size)
    {
        RectTransform shade = Nodo(canvas, name);
        Estirar(shade);
        shade.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);

        RectTransform box = Nodo(shade, "Caja");
        Colocar(box, new Vector2(0.5f, 0.5f), Vector2.zero, size);
        box.gameObject.AddComponent<Image>().color = Fondo;
        Enmarcar(box.gameObject, Naranja);
        return box;
    }

    public static void Enmarcar(GameObject go, Color color)
    {
        Outline outline = go.GetComponent<Outline>();
        if (outline == null) outline = go.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(4f, -4f);
    }

    // Quita las esquinas redondeadas de un botón que ya existe y le pone borde pixel
    public static void Estilizar(Button b)
    {
        Image img = b.GetComponent<Image>();
        if (img == null) return;
        img.sprite = null;
        Enmarcar(b.gameObject, Borde);
    }

    // Los textos se colocan con la esquina de arriba al centro de su caja
    public static TMP_Text Texto(Transform parent, string name, string text, float size, Vector2 pos, Vector2 area,
                                 TMP_FontAsset font, Color? color = null)
    {
        var t = Nodo(parent, name).gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.alignment = TextAlignmentOptions.Center;
        t.color = color ?? Color.white;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.Normal;
        Colocar(t.rectTransform, new Vector2(0.5f, 1f), pos, area);
        return t;
    }

    public static Button Boton(Transform parent, string name, string label, Vector2 pos, Vector2 size,
                               TMP_FontAsset font, UnityAction onClick, Color? color = null)
    {
        RectTransform rt = Nodo(parent, name);
        Colocar(rt, new Vector2(0.5f, 1f), pos, size);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color ?? Azul;
        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = img;
        button.onClick.AddListener(onClick);
        Enmarcar(rt.gameObject, Borde);

        TMP_Text text = Texto(rt, "Texto", label, 32f, Vector2.zero, size, font);
        Estirar(text.rectTransform);
        return button;
    }

    public static TMP_InputField Campo(Transform parent, string name, string placeholder, Vector2 pos, Vector2 size,
                                       TMP_FontAsset font)
    {
        RectTransform rt = Nodo(parent, name);
        rt.gameObject.SetActive(false); // El InputField necesita sus partes antes de activarse
        Colocar(rt, new Vector2(0.5f, 1f), pos, size);
        var bg = rt.gameObject.AddComponent<Image>();
        bg.color = Color.white;
        var field = rt.gameObject.AddComponent<TMP_InputField>();
        Enmarcar(rt.gameObject, Borde);

        RectTransform area = Nodo(rt, "Area");
        Estirar(area);
        area.offsetMin = new Vector2(16f, 6f);
        area.offsetMax = new Vector2(-16f, -6f);
        area.gameObject.AddComponent<RectMask2D>();

        TMP_Text hint = Texto(area, "Pista", placeholder, 30f, Vector2.zero, size, font, new Color(0.4f, 0.4f, 0.45f));
        TMP_Text value = Texto(area, "Valor", "", 30f, Vector2.zero, size, font, Borde);
        foreach (TMP_Text t in new[] { hint, value })
        {
            Estirar(t.rectTransform);
            t.alignment = TextAlignmentOptions.Left;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.verticalAlignment = VerticalAlignmentOptions.Middle;
        }

        field.targetGraphic = bg;
        field.textViewport = area;
        field.textComponent = value;
        field.placeholder = hint;
        if (font != null) field.fontAsset = font;
        field.pointSize = 30f;
        rt.gameObject.SetActive(true);
        return field;
    }
}
