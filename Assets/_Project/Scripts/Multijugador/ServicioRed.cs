using System;
using UnityEngine;

// Punto de entrada del multijugador en línea: ServicioRed.Actual.CrearSala();
// Por ahora es una versión SIMULADA que no se conecta a nada, para poder armar la pantalla de salas.
// TAREA DEL EQUIPO (TAREAS_EQUIPO.md, tarea M2): reemplazar por RedNetcode (Netcode for GameObjects + Relay).
public static class ServicioRed
{
    static IServicioRed actual;
    public static IServicioRed Actual => actual ??= new ServicioRedSimulado();
}

public class ServicioRedSimulado : IServicioRed
{
    public bool Conectado { get; private set; }
    public bool SoyAnfitrion { get; private set; }

    public event Action<string> AlCrearSala;
    public event Action AlConectarseOtroJugador;
    public event Action AlDesconectarse;
    public event Action<string> AlFallar;

    public void CrearSala()
    {
        SoyAnfitrion = true;
        Conectado = true;
        string codigo = UnityEngine.Random.Range(100000, 999999).ToString();
        Debug.Log("[Red simulada] Sala creada: " + codigo);
        AlCrearSala?.Invoke(codigo);
    }

    public void UnirseASala(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo)) { AlFallar?.Invoke("Escribe el código de la sala"); return; }
        Conectado = true;
        Debug.Log("[Red simulada] Unido a la sala " + codigo);
        AlConectarseOtroJugador?.Invoke();
    }

    public void Salir()
    {
        Conectado = false;
        SoyAnfitrion = false;
        AlDesconectarse?.Invoke();
    }
}
