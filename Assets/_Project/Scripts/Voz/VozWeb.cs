#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using UnityEngine;

// Voz en el navegador (Web Speech API). El código JavaScript está en Assets/Plugins/WebGL/Voz.jslib.
public class VozWeb : IServicioVoz
{
    [DllImport("__Internal")] static extern int VozDisponible();
    [DllImport("__Internal")] static extern void VozEscuchar(string receptor);
    [DllImport("__Internal")] static extern void VozParar();
    [DllImport("__Internal")] static extern void VozHablar(string texto, int mujer);

    public event Action<string> AlReconocer;
    public event Action<string> AlOir;
    public event Action<string> AlFallar;

    public bool Disponible => VozDisponible() == 1;
    public bool Escuchando { get; private set; }

    public VozWeb()
    {
        // El navegador responde con SendMessage a este objeto
        var go = new GameObject("ServicioVozWeb");
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<ReceptorVozWeb>().servicio = this;
    }

    public void EmpezarAEscuchar()
    {
        Escuchando = true;
        VozEscuchar("ServicioVozWeb");
    }

    public void DejarDeEscuchar()
    {
        Escuchando = false;
        VozParar();
    }

    public void Hablar(string texto, Sexo voz) => VozHablar(texto, voz == Sexo.Mujer ? 1 : 0);

    public void Texto(string text) => AlReconocer?.Invoke(text);
    public void Parcial(string text) => AlOir?.Invoke(text);
    public void Error(string message) => AlFallar?.Invoke(message);
    public void Fin() => Escuchando = false;
}

public class ReceptorVozWeb : MonoBehaviour
{
    public VozWeb servicio;

    public void AlTexto(string text) => servicio.Texto(text);
    public void AlParcial(string text) => servicio.Parcial(text);
    public void AlError(string message) => servicio.Error(message);
    public void AlTerminar() => servicio.Fin();
}
#endif
