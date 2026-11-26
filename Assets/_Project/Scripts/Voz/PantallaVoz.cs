using TMPro;
using UnityEngine;
using UnityEngine.UI;

// "El garfiero repite": escucha lo que dice el jugador, lo escribe y lo repite
// con voz de hombre o de mujer según el sexo de su DNI.
public class PantallaVoz : MonoBehaviour
{
    TMP_Text perfil, estado, reconocido;
    IServicioVoz voz;
    TMP_FontAsset font;
    GameObject ajustes; // Botón que abre el permiso de voz de Windows
    TMP_Text hablar;
    string lastText = "";
    float echoUntil;

    public static void Mostrar(Transform canvas, TMP_FontAsset font)
    {
        RectTransform box = UIPixel.Ventana(canvas, "Voz", new Vector2(1100f, 720f));
        var screen = box.parent.gameObject.AddComponent<PantallaVoz>();
        screen.font = font;
        screen.Construir(box);
    }

    void Construir(RectTransform box)
    {
        UIPixel.Texto(box, "Titulo", "EL GARFIERO REPITE", 50f, new Vector2(0f, -30f), new Vector2(1060f, 64f), font, UIPixel.Naranja);
        perfil = UIPixel.Texto(box, "Perfil", "", 28f, new Vector2(0f, -104f), new Vector2(1000f, 40f), font);
        hablar = UIPixel.Boton(box, "Hablar", "Hablar", new Vector2(0f, -170f), new Vector2(360f, 90f), font, Alternar, UIPixel.Naranja)
            .GetComponentInChildren<TMP_Text>();
        estado = UIPixel.Texto(box, "Estado", "Pulsa Hablar: repetiré cada frase que digas", 26f, new Vector2(0f, -285f), new Vector2(1000f, 70f), font, new Color(0.8f, 0.84f, 0.92f));
        reconocido = UIPixel.Texto(box, "Texto", "", 40f, new Vector2(0f, -370f), new Vector2(1000f, 130f), font);

        TMP_Text commands = null;
        var toggle = UIPixel.Boton(box, "Comandos", "", new Vector2(0f, -515f), new Vector2(660f, 60f), font, () =>
        {
            AudioManager.Play(Sfx.Click);
            ComandosVoz.Activados = !ComandosVoz.Activados;
            commands.text = EtiquetaComandos();
        }, UIPixel.Gris);
        commands = toggle.GetComponentInChildren<TMP_Text>();
        commands.fontSize = 24f;
        commands.text = EtiquetaComandos();

        UIPixel.Boton(box, "Datos", "Cambiar mis datos", new Vector2(-200f, -610f), new Vector2(360f, 70f), font, () =>
        {
            AudioManager.Play(Sfx.Click);
            PantallaRegistro.Mostrar(transform.parent, font, ActualizarPerfil);
        });
        UIPixel.Boton(box, "Volver", "Volver", new Vector2(200f, -610f), new Vector2(360f, 70f), font, () =>
        {
            AudioManager.Play(Sfx.Click);
            Destroy(gameObject);
        });

        // El juego no puede activar un permiso de privacidad, pero sí llevar al jugador a la pantalla correcta
        ajustes = UIPixel.Boton(box, "Ajustes", "Abrir ajustes de voz", new Vector2(0f, -400f), new Vector2(460f, 72f), font,
            () => Application.OpenURL("ms-settings:privacy-speech"), UIPixel.Azul).gameObject;
        ajustes.SetActive(false);

        // Para comprobar la voz de hombre o mujer sin usar el micrófono
        UIPixel.Boton(box, "Probar", "Probar voz", new Vector2(360f, -180f), new Vector2(260f, 70f), font, () =>
        {
            string sexo = PerfilJugador.Sexo == Sexo.Mujer ? "mujer" : "hombre";
            estado.text = "Reproduciendo voz de " + sexo + "...";
            ServicioVoz.Actual.Hablar("Hola " + PerfilJugador.Nombre + ", esta es la voz de " + sexo, PerfilJugador.Sexo);
        }, UIPixel.Gris);

        CrearMedidor(box);
        ActualizarPerfil();

        voz = ServicioVoz.Actual;
        voz.AlReconocer += Reconocido;
        voz.AlOir += Oyendo;
        voz.AlFallar += Fallo;
        if (!voz.Disponible) estado.text = "Este dispositivo no puede reconocer voz";
    }

    static string EtiquetaComandos() =>
        $"Empezar la partida con control por voz (tecla V): {(ComandosVoz.Activados ? "SÍ" : "NO")}";

    void ActualizarPerfil()
    {
        string voice = PerfilJugador.Sexo == Sexo.Mujer ? "de mujer" : "de hombre";
        perfil.text = $"{PerfilJugador.Nombre}  ·  Sexo según DNI: <b>{PerfilJugador.SexoComoLetra}</b>  ·  Repetiré con voz {voice}";
    }

    // Un botón para empezar y para detener la escucha continua
    void Alternar()
    {
        AudioManager.Play(Sfx.Click);
        if (hablar.text == "Hablar") Escuchar();
        else Detener("Escucha detenida");
    }

    void Detener(string message)
    {
        voz.DejarDeEscuchar();
        ApagarMedidor();
        hablar.text = "Hablar";
        estado.text = message;
    }

    void Escuchar()
    {
        hablar.text = "Detener";
        estado.text = "Escuchando... habla ahora";
        reconocido.text = "";
        ajustes.SetActive(false);
        voz.EmpezarAEscuchar();
        EncenderMedidor();
        if (genero != null) genero.text = "";

        // Sin micrófono real (versión de prueba) se simula una frase para ver el flujo
        if (voz is ServicioVozSimulado fake) fake.Simular("hola, soy un garfiero");
    }

    void Oyendo(string text)
    {
        estado.text = "Te estoy oyendo...";
        reconocido.text = $"<color=#9AA6BD>{text}</color>";
    }

    // Se sigue escuchando: cada frase se escribe y se repite
    void Reconocido(string text)
    {
        // Si el micrófono capta la propia voz del juego, no se repite en bucle
        if (Time.unscaledTime < echoUntil && Clean(text) == Clean(lastText)) return;
        lastText = text;
        echoUntil = Time.unscaledTime + 3f + text.Length * 0.09f;

        estado.text = "Dijiste (sigue hablando o pulsa Detener):";
        reconocido.text = $"\"{text}\"";
        MostrarGenero();
        voz.Hablar(text, PerfilJugador.Sexo);
    }

    static string Clean(string text) => text.Trim().TrimEnd('.', '!', '?').ToLowerInvariant();

    void Fallo(string message)
    {
        Detener(message);
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (message == VozWindows.AvisoPermiso) ajustes.SetActive(true);
#endif
    }

    // ---- Medidor del micrófono: muestra si de verdad está llegando sonido ----
    RectTransform barra;
    TMP_Text microfono;
    TMP_Text genero;   // Resultado del detector de género por voz

    void CrearMedidor(RectTransform box)
    {
        RectTransform fondo = UIPixel.Nodo(box, "Medidor");
        UIPixel.Colocar(fondo, new Vector2(0.5f, 1f), new Vector2(0f, -340f), new Vector2(600f, 16f));
        fondo.gameObject.AddComponent<Image>().color = UIPixel.Gris;

        barra = UIPixel.Nodo(fondo, "Nivel");
        barra.anchorMin = Vector2.zero;
        barra.anchorMax = new Vector2(0f, 1f);
        barra.offsetMin = barra.offsetMax = Vector2.zero;
        barra.gameObject.AddComponent<Image>().color = new Color(0.45f, 0.9f, 0.5f);

        microfono = UIPixel.Texto(box, "Microfono", "", 20f, new Vector2(0f, -485f), new Vector2(1000f, 28f), font, new Color(0.65f, 0.7f, 0.8f));
        genero = UIPixel.Texto(box, "Genero", "", 24f, new Vector2(0f, -445f), new Vector2(1060f, 36f), font, new Color(1f, 0.86f, 0.35f));
        fondo.gameObject.SetActive(false);
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    // En el navegador el tono se mide en JavaScript (Plugins/WebGL/Voz.jslib), con el mismo algoritmo
    [System.Runtime.InteropServices.DllImport("__Internal")] static extern void TonoIniciar();
    [System.Runtime.InteropServices.DllImport("__Internal")] static extern void TonoParar();
    [System.Runtime.InteropServices.DllImport("__Internal")] static extern void TonoPausar(int pausa);
    [System.Runtime.InteropServices.DllImport("__Internal")] static extern int TonoMuestras();
    [System.Runtime.InteropServices.DllImport("__Internal")] static extern float TonoMediana();
    [System.Runtime.InteropServices.DllImport("__Internal")] static extern void TonoReiniciar();

    void EncenderMedidor() { TonoIniciar(); }
    void ApagarMedidor() { TonoParar(); }
    void Update() { TonoPausar(Time.unscaledTime < echoUntil ? 1 : 0); }

    void MostrarGenero()
    {
        EscribirGenero(TonoMuestras(), TonoMediana());
        TonoReiniciar();
    }
#else
    AudioClip muestra;
    readonly float[] datos = new float[512];

    // ---- Detector de género por voz (ver DetectorGenero.cs) ----
    readonly DetectorGenero detector = new DetectorGenero(16000);
    readonly float[] ventana = new float[1024];   // 64 ms de audio
    float proximoAnalisis;

    // Al terminar cada frase: qué sexo parece por la voz y si coincide con el DNI
    void MostrarGenero()
    {
        EscribirGenero(detector.Muestras, detector.TonoMedio());
        detector.Reiniciar();
    }

    void EncenderMedidor()
    {
        ApagarMedidor();
        if (Microphone.devices.Length == 0)
        {
            microfono.text = "No se encontró ningún micrófono";
            return;
        }
        muestra = Microphone.Start(null, true, 1, 16000);
        microfono.text = "Micrófonos: " + string.Join(", ", Microphone.devices);
        barra.parent.gameObject.SetActive(true);
    }

    void ApagarMedidor()
    {
        if (muestra != null) Microphone.End(null);
        muestra = null;
        if (barra != null) barra.parent.gameObject.SetActive(false);
    }

    void Update()
    {
        if (muestra == null) return;
        int position = Microphone.GetPosition(null) - datos.Length;
        if (position < 0) return;

        muestra.GetData(datos, position);
        float peak = 0f;
        foreach (float d in datos) peak = Mathf.Max(peak, Mathf.Abs(d));
        barra.anchorMax = new Vector2(Mathf.Clamp01(peak * 4f), 1f);

        // Mide el tono cada 50 ms, pero no mientras el juego está hablando (no se mide su propia voz)
        int inicio = Microphone.GetPosition(null) - ventana.Length;
        if (inicio >= 0 && Time.unscaledTime >= proximoAnalisis && Time.unscaledTime > echoUntil)
        {
            muestra.GetData(ventana, inicio);
            detector.Analizar(ventana);
            proximoAnalisis = Time.unscaledTime + 0.05f;
        }
    }
#endif

    // Resultado del detector (igual en Windows y en la web)
    void EscribirGenero(int muestras, float tono)
    {
        if (genero == null) return;
        if (muestras < 6)
        {
            genero.text = "Detector de voz: no alcancé a medir tu tono (habla un poco más fuerte o más largo)";
            return;
        }
        Sexo? estimado = DetectorGenero.Clasificar(tono);
        if (estimado == null)
            genero.text = $"Detector de voz: tu tono (≈ {tono:0} Hz) está en la zona intermedia, no puedo asegurar  ·  se usa el DNI: {PerfilJugador.SexoComoLetra}";
        else
        {
            bool coincide = estimado == PerfilJugador.Sexo;
            string sexo = estimado == Sexo.Hombre ? "hombre" : "mujer";
            genero.text = $"Por tu voz pareces {sexo} (tono ≈ {tono:0} Hz)  ·  DNI: {PerfilJugador.SexoComoLetra}  ·  " +
                          (coincide ? "<color=#7CFF8A>coincide</color>" : "<color=#FF8A8A>no coincide, se usa el DNI</color>");
        }
    }

    void OnDestroy()
    {
        if (voz == null) return;
        voz.DejarDeEscuchar();
        voz.AlReconocer -= Reconocido;
        voz.AlOir -= Oyendo;
        ApagarMedidor();
        voz.AlFallar -= Fallo;
    }
}
