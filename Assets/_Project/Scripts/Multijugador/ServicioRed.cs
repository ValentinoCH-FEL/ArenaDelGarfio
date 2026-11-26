using System;
using UnityEngine;

// Punto de entrada del multijugador en línea: ServicioRed.Actual.CrearSala();
// La implementación real está en RedNetcode (Netcode for GameObjects + Relay).
public static class ServicioRed
{
    static RedNetcode netcode;
    public static RedNetcode Netcode => netcode ??= new RedNetcode();
    public static IServicioRed Actual => Netcode;
}
