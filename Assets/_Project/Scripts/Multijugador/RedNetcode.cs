using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

// Multijugador en línea con Netcode for GameObjects y Unity Relay: el anfitrión crea una sala,
// recibe un código y el invitado se une escribiéndolo. Máximo 2 jugadores.
// Los dos programas se hablan con mensajes (MensajeRed); la partida la simula el anfitrión (ver PartidaEnLinea).
//
// Requisito: el proyecto vinculado a Unity Cloud con Relay activado (Edit > Project Settings > Services).
public class RedNetcode : IServicioRed
{
    const string Canal = "arena";
    const int Puerto = 7777;

    public bool Conectado { get; private set; }      // Hay otro jugador al otro lado
    public bool SoyAnfitrion { get; private set; }

    public event Action<string> AlCrearSala;
    public event Action AlConectarseOtroJugador;
    public event Action AlDesconectarse;
    public event Action<string> AlFallar;
    public event Action<MensajeRed> AlRecibir;

    NetworkManager manager;
    UnityTransport transport;
    ISession session;
    ulong otro;
    bool ocupado;

    public async void CrearSala()
    {
        if (ocupado) return;
        ocupado = true;
        try
        {
            await IniciarServicios();
            Preparar();
            SoyAnfitrion = true;
            var options = new SessionOptions { MaxPlayers = 2 }.WithRelayNetwork();
            options.WithNetworkOptions(new NetworkOptions { RelayProtocol = Protocolo });
            session = await MultiplayerService.Instance.CreateSessionAsync(options);
            AlCrearSala?.Invoke(session.Code);
        }
        catch (Exception e) { Fallar("No se pudo crear la sala", e); }
        finally { ocupado = false; }
    }

    public async void UnirseASala(string codigo)
    {
        if (ocupado) return;
        if (string.IsNullOrWhiteSpace(codigo)) { AlFallar?.Invoke("Escribe el código de la sala"); return; }
        ocupado = true;
        try
        {
            await IniciarServicios();
            Preparar();
            SoyAnfitrion = false;
            var options = new JoinSessionOptions();
            options.WithNetworkOptions(new NetworkOptions { RelayProtocol = Protocolo });
            session = await MultiplayerService.Instance.JoinSessionByCodeAsync(codigo.Trim().ToUpperInvariant(), options);
        }
        catch (Exception e) { Fallar("No se pudo entrar a la sala. Revisa el código", e); }
        finally { ocupado = false; }
    }

    // Conexión directa sin internet (misma PC o misma red). Solo la usa la prueba automática.
    public void CrearLocal()
    {
        Preparar();
        SoyAnfitrion = true;
        transport.SetConnectionData("127.0.0.1", Puerto);
        if (!manager.StartHost()) AlFallar?.Invoke("No se pudo abrir el puerto " + Puerto);
    }

    public void UnirseLocal()
    {
        Preparar();
        SoyAnfitrion = false;
        transport.SetConnectionData("127.0.0.1", Puerto);
        if (!manager.StartClient()) AlFallar?.Invoke("No se pudo conectar");
    }

    // El navegador solo permite WebSockets
    static RelayProtocol Protocolo =>
#if UNITY_WEBGL && !UNITY_EDITOR
        RelayProtocol.WSS;
#else
        RelayProtocol.DTLS;
#endif

    static async Task IniciarServicios()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            // Cada copia abierta del juego usa su propio perfil anónimo: así dos ventanas en la misma PC
            // cuentan como jugadores distintos (si no, Unity responde "player is already a member of the lobby").
            var opciones = new InitializationOptions();
            opciones.SetProfile("p" + UnityEngine.Random.Range(0, 999999999)); // Al azar: también funciona en la versión web
            await UnityServices.InitializeAsync(opciones);
        }
        if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    void Preparar()
    {
        Salir();
        Application.runInBackground = true; // La partida no se detiene si la ventana pierde el foco

        if (manager == null)
        {
            var go = new GameObject("Red");
            UnityEngine.Object.DontDestroyOnLoad(go);
            manager = go.AddComponent<NetworkManager>();
            transport = go.AddComponent<UnityTransport>();
#if UNITY_WEBGL && !UNITY_EDITOR
            transport.UseWebSockets = true;
#endif
            manager.NetworkConfig = new NetworkConfig { NetworkTransport = transport, EnableSceneManagement = false };
            manager.OnServerStarted += Escuchar;
            manager.OnClientConnectedCallback += Conectar;
            manager.OnClientDisconnectCallback += Desconectar;
            manager.OnTransportFailure += () => Debug.LogWarning("[RED] falló el transporte");
        }
    }

    void Escuchar() => manager.CustomMessagingManager.RegisterNamedMessageHandler(Canal, Recibir);

    void Conectar(ulong id)
    {
        if (manager.IsServer && id == manager.LocalClientId) return; // El propio anfitrión
        Escuchar();
        otro = manager.IsServer ? id : NetworkManager.ServerClientId;
        Conectado = true;
        AlConectarseOtroJugador?.Invoke();
    }

    void Desconectar(ulong id)
    {
        Debug.Log($"[RED] desconectado id={id} motivo='{manager.DisconnectReason}' anfitrión={SoyAnfitrion}");
        if (!Conectado) return;
        Conectado = false;
        AlDesconectarse?.Invoke();
    }

    public void Enviar(MensajeRed mensaje)
    {
        if (!Conectado || manager == null || manager.CustomMessagingManager == null) return;
        string json = JsonUtility.ToJson(mensaje);
        using var writer = new FastBufferWriter(FastBufferWriter.GetWriteSize(json) + 16, Allocator.Temp);
        writer.WriteValueSafe(json);
        manager.CustomMessagingManager.SendNamedMessage(Canal, otro, writer, NetworkDelivery.ReliableFragmentedSequenced);
    }

    void Recibir(ulong sender, FastBufferReader reader)
    {
        reader.ReadValueSafe(out string json);
        AlRecibir?.Invoke(JsonUtility.FromJson<MensajeRed>(json));
    }

    public void Salir()
    {
        if (Conectado) Debug.Log("[RED] cerrando la conexión desde este lado");
        bool estaba = Conectado;
        Conectado = false;
        SoyAnfitrion = false;

        ISession old = session;
        session = null;
        // Con sala, LeaveAsync cierra la conexión de Relay; en la conexión directa se apaga a mano
        if (old != null) _ = Abandonar(old);
        else if (manager != null && manager.IsListening) manager.Shutdown();
        if (estaba) AlDesconectarse?.Invoke();
    }

    static async Task Abandonar(ISession old)
    {
        try { await old.LeaveAsync(); }
        catch (Exception e) { Debug.LogWarning("[RED] al salir de la sala: " + e.Message); }
    }

    void Fallar(string message, Exception e)
    {
        Debug.LogWarning(e);
        Salir();
        AlFallar?.Invoke($"{message}.\n<size=70%>{e.Message}</size>");
    }
}

// Un solo tipo de mensaje para todo: "tipo" dice qué campos se usan.
[Serializable]
public class MensajeRed
{
    public string tipo;                 // cargar, listo, controles, estado, cuenta, ronda, finRonda, fin, anuncio, kill, chispas
    public string texto;
    public int a, b;
    public bool si;
    public Vector3 punto;
    public ControlesRed controles;
    public EstadoRed estado;
}

// Lo que hace el invitado en un cuadro
[Serializable]
public class ControlesRed
{
    public Vector2 mover;
    public Vector3 apuntar;
    public bool garfio, red, fase, escudo;
}

// Foto de la partida que manda el anfitrión
[Serializable]
public class EstadoRed
{
    public List<UnidadRed> unidades = new List<UnidadRed>();
    public List<ProyectilRed> proyectiles = new List<ProyectilRed>();
    public int kills0, kills1, rondas0, rondas1, ronda, fase;
}

[Serializable]
public class UnidadRed
{
    public Vector3 p;
    public float giro, vida, vidaMax;
    public bool viva, enRed, enFase, jalado, escudo;
    public int dibujo, oro;
    public float recargaGarfio, recargaRed, recargaFase, esperaEscudo;
}

[Serializable]
public class ProyectilRed
{
    public int id, dueno;
    public bool esRed;
    public Vector3 p;
}
