using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Control del juego por voz. En la partida se activa y desactiva con la tecla V.
// En Windows usa KeywordRecognizer (lista fija de palabras: rápido y local);
// en el navegador usa el reconocimiento normal y busca las palabras dentro de la frase.
public class ComandosVoz : MonoBehaviour
{
    const string Key = "voz_comandos";

    // true = la partida empieza con el control por voz ya encendido
    public static bool Activados
    {
        get => PlayerPrefs.GetInt(Key, 0) == 1;
        set { PlayerPrefs.SetInt(Key, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    const string Ayuda =
        "<b>Moverte:</b> arriba · abajo · izquierda · derecha · arriba izquierda... · alto\n" +
        "<b>Atacar:</b> garfio · red (al más cercano) · garfio arriba · red abajo derecha... · runa\n" +
        "<b>Defender:</b> escudo · fase\n" +
        "<b>Tienda:</b> tienda · comprar uno (hasta ocho) · cerrar\n" +
        "<b>Juego:</b> pausa · continuar · reintentar · siguiente · menú";
    const string AyudaMemoria = "\n<b>Memoria:</b> celeste · rosa · amarilla · café · morada · naranja";

    static readonly Dictionary<string, Vector2> Directions = new Dictionary<string, Vector2>
    {
        { "arriba", new Vector2(0, 1) }, { "abajo", new Vector2(0, -1) },
        { "izquierda", new Vector2(-1, 0) }, { "derecha", new Vector2(1, 0) },
        { "arriba izquierda", new Vector2(-1, 1) }, { "arriba derecha", new Vector2(1, 1) },
        { "abajo izquierda", new Vector2(-1, -1) }, { "abajo derecha", new Vector2(1, -1) },
    };

    static readonly string[] Numbers = { "uno", "dos", "tres", "cuatro", "cinco", "seis", "siete", "ocho" };
    static readonly string[] RuneWords = { "celeste", "rosa", "amarilla", "café", "morada", "naranja" };

    Dictionary<string, Action> commands;
    bool listening;
    TMP_Text title, help;
    GameObject helpBox;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    UnityEngine.Windows.Speech.KeywordRecognizer recognizer;
#else
    IServicioVoz voz;
    float retryAt;
#endif

    void Start()
    {
        BuildCommands();
        BuildOverlay();
        if (Activados) Encender();
        else Show("Pulsa <b>V</b> para controlar el juego con la voz", false);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
        {
            if (listening) Apagar();
            else Encender();
        }

#if !(UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)
        // El navegador deja de escuchar tras un silencio: se vuelve a encender
        if (listening && voz != null && !voz.Escuchando && Time.unscaledTime >= retryAt)
        {
            retryAt = Time.unscaledTime + 1.5f;
            voz.EmpezarAEscuchar();
        }
#endif
    }

    void OnDestroy() { Apagar(); }

    // ---------------------------------------------------------------------------------------------
    // Escuchar
    // ---------------------------------------------------------------------------------------------

    void Encender()
    {
        try
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            ServicioVoz.Actual.DejarDeEscuchar(); // El dictado y las palabras clave no pueden ir juntos
            recognizer = new UnityEngine.Windows.Speech.KeywordRecognizer(commands.Keys.ToArray(),
                                                                          UnityEngine.Windows.Speech.ConfidenceLevel.Low);
            recognizer.OnPhraseRecognized += args => Obedecer(args.text);
            recognizer.Start();
#else
            voz = ServicioVoz.Actual;
            if (!voz.Disponible) { Show("Este navegador no reconoce voz. Usa Chrome o Edge.", false); return; }
            voz.AlReconocer += ObedecerFrase;
#endif
        }
        catch (Exception e)
        {
            Debug.LogWarning("[VOZ] No se pudo encender el control por voz: " + e.Message);
            Show("No se pudo encender la voz. Revisa el micrófono y el idioma de voz de Windows.", false);
            return;
        }
        listening = true;
        Debug.Log("[VOZ] control por voz encendido");
        Show("<color=#7CFC8A>VOZ ENCENDIDA</color>  (V para apagar)", true);
    }

    void Apagar()
    {
        VozControl.Reiniciar();
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (recognizer != null)
        {
            if (recognizer.IsRunning) recognizer.Stop();
            recognizer.Dispose();
            recognizer = null;
            UnityEngine.Windows.Speech.PhraseRecognitionSystem.Shutdown(); // Deja libre el dictado del menú
        }
#else
        if (listening && voz != null)
        {
            voz.AlReconocer -= ObedecerFrase;
            voz.DejarDeEscuchar();
        }
#endif
        if (!listening) return;
        listening = false;
        if (title != null) Show("Pulsa <b>V</b> para controlar el juego con la voz", false);
    }

#if !(UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)
    // De la frase se toma la orden más larga que aparezca (por ejemplo "arriba izquierda" antes que "arriba")
    void ObedecerFrase(string phrase)
    {
        string text = " " + phrase.ToLowerInvariant().Replace(",", " ").Replace(".", " ") + " ";
        string match = commands.Keys.Where(k => text.Contains(" " + k + " ")).OrderByDescending(k => k.Length).FirstOrDefault();
        if (match != null) Obedecer(match);
    }
#endif

    public void Obedecer(string word)
    {
        if (!commands.TryGetValue(word, out Action action)) return;
        Debug.Log("[VOZ] orden: " + word);
        Show($"<color=#7CFC8A>VOZ ENCENDIDA</color>  Oí: <b>{word}</b>", true);
        action();
    }

    // ---------------------------------------------------------------------------------------------
    // Órdenes
    // ---------------------------------------------------------------------------------------------

    void BuildCommands()
    {
        commands = new Dictionary<string, Action>
        {
            { "arriba", () => VozControl.Mover(0, 1) },
            { "abajo", () => VozControl.Mover(0, -1) },
            { "izquierda", () => VozControl.Mover(-1, 0) },
            { "derecha", () => VozControl.Mover(1, 0) },
            { "arriba izquierda", () => VozControl.Mover(-1, 1) },
            { "arriba derecha", () => VozControl.Mover(1, 1) },
            { "abajo izquierda", () => VozControl.Mover(-1, -1) },
            { "abajo derecha", () => VozControl.Mover(1, -1) },
            { "alto", () => VozControl.Mover(0, 0) },
            { "para", () => VozControl.Mover(0, 0) },

            { "garfio", () => { Apuntar(Enemy(), HookSpeed()); VozControl.Pulsar(VozControl.Boton.Garfio); } },
            { "red", () => { Apuntar(Enemy(), NetSpeed()); VozControl.Pulsar(VozControl.Boton.Red); } },
            { "runa", () => { if (HookRune(Rune.Active.FirstOrDefault(r => r != null && !r.IsGrabbed))) VozControl.Pulsar(VozControl.Boton.Garfio); } },
            { "escudo", () => VozControl.Pulsar(VozControl.Boton.Escudo) },
            { "fase", () => VozControl.Pulsar(VozControl.Boton.Fase) },

            { "tienda", () => { if (!PartidaEnLinea.Activa) FindAnyObjectByType<ShopUI>()?.Open(); } },
            { "cerrar", () => FindAnyObjectByType<ShopUI>()?.Close() },
            { "pausa", () => { if (!PartidaEnLinea.Activa) FindAnyObjectByType<PauseMenu>()?.Pause(); } },
            { "continuar", () => { var p = FindAnyObjectByType<PauseMenu>(); if (p != null && p.IsOpen) p.Resume(); } },
            { "reintentar", () => { if (GameOver) FindAnyObjectByType<GameOverUI>()?.Retry(); } },
            { "siguiente", () => { if (GameOver && Niveles.Siguiente != null && GameObject.Find("Siguiente") != null) SceneLoader.Load(Niveles.Siguiente); } },
            { "menú", () => { var gm = GameManager.Instance; if (gm != null && (gm.IsPaused || GameOver)) SceneLoader.Load("MainMenu"); } },
        };

        // "garfio arriba", "red abajo izquierda"...: se lanza hacia esa dirección en vez de al enemigo
        foreach (var pair in Directions)
        {
            Vector2 direction = pair.Value;
            commands["garfio " + pair.Key] = () => { ApuntarHacia(direction); VozControl.Pulsar(VozControl.Boton.Garfio); };
            commands["red " + pair.Key] = () => { ApuntarHacia(direction); VozControl.Pulsar(VozControl.Boton.Red); };
        }

        for (int i = 0; i < Numbers.Length; i++)
        {
            int index = i;
            commands["comprar " + Numbers[i]] = () => { if (ShopUI.IsOpen) FindAnyObjectByType<ShopUI>()?.Comprar(index); };
        }

        for (int i = 0; i < RuneWords.Length; i++)
        {
            int index = i;
            commands[RuneWords[i]] = () =>
            {
                if (MemoryMode.Instance != null && HookRune(MemoryMode.Instance.RunaDeColor(index)))
                    VozControl.Pulsar(VozControl.Boton.Garfio);
            };
        }
    }

    static bool GameOver => GameManager.Instance != null && GameManager.Instance.State == GameState.FinPartida;

    static float HookSpeed() => Unit.FindPlayer()?.GetComponent<HookAbility>()?.speed ?? 16f;
    static float NetSpeed() => Unit.FindPlayer()?.GetComponent<NetAbility>()?.speed ?? 18f;

    // Sin mouse: se apunta solo al enemigo vivo más cercano
    static Unit Enemy()
    {
        Unit player = Unit.FindPlayer();
        if (player == null) return null;
        return Unit.All
            .Where(u => u != null && u.IsAlive && player.IsEnemyOf(u) && u.Status.IsTargetable && !u.Status.IsHidden)
            .OrderBy(u => (u.transform.position - player.transform.position).sqrMagnitude)
            .FirstOrDefault();
    }

    // Apunta un poco por delante si el objetivo se está moviendo
    static void Apuntar(Unit target, float projectileSpeed)
    {
        Unit player = Unit.FindPlayer();
        if (target == null || player == null) return;
        float time = Vector3.Distance(player.transform.position, target.transform.position) / Mathf.Max(1f, projectileSpeed);
        Vector3 velocity = target.Velocity;
        velocity.y = 0f;
        VozControl.Apuntar(target.transform.position + velocity * time);
    }

    static void ApuntarHacia(Vector2 direction)
    {
        Unit player = Unit.FindPlayer();
        if (player == null) return;
        Vector2 d = direction.normalized * 12f;
        VozControl.Apuntar(player.transform.position + new Vector3(d.x, 0f, d.y));
    }

    static bool HookRune(Rune rune)
    {
        if (rune == null) return false;
        VozControl.Apuntar(rune.transform.position);
        return true;
    }

    // ---------------------------------------------------------------------------------------------
    // Aviso en pantalla
    // ---------------------------------------------------------------------------------------------

    void BuildOverlay()
    {
        var canvas = new GameObject("VozEnPantalla").AddComponent<Canvas>();
        canvas.transform.SetParent(transform, false);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        RectTransform box = UIPixel.Nodo(canvas.transform, "Ordenes");
        UIPixel.Colocar(box, new Vector2(0f, 0f), new Vector2(20f, 190f), new Vector2(780f, 175f));
        var background = box.gameObject.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.6f);
        background.raycastTarget = false;
        helpBox = box.gameObject;

        help = UIPixel.Texto(box, "Texto", Ayuda, 19f, Vector2.zero, Vector2.zero, null);
        UIPixel.Estirar(help.rectTransform);
        help.margin = new Vector4(14f, 8f, 14f, 8f);
        help.alignment = TextAlignmentOptions.TopLeft;
        if (GameSession.Mode == GameMode.Memoria)
        {
            help.text += AyudaMemoria;
            box.sizeDelta = new Vector2(780f, 200f);
        }

        title = UIPixel.Texto(canvas.transform, "Estado", "", 24f, Vector2.zero, new Vector2(900f, 34f), null);
        UIPixel.Colocar(title.rectTransform, new Vector2(0f, 0f), new Vector2(20f, 150f), new Vector2(900f, 34f));
        title.alignment = TextAlignmentOptions.Left;
        title.outlineWidth = 0.25f;
        title.outlineColor = Color.black;
    }

    void Show(string message, bool withHelp)
    {
        if (title != null) title.text = message;
        if (helpBox != null) helpBox.SetActive(withHelp);
    }
}

// Los controles "virtuales" que maneja la voz. InputMap y PlayerController los leen igual que el teclado.
public static class VozControl
{
    public enum Boton { Garfio, Red, Fase, Escudo }

    static readonly int[] pressedFrame = { -1, -1, -1, -1 };
    static Vector3 aim;
    static float aimUntil;

    public static Vector2 Direccion { get; private set; }
    public static Vector3? Mira => Time.unscaledTime < aimUntil ? aim : (Vector3?)null;

    public static void Mover(float x, float y) => Direccion = new Vector2(x, y).normalized;

    public static void Apuntar(Vector3 point)
    {
        aim = point;
        aimUntil = Time.unscaledTime + 0.4f;
    }

    // Se pulsa dos cuadros después, cuando el personaje ya está mirando al objetivo
    public static void Pulsar(Boton button) => pressedFrame[(int)button] = Time.frameCount + 2;

    public static bool Pulsado(Boton button) => pressedFrame[(int)button] == Time.frameCount;

    public static void Reiniciar()
    {
        Direccion = Vector2.zero;
        aimUntil = 0f;
        for (int i = 0; i < pressedFrame.Length; i++) pressedFrame[i] = -1;
    }
}
