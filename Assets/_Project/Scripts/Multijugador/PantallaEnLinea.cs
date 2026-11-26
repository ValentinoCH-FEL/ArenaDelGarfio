using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Menú "En línea": crear una sala (muestra el código) o unirse escribiendo el código.
public class PantallaEnLinea : MonoBehaviour
{
    TMP_Text estado, codigo;
    TMP_InputField campo;
    Button crear, unirse;
    GameObject niveles;
    IServicioRed red;

    public static void Mostrar(Transform canvas, TMP_FontAsset font)
    {
        PartidaEnLinea.Suscribir();
        RectTransform box = UIPixel.Ventana(canvas, "EnLinea", new Vector2(1100f, 760f));
        box.parent.gameObject.AddComponent<PantallaEnLinea>().Construir(box, font);
    }

    void Construir(RectTransform box, TMP_FontAsset font)
    {
        UIPixel.Texto(box, "Titulo", "PARTIDA EN LÍNEA", 50f, new Vector2(0f, -30f), new Vector2(1060f, 64f), font, UIPixel.Naranja);
        UIPixel.Texto(box, "Ayuda", "El que crea la sala pelea por el Cusco; el que se une, por Quito.", 26f,
                      new Vector2(0f, -100f), new Vector2(1000f, 40f), font);

        crear = UIPixel.Boton(box, "Crear", "Crear sala", new Vector2(0f, -160f), new Vector2(460f, 80f), font, () =>
        {
            AudioManager.Play(Sfx.Click);
            Esperar("Creando la sala...");
            red.CrearSala();
        }, UIPixel.Naranja);
        codigo = UIPixel.Texto(box, "Codigo", "", 70f, new Vector2(0f, -252f), new Vector2(1000f, 90f), font, new Color(1f, 0.86f, 0.35f));

        campo = UIPixel.Campo(box, "Campo", "Código de la sala", new Vector2(-130f, -370f), new Vector2(460f, 72f), font);
        campo.characterLimit = 12;
        unirse = UIPixel.Boton(box, "Unirse", "Unirse", new Vector2(240f, -370f), new Vector2(240f, 72f), font, () =>
        {
            AudioManager.Play(Sfx.Click);
            Esperar("Entrando a la sala...");
            red.UnirseASala(campo.text);
        });

        estado = UIPixel.Texto(box, "Estado", "", 28f, new Vector2(0f, -465f), new Vector2(1040f, 90f), font);

        // Solo el anfitrión elige el nivel, cuando ya entró el otro jugador
        RectTransform row = UIPixel.Nodo(box, "Niveles");
        UIPixel.Estirar(row);
        for (int i = 0; i < Niveles.Escenas.Length; i++)
        {
            string scene = Niveles.Escenas[i];
            UIPixel.Boton(row, scene, "Jugar " + Niveles.Nombres[i], new Vector2((i - 1) * 340f, -570f), new Vector2(320f, 70f), font, () =>
            {
                AudioManager.Play(Sfx.Click);
                PartidaEnLinea.Empezar(scene);
            }, UIPixel.Naranja);
        }
        niveles = row.gameObject;
        niveles.SetActive(false);

        UIPixel.Boton(box, "Volver", "Volver", new Vector2(0f, -665f), new Vector2(300f, 66f), font, () =>
        {
            AudioManager.Play(Sfx.Click);
            red.Salir();
            Destroy(gameObject);
        });

        red = ServicioRed.Actual;
        red.AlCrearSala += SalaCreada;
        red.AlConectarseOtroJugador += Conectado;
        red.AlDesconectarse += Desconectado;
        red.AlFallar += Fallo;
    }

    void OnDestroy()
    {
        if (red == null) return;
        red.AlCrearSala -= SalaCreada;
        red.AlConectarseOtroJugador -= Conectado;
        red.AlDesconectarse -= Desconectado;
        red.AlFallar -= Fallo;
    }

    void Esperar(string message)
    {
        estado.text = message;
        codigo.text = "";
        crear.interactable = unirse.interactable = false;
    }

    void SalaCreada(string code)
    {
        codigo.text = code;
        estado.text = "Comparte este código. Esperando al otro jugador...";
    }

    void Conectado()
    {
        if (red.SoyAnfitrion)
        {
            estado.text = "¡Se unió el otro jugador! Elige el nivel:";
            niveles.SetActive(true);
        }
        else estado.text = "Conectado. Esperando a que el anfitrión elija el nivel...";
    }

    void Desconectado()
    {
        niveles.SetActive(false);
        crear.interactable = unirse.interactable = true;
        codigo.text = "";
        estado.text = "Se cerró la conexión";
    }

    void Fallo(string message)
    {
        niveles.SetActive(false);
        crear.interactable = unirse.interactable = true;
        estado.text = message;
    }
}
