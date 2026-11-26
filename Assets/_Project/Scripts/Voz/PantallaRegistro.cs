using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Registro con DNI: nombre, número (8 dígitos) y sexo H/M. No deja continuar con datos inválidos.
public class PantallaRegistro : MonoBehaviour
{
    TMP_InputField nombre, dni;
    TMP_Text error;
    Button hombre, mujer;
    Sexo? sexo;
    Action alTerminar;

    public static void Mostrar(Transform canvas, TMP_FontAsset font, Action alTerminar)
    {
        RectTransform box = UIPixel.Ventana(canvas, "Registro", new Vector2(900f, 720f));
        var screen = box.parent.gameObject.AddComponent<PantallaRegistro>();
        screen.alTerminar = alTerminar;
        screen.Construir(box, font);
    }

    void Construir(RectTransform box, TMP_FontAsset font)
    {
        UIPixel.Texto(box, "Titulo", "REGISTRO DEL GARFIERO", 48f, new Vector2(0f, -30f), new Vector2(860f, 60f), font, UIPixel.Naranja);
        UIPixel.Texto(box, "Ayuda", "Escribe tus datos como figuran en tu DNI", 26f, new Vector2(0f, -96f), new Vector2(860f, 40f), font);

        UIPixel.Texto(box, "EtiquetaNombre", "Nombre", 28f, new Vector2(0f, -158f), new Vector2(600f, 36f), font);
        nombre = UIPixel.Campo(box, "Nombre", "Tu nombre", new Vector2(0f, -198f), new Vector2(600f, 66f), font);
        nombre.characterLimit = 40;

        UIPixel.Texto(box, "EtiquetaDni", "Número de DNI", 28f, new Vector2(0f, -286f), new Vector2(600f, 36f), font);
        dni = UIPixel.Campo(box, "Dni", "8 dígitos", new Vector2(0f, -326f), new Vector2(600f, 66f), font);
        dni.contentType = TMP_InputField.ContentType.IntegerNumber;
        dni.characterLimit = 8;

        UIPixel.Texto(box, "EtiquetaSexo", "Sexo", 28f, new Vector2(0f, -414f), new Vector2(600f, 36f), font);
        hombre = UIPixel.Boton(box, "Hombre", "H (hombre)", new Vector2(-155f, -456f), new Vector2(290f, 70f), font, () => Elegir(Sexo.Hombre), UIPixel.Gris);
        mujer = UIPixel.Boton(box, "Mujer", "M (mujer)", new Vector2(155f, -456f), new Vector2(290f, 70f), font, () => Elegir(Sexo.Mujer), UIPixel.Gris);

        error = UIPixel.Texto(box, "Error", "", 26f, new Vector2(0f, -540f), new Vector2(820f, 40f), font, new Color(1f, 0.45f, 0.4f));
        UIPixel.Boton(box, "Continuar", "Continuar", new Vector2(0f, -600f), new Vector2(360f, 80f), font, Continuar, UIPixel.Naranja);

        // Si ya había datos (al cambiarlos), se muestran
        if (PerfilJugador.EstaRegistrado)
        {
            nombre.text = PerfilJugador.Nombre;
            dni.text = PerfilJugador.Dni;
            Marcar(PerfilJugador.Sexo);
        }
    }

    void Elegir(Sexo value)
    {
        AudioManager.Play(Sfx.Click);
        Marcar(value);
    }

    void Marcar(Sexo value)
    {
        sexo = value;
        hombre.GetComponent<Image>().color = value == Sexo.Hombre ? UIPixel.Naranja : UIPixel.Gris;
        mujer.GetComponent<Image>().color = value == Sexo.Mujer ? UIPixel.Naranja : UIPixel.Gris;
    }

    void Continuar()
    {
        AudioManager.Play(Sfx.Click);
        string n = nombre.text.Trim();
        if (n.Length == 0) error.text = "Escribe tu nombre";
        else if (!PerfilJugador.EsDniValido(dni.text)) error.text = "El DNI debe tener exactamente 8 dígitos";
        else if (sexo == null) error.text = "Elige H o M, como figura en tu DNI";
        else
        {
            PerfilJugador.Nombre = n;
            PerfilJugador.Dni = dni.text;
            PerfilJugador.Sexo = sexo.Value;
            alTerminar?.Invoke();
            Destroy(gameObject);
        }
    }
}
