using UnityEngine;
using UnityEngine.SceneManagement;

// Niveles en secuencia del modo Clásico (la "campaña" de la guerra civil inca).
// Ganar un nivel abre el siguiente. El avance se guarda en PlayerPrefs.
public static class Niveles
{
    const string Key = "nivel_desbloqueado";

    public static readonly string[] Escenas = { "Arena01", "Arena02", "Arena03" };
    public static readonly string[] Nombres = { "Chimborazo", "Apurímac", "Quipaipán" };
    public static readonly string[] Descripciones =
    {
        "La guerra avanza por los Andes del norte. Pradera andina.",
        "El ejército de Quito llega al cañón del gran río.",
        "La batalla final, cerca del Cusco (1532). Puna fría.",
    };

    // Índice del último nivel abierto (0 = solo el primero)
    public static int Desbloqueado => Mathf.Clamp(PlayerPrefs.GetInt(Key, 0), 0, Escenas.Length - 1);

    // Los candados solo valen en el modo Clásico; los otros modos pueden elegir cualquier mapa
    public static bool EstaAbierto(int nivel) => GameSession.Mode != GameMode.Clasico || nivel <= Desbloqueado;

    public static int IndiceActual => System.Array.IndexOf(Escenas, SceneManager.GetActiveScene().name);

    // El nivel que sigue al actual, o null si este era el último
    public static string Siguiente
    {
        get
        {
            int i = IndiceActual;
            return i >= 0 && i + 1 < Escenas.Length ? Escenas[i + 1] : null;
        }
    }

    // Se llama al ganar la partida del nivel actual
    public static void CompletarActual()
    {
        int i = IndiceActual;
        if (i < 0 || i + 1 >= Escenas.Length || i + 1 <= Desbloqueado) return;
        PlayerPrefs.SetInt(Key, i + 1);
        PlayerPrefs.Save();
    }
}
