using System;

// Lo que debe saber hacer cualquier sistema de voz del juego.
// Hay una versión de prueba (ServicioVozSimulado) para que el juego funcione mientras se programa la real.
//
// TAREA DEL EQUIPO (ver TAREAS_EQUIPO.md, tareas V2 y V3):
//  - Windows: reconocer con UnityEngine.Windows.Speech.DictationRecognizer y hablar con la voz de Windows.
//  - Web (GitHub Pages): usar la Web Speech API del navegador con un archivo .jslib.
public interface IServicioVoz
{
    // ¿Este dispositivo puede reconocer voz?
    bool Disponible { get; }

    // Empieza a escuchar el micrófono. Cuando entiende una frase dispara AlReconocer.
    void EmpezarAEscuchar();
    void DejarDeEscuchar();

    // Texto que entendió (por ejemplo: "lanza el garfio")
    event Action<string> AlReconocer;

    // Mensaje de error para mostrar en pantalla (sin micrófono, sin permiso, etc.)
    event Action<string> AlFallar;

    // Repite un texto en voz alta con voz de hombre o de mujer
    void Hablar(string texto, Sexo voz);
}
