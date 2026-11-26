using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

// Prueba automática del multijugador en una sola PC, sin internet. Abrir el juego dos veces:
//   ArenaDelGarfio.exe -prueba-red anfitrion <carpeta>
//   ArenaDelGarfio.exe -prueba-red invitado <carpeta>
// Escribe [PRUEBARED] en el registro y guarda una captura de cada lado.
public class PruebaRed : MonoBehaviour
{
    bool host;
    string folder;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        int i = System.Array.IndexOf(args, "-prueba-red");
        if (i < 0 || i + 2 >= args.Length) return;

        var go = new GameObject("PruebaRed");
        DontDestroyOnLoad(go);
        var test = go.AddComponent<PruebaRed>();
        test.host = args[i + 1] == "anfitrion";
        test.folder = args[i + 2];
    }

    IEnumerator Start()
    {
        Directory.CreateDirectory(folder);
        yield return new WaitForSeconds(1f);

        PartidaEnLinea.Suscribir();
        RedNetcode net = ServicioRed.Netcode;
        if (host) net.CrearLocal();
        else
        {
            yield return new WaitForSeconds(2f);
            net.UnirseLocal();
        }

        float limit = Time.realtimeSinceStartup + 40f;
        yield return new WaitUntil(() => net.Conectado || Time.realtimeSinceStartup > limit);
        Log("Conectado = " + net.Conectado);
        if (!net.Conectado) { Application.Quit(1); yield break; }

        if (host)
        {
            yield return new WaitForSeconds(1f);
            PartidaEnLinea.Empezar("Arena01");
        }

        yield return new WaitUntil(() => GameManager.Instance != null && GameManager.Instance.State == GameState.Jugando);
        Log("Ronda en juego");

        // El invitado camina y lanza el garfio; el anfitrión solo observa
        for (int i = 0; i < 40 && PartidaEnLinea.Activa; i++)
        {
            if (!host)
            {
                Unit target = Unit.All.FirstOrDefault(u => u.team == TeamId.Azul && u.IsAlive);
                PartidaEnLinea.ControlesDePrueba = new ControlesRed
                {
                    mover = new Vector2(i / 5 % 2 == 0 ? 1f : -1f, 0f),
                    apuntar = target != null ? target.transform.position : Vector3.zero,
                    garfio = i % 4 == 0,
                };
            }
            yield return new WaitForSeconds(0.5f);
            if (i == 10)
            {
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(folder, host ? "red_anfitrion.png" : "red_invitado.png"));
            }
            if (i % 4 == 3) Report();
        }

        Log(PartidaEnLinea.Activa ? "FIN OK" : "FIN CON FALLO: la partida se cortó antes de tiempo");
        yield return new WaitForSeconds(host ? 3f : 0.5f);
        Application.Quit();
    }

    void Report()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        string units = string.Join(" | ", Unit.All.Where(u => u != null)
            .OrderBy(u => (int)u.team).ThenBy(u => u.SpawnPosition.x).ThenBy(u => u.SpawnPosition.z)
            .Select(u => $"{(u.team == TeamId.Azul ? "C" : "Q")} ({u.transform.position.x:0},{u.transform.position.z:0}) v{u.Health.Current:0}"));
        Unit mine = Unit.FindPlayer();
        Log($"HUD de este lado: {(mine == null ? "?" : mine.displayName + " " + mine.team)}");
        Log($"Marcador {gm.GetKills(TeamId.Azul)}-{gm.GetKills(TeamId.Rojo)}  ronda {gm.Round}  {units}");
    }

    static void Log(string message) => Debug.Log("[PRUEBARED] " + message);
}
