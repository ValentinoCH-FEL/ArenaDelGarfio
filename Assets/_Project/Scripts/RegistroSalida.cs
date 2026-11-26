using UnityEngine;

// Anota en el registro (Player.log) por qué se cerró el juego. Ayuda a encontrar cierres inesperados.
public static class RegistroSalida
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Iniciar()
    {
        Application.quitting += () => Debug.Log("[SALIDA] El juego se está cerrando");
    }
}
