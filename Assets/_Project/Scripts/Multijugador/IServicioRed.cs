using System;

// Base para el modo MULTIJUGADOR EN LÍNEA (dos PCs distintas).
// Hoy el juego ya tiene "2 Jugadores" en la misma PC (GameMode.Versus); esto es para jugar por internet.
//
// TAREA DEL EQUIPO (ver TAREAS_EQUIPO.md, tareas M1 a M4). Opción recomendada:
//   Netcode for GameObjects + Unity Relay (paquetes oficiales de Unity, gratis para pocos jugadores).
//   Window > Package Manager > Unity Registry > "Netcode for GameObjects" y "Multiplayer Services".
public interface IServicioRed
{
    bool Conectado { get; }
    bool SoyAnfitrion { get; }

    // Crea una sala y devuelve (por el evento) un código corto para compartir, por ejemplo "K7Q2XZ"
    void CrearSala();

    // Se une a la sala de otro jugador con su código
    void UnirseASala(string codigo);

    void Salir();

    event Action<string> AlCrearSala;        // código de la sala
    event Action AlConectarseOtroJugador;
    event Action AlDesconectarse;
    event Action<string> AlFallar;           // mensaje de error
}
