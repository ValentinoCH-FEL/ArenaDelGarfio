#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Prueba automática de las tareas, solo en el editor: menú "Arena del Garfio > Probar todo".
// Recorre el juego solo, guarda capturas e informe en Logs/prueba y al final devuelve los datos guardados.
public class PruebaCompleta : MonoBehaviour
{
    public const string Flag = "prueba_completa";
    const string Folder = "Logs/prueba";

    static readonly string[] IntKeys =
    {
        "perfil_sexo", "nivel_desbloqueado", GameSession.ModeKey, GameSession.CharacterKey, GameManager.DifficultyKey, "voz_comandos",
    };
    static readonly string[] StringKeys = { "perfil_nombre", "perfil_dni" };

    static readonly Dictionary<string, int?> savedInts = new Dictionary<string, int?>();
    static readonly Dictionary<string, string> savedStrings = new Dictionary<string, string>();

    readonly List<string> report = new List<string>();
    readonly HashSet<string> errors = new HashSet<string>();
    int failures;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        if (!SessionState.GetBool(Flag, false)) return;
        SessionState.SetBool(Flag, false);

        // Se guardan los datos del jugador y se empieza como la primera vez
        foreach (string k in IntKeys) savedInts[k] = PlayerPrefs.HasKey(k) ? PlayerPrefs.GetInt(k) : (int?)null;
        foreach (string k in StringKeys) savedStrings[k] = PlayerPrefs.HasKey(k) ? PlayerPrefs.GetString(k) : null;
        foreach (string k in IntKeys.Concat(StringKeys)) PlayerPrefs.DeleteKey(k);

        var go = new GameObject("PruebaCompleta");
        DontDestroyOnLoad(go);
        go.AddComponent<PruebaCompleta>();
    }

    void OnEnable() { Application.logMessageReceived += Log; }

    void OnDisable()
    {
        Application.logMessageReceived -= Log;

        foreach (var pair in savedInts)
            if (pair.Value.HasValue) PlayerPrefs.SetInt(pair.Key, pair.Value.Value);
            else PlayerPrefs.DeleteKey(pair.Key);
        foreach (var pair in savedStrings)
            if (pair.Value != null) PlayerPrefs.SetString(pair.Key, pair.Value);
            else PlayerPrefs.DeleteKey(pair.Key);
        PlayerPrefs.Save();
        Time.timeScale = 1f;
    }

    void Log(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            errors.Add(message + "\n      " + stack.Split('\n').FirstOrDefault());
    }

    IEnumerator Start()
    {
        Application.runInBackground = true;
        if (Directory.Exists(Folder)) Directory.Delete(Folder, true);
        Directory.CreateDirectory(Folder);

        yield return Run(Menu(), "Menú, registro y portada");
        yield return Run(Voice(), "Voz");
        yield return Run(Levels(), "Niveles con candado");
        yield return Run(Effects(), "Efectos");
        yield return Run(Campaign(), "Campaña");
        yield return Run(Online(), "En línea");

        report.Add($"\nFALLOS: {failures}   ERRORES EN CONSOLA: {errors.Count}");
        report.AddRange(errors.Select(e => "  " + e));
        Save();
        Debug.Log("[PRUEBA] Terminada. Informe en " + Folder + "/informe.txt");
        EditorApplication.isPlaying = false;
    }

    // Si una parte se rompe, se anota y se sigue con la siguiente
    IEnumerator Run(IEnumerator part, string name)
    {
        report.Add("\n== " + name);
        var stack = new Stack<IEnumerator>();
        stack.Push(part);
        float limit = Time.realtimeSinceStartup + 90f;

        while (stack.Count > 0)
        {
            object current = null;
            try
            {
                if (Time.realtimeSinceStartup > limit) throw new TimeoutException("tardó demasiado");
                if (!stack.Peek().MoveNext()) { stack.Pop(); continue; }
                current = stack.Peek().Current;
            }
            catch (Exception e)
            {
                Check(false, $"{name} se interrumpió: {e.GetType().Name}: {e.Message}");
                break;
            }
            if (current is IEnumerator inner) stack.Push(inner);
            else yield return current;
        }
        Save();
    }

    void Save() => File.WriteAllLines(Folder + "/informe.txt", report);

    void Check(bool ok, string what)
    {
        if (!ok) failures++;
        report.Add((ok ? "  OK     " : "  FALLA  ") + what);
        Save();
    }

    static IEnumerator Wait(float seconds) { yield return new WaitForSecondsRealtime(seconds); }

    IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot($"{Folder}/{name}.png");
        yield return Wait(0.4f);
    }

    static Transform Find(string path)
    {
        string[] parts = path.Split(new[] { '/' }, 2);
        GameObject root = GameObject.Find(parts[0]);
        if (root == null) return null;
        return parts.Length == 1 ? root.transform : root.transform.Find(parts[1]);
    }

    static void Click(Transform t) => t.GetComponent<Button>().onClick.Invoke();

    // -----------------------------------------------------------------------------------------

    IEnumerator Menu()
    {
        yield return Wait(1.5f);
        GameObject intro = GameObject.Find("Intro");
        Check(intro != null, "La intro aparece al abrir");
        for (int i = 1; i <= 6; i++)
            Check(Resources.Load<Texture2D>("Intro/intro_" + i) != null, $"Imagen {i} de la intro");
        yield return Wait(2.5f);
        yield return Capture("01_intro");
        if (intro != null) Destroy(intro);
        yield return Wait(0.3f);

        Check(Find("Registro") != null, "Se pide el registro con DNI");
        var name = Find("Registro/Caja/Nombre").GetComponent<TMP_InputField>();
        var dni = Find("Registro/Caja/Dni").GetComponent<TMP_InputField>();
        name.text = "Prueba";
        dni.text = "1234567";
        Click(Find("Registro/Caja/Mujer"));
        Click(Find("Registro/Caja/Continuar"));
        yield return Wait(0.3f);
        Check(Find("Registro") != null && !PerfilJugador.EstaRegistrado, "No deja continuar con un DNI de 7 dígitos");
        yield return Capture("02_registro_dni_invalido");

        dni.text = "12345678";
        Click(Find("Registro/Caja/Continuar"));
        yield return Wait(0.5f);
        Check(Find("Registro") == null && PerfilJugador.EstaRegistrado && PerfilJugador.Sexo == Sexo.Mujer, "Con datos válidos guarda el perfil");
        bool welcome = FindObjectsByType<TextMeshProUGUI>().Any(t => t.text.StartsWith("Bienvenido"));
        Check(welcome, "El menú muestra \"Bienvenido, <nombre>\"");
        Check(Find("Menu/FondoPixel") != null && Find("Menu/TituloPixel") != null && Find("Menu/Logo") != null, "Portada: fondo, título y logo");
        Check(Find("Menu/GuerreroCusco") != null && Find("Menu/GuerreroQuito") != null, "Portada: personajes animados");
        yield return Capture("03_portada");
    }

    IEnumerator Voice()
    {
        Click(Find("Menu/PanelPrincipal/Voz"));
        yield return Wait(0.5f);
        Check(Find("Voz") != null, "Se abre la pantalla \"El garfiero repite\"");
        yield return Capture("04_voz");

        IServicioVoz voice = ServicioVoz.Actual;
        report.Add("  Servicio de voz: " + voice.GetType().Name);
        Check(voice.Disponible, "Reconocimiento de voz disponible en esta PC");

        string error = null;
        Action<string> onError = m => error = m;
        voice.AlFallar += onError;
        voice.Hablar("Prueba de voz de mujer", Sexo.Mujer);
        yield return Wait(5f);
        voice.Hablar("Prueba de voz de hombre", Sexo.Hombre);
        yield return Wait(5f);
        voice.EmpezarAEscuchar();
        yield return Wait(2f);
        Check(error == null, "El micrófono se enciende sin errores" + (error != null ? ": " + error : ""));
        voice.DejarDeEscuchar();
        voice.AlFallar -= onError;
        Click(Find("Voz/Caja/Volver"));
        yield return Wait(0.3f);
    }

    IEnumerator Levels()
    {
        MainMenu menu = FindAnyObjectByType<MainMenu>();
        menu.ShowLevels();
        menu.SetMode(0);
        menu.SetCharacter(0);
        yield return null;
        Click(Find("Menu/PanelNiveles/Dificultad_1"));
        yield return Wait(0.5f);
        Check(Find("Menu/PanelNiveles/Arena1").GetComponent<Button>().interactable, "Nivel 1 abierto");
        Check(!Find("Menu/PanelNiveles/Arena2").GetComponent<Button>().interactable, "Nivel 2 con candado");
        Check(!Find("Menu/PanelNiveles/Arena3").GetComponent<Button>().interactable, "Nivel 3 con candado");
        yield return Capture("05_niveles");
    }

    IEnumerator Effects()
    {
        GameSession.Mode = GameMode.Clasico;
        SceneLoader.Load("Arena01");
        yield return WaitPlaying();
        Freeze();

        Unit player = Unit.FindPlayer();
        Unit enemy = Unit.All.First(u => u.team != player.team);
        float side = Mathf.Sign(player.transform.position.z);
        Place(player, new Vector3(0f, player.transform.position.y, 4f * side));
        Place(enemy, new Vector3(0f, enemy.transform.position.y, -4f * side));
        yield return Wait(0.5f);

        // Polvo al caminar
        var agent = enemy.GetComponent<NavMeshAgent>();
        agent.isStopped = false;
        agent.speed = 5f;
        agent.SetDestination(enemy.transform.position + new Vector3(3f, 0f, 0f));
        yield return Expect("polvo", 2f, "Polvo al caminar", null);
        agent.isStopped = true;
        Place(enemy, new Vector3(0f, enemy.transform.position.y, -4f * side));
        yield return Wait(0.5f);

        // Garfio del jugador: golpe, número de daño y salpicadura al cruzar el río
        var seen = new HashSet<string>();
        player.GetComponent<HookAbility>().TryFire(enemy.transform.position);
        yield return Watch(seen, 2.5f, "06_garfio");
        Check(seen.Contains("destello"), "Destello al recibir un golpe");
        Check(seen.Contains("NumeroDeDano"), "Número de daño");
        Check(seen.Contains("onda"), "Salpicadura al arrastrarlo por el río");

        // Escudo: el garfio se refleja y salen chispas
        yield return Wait(2f);
        Freeze();
        enemy = Unit.All.FirstOrDefault(u => u.team != player.team && u.IsAlive);
        Check(enemy != null && player.IsAlive, "Hay un enemigo para probar el escudo");
        Place(player, new Vector3(0f, player.transform.position.y, 4f * side));
        Place(enemy, new Vector3(0f, enemy.transform.position.y, -4f * side));
        yield return Wait(0.5f);
        seen.Clear();
        player.GetComponent<ShieldAbility>().TryActivate();
        enemy.GetComponent<HookAbility>().TryFire(player.transform.position);
        yield return Watch(seen, 1.5f, "07_escudo");
        Check(seen.Contains("chispa"), "Chispas al reflejar el garfio con el escudo");
    }

    IEnumerator Campaign()
    {
        SceneLoader.Load("Arena01");
        for (int level = 0; level < 3; level++)
        {
            yield return WaitPlaying();
            yield return Wait(0.5f);
            Check(SceneManager.GetActiveScene().name == Niveles.Escenas[level], $"Cargó el nivel {level + 1}");
            yield return Capture($"08_nivel{level + 1}");
            yield return Win();
            yield return Wait(1f);
            Check(Niveles.Desbloqueado == Mathf.Min(level + 1, 2), $"Ganar el nivel {level + 1} deja abierto hasta el {Mathf.Min(level + 2, 3)}");

            Button next = FindObjectsByType<Button>().FirstOrDefault(b => b.name == "Siguiente");
            Check((next != null) == (level < 2), level < 2 ? "Aparece \"Siguiente nivel\"" : "En el último no hay \"Siguiente nivel\"");
            yield return Capture($"09_fin_nivel{level + 1}");
            if (next == null) yield break;

            Scene previous = SceneManager.GetActiveScene();
            next.onClick.Invoke();
            yield return new WaitUntil(() => SceneManager.GetActiveScene() != previous);
        }
    }

    IEnumerator Online()
    {
        SceneLoader.Load("MainMenu");
        yield return Wait(1f);
        GameObject intro = GameObject.Find("Intro");
        if (intro != null) Destroy(intro);

        Click(Find("Menu/PanelPrincipal/EnLinea"));
        yield return Wait(0.5f);
        Check(Find("EnLinea") != null, "Se abre la pantalla de salas");

        string code = null, error = null;
        IServicioRed net = ServicioRed.Actual;
        Action<string> created = c => code = c, failed = m => error = m;
        net.AlCrearSala += created;
        net.AlFallar += failed;
        Click(Find("EnLinea/Caja/Crear"));
        float limit = Time.realtimeSinceStartup + 25f;
        yield return new WaitUntil(() => code != null || error != null || Time.realtimeSinceStartup > limit);
        Check(code != null, code != null ? "Relay devolvió el código " + code : "No se creó la sala: " + (error ?? "sin respuesta"));
        yield return Capture("10_sala");
        net.AlCrearSala -= created;
        net.AlFallar -= failed;
        net.Salir();
    }

    // -----------------------------------------------------------------------------------------

    static IEnumerator WaitPlaying()
    {
        yield return Wait(0.5f);
        yield return new WaitUntil(() => GameManager.Instance != null && GameManager.Instance.State == GameState.Jugando);
    }

    // Los bots se quedan quietos para que la prueba sea siempre igual
    static void Freeze()
    {
        foreach (Unit u in Unit.All)
        {
            var bot = u.GetComponent<BotController>();
            if (bot == null) continue;
            bot.enabled = false;
            var agent = u.GetComponent<NavMeshAgent>();
            if (agent != null && agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
        }
    }

    static void Place(Unit u, Vector3 position)
    {
        var cc = u.GetComponent<CharacterController>();
        var agent = u.GetComponent<NavMeshAgent>();
        if (cc != null) cc.enabled = false;
        if (agent != null) agent.Warp(position);
        else u.transform.position = position;
        if (cc != null) cc.enabled = true;
    }

    // Anota qué efectos aparecen durante unos segundos y toma una captura a la mitad
    IEnumerator Watch(HashSet<string> seen, float seconds, string capture)
    {
        float end = Time.realtimeSinceStartup + seconds;
        bool shot = false;
        while (Time.realtimeSinceStartup < end)
        {
            foreach (var p in FindObjectsByType<Particula>())
            {
                var sr = p.GetComponent<SpriteRenderer>();
                seen.Add(sr != null && sr.sprite != null ? sr.sprite.name : p.name);
            }
            if (!shot && (seen.Contains("onda") || seen.Contains("chispa")))
            {
                shot = true;
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot($"{Folder}/{capture}.png");
            }
            yield return null;
        }
    }

    IEnumerator Expect(string effect, float seconds, string what, string capture)
    {
        var seen = new HashSet<string>();
        yield return Watch(seen, seconds, capture);
        Check(seen.Contains(effect), what);
    }

    // Gana la partida eliminando enemigos (con menos kills y esperas para que sea rápido)
    IEnumerator Win()
    {
        GameManager gm = GameManager.Instance;
        gm.killsToWinRound = 1;
        gm.respawnDelay = 0.5f;
        gm.countdownSeconds = 1;
        Freeze();

        float limit = Time.realtimeSinceStartup + 40f;
        while (gm.State != GameState.FinPartida && Time.realtimeSinceStartup < limit)
        {
            if (gm.State == GameState.Jugando)
            {
                Unit player = Unit.FindPlayer();
                Unit enemy = Unit.All.FirstOrDefault(u => u.team != gm.playerTeam && u.IsAlive);
                if (player != null && enemy != null) enemy.Health.TakeDamage(9999f, player.gameObject);
            }
            yield return Wait(0.3f);
        }
        Check(gm.State == GameState.FinPartida, "La partida termina con victoria");
    }
}
#endif
