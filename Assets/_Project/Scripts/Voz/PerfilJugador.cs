using UnityEngine;

public enum Sexo { Hombre = 0, Mujer = 1 }

// Datos del jugador tomados de su DNI: nombre, número y sexo (H/M).
// El sexo decide con qué voz (hombre o mujer) el juego repite lo que dijo el jugador.
// Se guardan en PlayerPrefs para no pedirlos cada vez.
//
// TAREA DEL EQUIPO (ver TAREAS_EQUIPO.md, tarea V1): crear la pantalla de registro que llena estos datos.
public static class PerfilJugador
{
    const string KeyNombre = "perfil_nombre";
    const string KeyDni = "perfil_dni";
    const string KeySexo = "perfil_sexo";

    public static string Nombre
    {
        get => PlayerPrefs.GetString(KeyNombre, "");
        set { PlayerPrefs.SetString(KeyNombre, value ?? ""); PlayerPrefs.Save(); }
    }

    public static string Dni
    {
        get => PlayerPrefs.GetString(KeyDni, "");
        set { PlayerPrefs.SetString(KeyDni, value ?? ""); PlayerPrefs.Save(); }
    }

    public static Sexo Sexo
    {
        get => (Sexo)PlayerPrefs.GetInt(KeySexo, 0);
        set { PlayerPrefs.SetInt(KeySexo, (int)value); PlayerPrefs.Save(); }
    }

    public static bool EstaRegistrado => EsDniValido(Dni) && !string.IsNullOrWhiteSpace(Nombre);

    // DNI peruano: 8 dígitos
    public static bool EsDniValido(string dni)
    {
        if (string.IsNullOrEmpty(dni) || dni.Length != 8) return false;
        foreach (char c in dni) if (c < '0' || c > '9') return false;
        return true;
    }

    // "H" o "M", como aparece en el DNI
    public static string SexoComoLetra => Sexo == Sexo.Mujer ? "M" : "H";
}
