using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

// Prueba automática del modo Memoria. Solo se activa al abrir el juego con: -capturas <carpeta>
// Recorre el menú, juega el modo Memoria (aciertos y errores), toma capturas y escribe [AUTOTEST] en el registro.
public class AutoTest : MonoBehaviour
{
    string folder;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] != "-capturas") continue;
            var go = new GameObject("AutoTest");
            DontDestroyOnLoad(go);
            go.AddComponent<AutoTest>().folder = args[i + 1];
            return;
        }
    }

    void Start() { StartCoroutine(Run()); }

    IEnumerator Run()
    {
        Directory.CreateDirectory(folder);
        PlayerPrefs.DeleteKey(MemoryMode.RecordKey);
        PlayerPrefs.SetInt(GameManager.DifficultyKey, 1);
        GameSession.Character = 0;

        // 1. Menú: paso 1 con los 4 modos
        yield return new WaitForSeconds(1.5f);
        GameObject intro = GameObject.Find("Intro");
        if (intro != null) Destroy(intro);
        MainMenu menu = FindAnyObjectByType<MainMenu>();
        if (menu != null) menu.ShowLevels();
        yield return new WaitForSeconds(0.8f);
        yield return Capture("01_menu_modos");
        Log("Menú con 4 modos: OK");

        // 2. Partida de Memoria en la Pradera
        GameSession.Mode = GameMode.Memoria;
        SceneLoader.Load("Arena01");
        yield return Play(true);

        // 3. Arena 3 · Nieve: solo la fase de memorizar
        SceneLoader.Load("Arena03");
        yield return Play(false);

        Log("FIN OK");
        Application.Quit();
    }

    IEnumerator Play(bool full)
    {
        yield return new WaitUntil(() => MemoryMode.Instance != null);
        MemoryMode memory = MemoryMode.Instance;
        string scene = SceneManager.GetActiveScene().name;
        Unit player = Unit.FindPlayer();
        HookAbility hook = player.GetComponent<HookAbility>();
        Log($"{scene}: modo Memoria iniciado, bots restantes = {Unit.All.Count - 1}");

        yield return new WaitUntil(() => memory.CurrentPhase == MemoryMode.Phase.Memoriza);
        yield return new WaitForSeconds(1.75f);
        yield return Capture(scene + "_02_memoriza");
        if (!full) yield break;

        // Tres niveles bien jugados
        for (int level = 0; level < 3; level++)
        {
            yield return new WaitUntil(() => memory.CurrentPhase == MemoryMode.Phase.Repite);
            if (level == 0) { yield return new WaitForSeconds(0.3f); yield return Capture("03_tu_turno"); }
            int length = memory.SequenceLength;
            for (int step = 0; step < length; step++)
            {
                yield return new WaitUntil(() => hook.IsReady);
                Rune target = memory.ExpectedRune;
                if (target == null) break;
                hook.TryFire(target.transform.position);
                if (level == 1 && step == 1) { yield return new WaitForSeconds(0.25f); yield return Capture("04_garfio_runa"); }
                yield return new WaitUntil(() => hook.IsReady);
            }
            Log($"Nivel {level + 1} completado (secuencia de {length}), vidas = {memory.Lives}");
            if (level == 1) { yield return new WaitForSeconds(0.5f); yield return Capture("05_nivel_superado"); }
        }

        // Errores a propósito hasta perder las 3 vidas
        for (int e = 0; e < 3; e++)
        {
            yield return new WaitUntil(() => memory.CurrentPhase == MemoryMode.Phase.Repite);
            yield return new WaitUntil(() => hook.IsReady);
            hook.TryFire(memory.WrongRune.transform.position);
            yield return new WaitUntil(() => memory.CurrentPhase != MemoryMode.Phase.Repite);
            Log($"Error {e + 1}: vidas = {memory.Lives}");
            if (e == 0) { yield return new WaitForSeconds(0.4f); yield return Capture("06_error"); }
        }

        yield return new WaitUntil(() => GameManager.Instance.State == GameState.FinPartida);
        yield return new WaitForSeconds(2f);
        yield return Capture("07_fin_estadisticas");
        Log($"Fin: span = {MatchStats.MemorySpan}, aciertos = {MatchStats.MemoryHits}, errores = {MatchStats.MemoryErrors}, récord = {MemoryMode.Record}");
    }

    IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(folder, name + ".png"));
        yield return null;
    }

    static void Log(string msg) => Debug.Log("[AUTOTEST] " + msg);
}
