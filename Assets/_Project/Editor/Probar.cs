using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using Debug = UnityEngine.Debug;

// Pruebas automáticas desde el menú "Arena del Garfio".
public static class Probar
{
    // El botón Play empieza siempre por el menú (intro, registro y portada), como el juego compilado,
    // aunque tengas abierta una arena.
    [InitializeOnLoadMethod]
    static void StartFromMenu()
    {
        EditorApplication.delayCall += () => EditorSceneManager.playModeStartScene =
            AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/_Project/Scenes/MainMenu.unity");
    }

    [MenuItem("Arena del Garfio/Probar todo (automático)")]
    public static void All()
    {
        if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/MainMenu.unity");
        SessionState.SetBool(PruebaCompleta.Flag, true);
        EditorApplication.isPlaying = true;
    }

    // Compila el juego para Windows y abre dos copias que se conectan entre sí en esta PC
    [MenuItem("Arena del Garfio/Probar multijugador (2 ventanas)")]
    public static void Network()
    {
        const string exe = "Build/PruebaRed/ArenaDelGarfio.exe";
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = exe,
            target = BuildTarget.StandaloneWindows64,
        });
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError("[PRUEBARED] No se pudo compilar el juego");
            return;
        }

        string folder = Path.GetFullPath("Logs/prueba-red");
        Directory.CreateDirectory(folder);
        foreach (string role in new[] { "anfitrion", "invitado" })
            Process.Start(Path.GetFullPath(exe),
                $"-screen-fullscreen 0 -screen-width 960 -screen-height 540 -logFile \"{folder}/{role}.log\" -prueba-red {role} \"{folder}\"");
        Debug.Log("[PRUEBARED] Dos ventanas abiertas. Registros en " + folder);
    }
}
