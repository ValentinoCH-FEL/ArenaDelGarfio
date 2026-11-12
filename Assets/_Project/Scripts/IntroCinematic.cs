using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Intro animada: cuenta con imágenes por qué se enfrentan los dos clanes.
// Se muestra una vez al abrir el juego, encima del menú. Cualquier tecla o clic la salta.
public class IntroCinematic : MonoBehaviour
{
    static bool alreadyShown;

    static readonly string[] Captions =
    {
        "Hace siglos, el río Ankari partió en dos el valle de Garfia.",
        "En su fondo duerme el Ancla del Titán: quien domine estas aguas tendrá su fuerza.",
        "Dos clanes pelearon por ella: la <color=#4FA3FF>Marea</color> y la <color=#FF5A5A>Brasa</color>.",
        "La guerra maldijo el río. Quien lo cruza a pie no vuelve. Los clanes juraron no pisar el agua jamás.",
        "Pero el pacto no dice nada de <color=#F28C26>traer</color> al enemigo. Así nació el garfio.",
        "En cada equinoccio, los mejores garfieros luchan de orilla a orilla. Quien gane custodiará el valle.\n<color=#4FA3FF>Tú peleas por la Marea.</color>",
    };

    const float SlideSeconds = 5.2f;
    const float FadeSeconds = 0.9f;

    RawImage imageA, imageB;
    TMP_Text caption, hint;
    CanvasGroup group;
    bool skip;

    // Crea la intro encima del menú (solo la primera vez que se abre el juego)
    public static void PlayOnce(Canvas canvas, TMP_Text fontSource)
    {
        if (alreadyShown || canvas == null) return;
        alreadyShown = true;

        var go = new GameObject("Intro", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsLastSibling();
        Stretch((RectTransform)go.transform);
        go.AddComponent<IntroCinematic>().Build(fontSource);
    }

    void Build(TMP_Text fontSource)
    {
        group = gameObject.AddComponent<CanvasGroup>();

        var bg = NewChild<Image>("Fondo");
        bg.color = Color.black; // También tapa los clics hacia el menú

        imageA = NewChild<RawImage>("ImagenA");
        imageB = NewChild<RawImage>("ImagenB");
        imageA.raycastTarget = imageB.raycastTarget = false;
        imageA.color = imageB.color = new Color(1, 1, 1, 0);

        var band = NewChild<Image>("Banda");
        band.color = new Color(0f, 0f, 0f, 0.55f);
        band.raycastTarget = false;
        RectTransform brt = band.rectTransform;
        brt.anchorMin = new Vector2(0f, 0f);
        brt.anchorMax = new Vector2(1f, 0f);
        brt.pivot = new Vector2(0.5f, 0f);
        brt.sizeDelta = new Vector2(0f, 260f);
        brt.anchoredPosition = Vector2.zero;

        caption = NewText("Texto", fontSource, 44, TextAlignmentOptions.Center, Color.white);
        RectTransform crt = caption.rectTransform;
        crt.anchorMin = new Vector2(0.5f, 0f);
        crt.anchorMax = new Vector2(0.5f, 0f);
        crt.pivot = new Vector2(0.5f, 0f);
        crt.sizeDelta = new Vector2(1500f, 190f);
        crt.anchoredPosition = new Vector2(0f, 50f);

        hint = NewText("Saltar", fontSource, 22, TextAlignmentOptions.TopRight, new Color(1f, 1f, 1f, 0.6f));
        hint.text = "Clic o cualquier tecla para saltar";
        RectTransform hrt = hint.rectTransform;
        hrt.anchorMin = hrt.anchorMax = hrt.pivot = new Vector2(1f, 1f);
        hrt.sizeDelta = new Vector2(600f, 40f);
        hrt.anchoredPosition = new Vector2(-30f, -24f);

        StartCoroutine(Play());
    }

    void Update()
    {
        bool key = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
        bool click = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        if (key || click) skip = true;
    }

    IEnumerator Play()
    {
        yield return null;
        skip = false; // Ignora el clic con el que se abrió el juego

        RawImage current = imageA, next = imageB;
        for (int i = 0; i < Captions.Length && !skip; i++)
        {
            Texture2D tex = Resources.Load<Texture2D>("Intro/intro_" + (i + 1));
            next.texture = tex;
            current.transform.SetSiblingIndex(1); // La nueva imagen va encima de la anterior,
            next.transform.SetSiblingIndex(2);    // pero debajo de la banda y del texto
            Vector2 drift = new Vector2(i % 2 == 0 ? 30f : -30f, 12f);

            caption.text = Captions[i];
            caption.ForceMeshUpdate();
            int total = caption.textInfo.characterCount;
            caption.maxVisibleCharacters = 0;

            float t = 0f;
            while (t < SlideSeconds && !skip)
            {
                t += Time.unscaledDeltaTime;
                float k = t / SlideSeconds;

                // Entrada con fundido, y zoom y paneo lentos durante toda la imagen
                float fade = Mathf.SmoothStep(0f, 1f, t / FadeSeconds);
                next.color = new Color(1, 1, 1, tex != null ? fade : 0f);
                current.color = new Color(1, 1, 1, current.texture != null ? 1f - fade : 0f); // La anterior se desvanece
                next.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.06f, 1.18f, k);
                next.rectTransform.anchoredPosition = drift * k;

                // El texto aparece letra por letra
                caption.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(t * 38f));
                yield return null;
            }

            RawImage tmp = current; current = next; next = tmp;
        }

        // Fundido de salida hacia el menú
        float f = 0f;
        while (f < 0.5f)
        {
            f += Time.unscaledDeltaTime;
            group.alpha = 1f - f / 0.5f;
            yield return null;
        }
        Destroy(gameObject);
    }

    T NewChild<T>(string name) where T : Graphic
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(transform, false);
        Stretch((RectTransform)go.transform);
        return go.AddComponent<T>();
    }

    TMP_Text NewText(string name, TMP_Text fontSource, float size, TextAlignmentOptions align, Color color)
    {
        var t = NewChild<TextMeshProUGUI>(name);
        if (fontSource != null) t.font = fontSource.font;
        t.fontSize = size;
        t.alignment = align;
        t.color = color;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.Normal;
        return t;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
