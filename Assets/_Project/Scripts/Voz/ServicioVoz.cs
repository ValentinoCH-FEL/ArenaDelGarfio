using System;
using UnityEngine;

// Punto de entrada del sistema de voz. Cualquier script usa:
//     ServicioVoz.Actual.EmpezarAEscuchar();
//     ServicioVoz.Actual.AlReconocer += texto => { ... };
//     ServicioVoz.Actual.Hablar("Hola", PerfilJugador.Sexo);
//
// En Windows usa VozWindows, en el navegador VozWeb y en otros sistemas la versión de prueba.
public static class ServicioVoz
{
    static IServicioVoz actual;

    public static IServicioVoz Actual => actual ??= Crear();

    static IServicioVoz Crear()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return new VozWeb();
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        return new VozWindows();
#else
        return new ServicioVozSimulado();
#endif
    }
}

// Versión de prueba: no usa micrófono. Sirve para probar la pantalla y el flujo del juego.
// "Reconoce" lo que se le pase con Simular(...) y "habla" escribiendo en la consola.
public class ServicioVozSimulado : IServicioVoz
{
    public bool Disponible => true;
    public event Action<string> AlReconocer;
    public event Action<string> AlOir;
    public event Action<string> AlFallar;

    bool escuchando;
    public bool Escuchando => escuchando;

    public void EmpezarAEscuchar() { escuchando = true; Debug.Log("[Voz simulada] Escuchando..."); }
    public void DejarDeEscuchar() { escuchando = false; }

    // Para probar sin micrófono: ServicioVozSimulado.Simular("hola")
    public void Simular(string texto)
    {
        if (!escuchando) { AlFallar?.Invoke("No está escuchando"); return; }
        AlReconocer?.Invoke(texto);
    }

    public void Hablar(string texto, Sexo voz)
    {
        Debug.Log($"[Voz simulada] ({(voz == Sexo.Mujer ? "voz de mujer" : "voz de hombre")}) {texto}");
    }
}
