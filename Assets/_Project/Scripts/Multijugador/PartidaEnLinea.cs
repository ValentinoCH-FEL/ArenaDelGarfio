using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

// Partida en línea (usa las reglas del modo 2 Jugadores).
//  - El ANFITRIÓN (Cusco) simula todo: movimiento, garfios, daño, muertes y marcador.
//  - El INVITADO (Quito) solo envía sus controles y dibuja lo que el anfitrión le manda.
// Así los dos ven siempre el mismo marcador.
[DefaultExecutionOrder(100)]
public class PartidaEnLinea : MonoBehaviour
{
    const float SendInterval = 1f / 30f;

    public static bool Activa { get; private set; }
    public static bool EsAnfitrion => Activa && ServicioRed.Netcode.SoyAnfitrion;
    public static bool EsInvitado => Activa && !ServicioRed.Netcode.SoyAnfitrion;

    // Controles del invitado que el anfitrión aplica al jugador 2
    public static ControlesRed Remoto { get; private set; } = new ControlesRed();

    // La prueba automática pone aquí controles fingidos
    public static ControlesRed ControlesDePrueba;

    static int? invitadoListo; // Personaje del invitado, cuando ya cargó la arena
    static bool suscrito;

    GameManager gm;
    List<Unit> units;
    float nextSend;
    bool started;
    readonly Dictionary<int, GameObject> ghosts = new Dictionary<int, GameObject>();

    // -----------------------------------------------------------------------------------------
    // Sala: empezar y terminar
    // -----------------------------------------------------------------------------------------

    // Se llama al abrir la pantalla de salas
    public static void Suscribir()
    {
        if (suscrito) return;
        suscrito = true;
        ServicioRed.Netcode.AlRecibir += RecibirDeSala;
        ServicioRed.Netcode.AlDesconectarse += Desconectado;
    }

    // El anfitrión elige el nivel y los dos cargan la misma arena
    public static void Empezar(string scene)
    {
        ServicioRed.Netcode.Enviar(new MensajeRed { tipo = "cargar", texto = scene, a = GameManager.MenuDifficulty });
        Cargar(scene);
    }

    static void Cargar(string scene)
    {
        Activa = true;
        invitadoListo = null;
        Remoto = new ControlesRed();
        SceneLoader.Load(scene);
    }

    public static void Terminar()
    {
        Activa = false;
        ServicioRed.Actual.Salir();
    }

    static void RecibirDeSala(MensajeRed m)
    {
        if (m.tipo == "cargar" && !ServicioRed.Netcode.SoyAnfitrion && System.Array.IndexOf(Niveles.Escenas, m.texto) >= 0)
        {
            PlayerPrefs.SetInt(GameManager.DifficultyKey, m.a);
            Cargar(m.texto);
        }
        else if (m.tipo == "listo") invitadoListo = m.a;
    }

    static void Desconectado()
    {
        Debug.Log("[RED] se perdió la conexión (en partida: " + Activa + ")");
        if (!Activa) return;
        Activa = false;
        ServicioRed.Actual.Salir();
        SceneLoader.Load("MainMenu");
    }

    // El invitado juega con el personaje rojo: su vida, oro y recargas son los que se muestran
    public static void ElegirJugadorLocal(TeamId anfitrion)
    {
        foreach (Unit u in Unit.All)
            if (u != null) u.isPlayer = u.team != anfitrion && u.GetComponent<PlayerController>() != null;
    }

    // -----------------------------------------------------------------------------------------
    // Preparación de la arena
    // -----------------------------------------------------------------------------------------

    void Start()
    {
        gm = GameManager.Instance;
        units = Unit.All.Where(u => u != null)
            .OrderBy(u => (int)u.team).ThenBy(u => u.SpawnPosition.x).ThenBy(u => u.SpawnPosition.z).ToList();

        RuneSpawner spawner = FindAnyObjectByType<RuneSpawner>();
        if (spawner != null) spawner.enabled = false;

        ServicioRed.Netcode.AlRecibir += Recibir;

        if (EsInvitado) PrepararInvitado();
        else
        {
            gm.OnCountdown += c => Enviar("cuenta", a: c);
            gm.OnRoundStarted += r => Enviar("ronda", a: r);
            gm.OnRoundEnded += t => Enviar("finRonda", a: (int)t);
            gm.OnGameOver += won => Enviar("fin", si: won);
            gm.OnAnnouncement += text => Enviar("anuncio", text);
            gm.OnKill += (killer, victim) => Enviar("kill", a: units.IndexOf(killer), b: units.IndexOf(victim));
            Efectos.AlSacarChispas += EnviarChispas;
        }
    }

    // El invitado maneja al jugador rojo y no simula nada: se apagan movimiento, IA y habilidades
    void PrepararInvitado()
    {
        foreach (Unit u in units)
        {
            var bot = u.GetComponent<BotController>();
            if (bot != null) bot.enabled = false;
            var agent = u.GetComponent<NavMeshAgent>();
            if (agent != null) agent.enabled = false;
            var cc = u.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            var pc = u.GetComponent<PlayerController>();
            if (pc != null) pc.enabled = false;

            var hook = u.GetComponent<HookAbility>(); if (hook != null) hook.usePlayerInput = false;
            var net = u.GetComponent<NetAbility>(); if (net != null) net.usePlayerInput = false;
            var dash = u.GetComponent<DashAbility>(); if (dash != null) dash.usePlayerInput = false;
            var shield = u.GetComponent<ShieldAbility>(); if (shield != null) shield.usePlayerInput = false;
        }
    }

    void OnDestroy()
    {
        ServicioRed.Netcode.AlRecibir -= Recibir;
        Efectos.AlSacarChispas -= EnviarChispas;
    }

    // -----------------------------------------------------------------------------------------
    // Cada cuadro
    // -----------------------------------------------------------------------------------------

    void Update()
    {
        if (!Activa) return;

        if (EsInvitado)
        {
            if (!started && Time.unscaledTime >= nextSend)
            {
                // Avisa hasta que lleguen los primeros datos del anfitrión
                nextSend = Time.unscaledTime + 0.3f;
                Enviar("listo", a: GameSession.Character);
            }
            EnviarControles();
        }
        else if (!started && invitadoListo.HasValue)
        {
            started = true;
            Unit guest = units.FirstOrDefault(u => u.team != gm.playerTeam && u.GetComponent<PlayerController>() != null);
            gm.ApplyCharacter(guest, invitadoListo.Value);
            gm.ComenzarEnLinea();
        }
    }

    // El anfitrión manda la foto de la partida después de que todo se movió
    void LateUpdate()
    {
        if (!EsAnfitrion) return;

        // Los botones pulsados ya se usaron en este cuadro
        Remoto.garfio = Remoto.red = Remoto.fase = Remoto.escudo = false;

        if (!started || Time.unscaledTime < nextSend) return;
        nextSend = Time.unscaledTime + SendInterval;
        ServicioRed.Netcode.Enviar(new MensajeRed { tipo = "estado", estado = Fotografiar() });
    }

    void EnviarControles()
    {
        var c = new ControlesRed();
        Unit me = Unit.FindPlayer();
        if (ControlesDePrueba != null) c = ControlesDePrueba;
        else if (me != null)
        {
            c.mover = InputMap.Move(0);
            c.garfio = InputMap.Hook(0);
            c.red = InputMap.Net(0);
            c.fase = InputMap.Phase(0);
            c.escudo = InputMap.Shield(0);
            c.apuntar = me.transform.position + me.transform.forward * 12f;

            Camera cam = Camera.main;
            if (cam != null && Mouse.current != null)
            {
                Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
                if (new Plane(Vector3.up, me.transform.position).Raycast(ray, out float d)) c.apuntar = ray.GetPoint(d);
            }
            if (VozControl.Mira.HasValue) c.apuntar = VozControl.Mira.Value;
        }
        ServicioRed.Netcode.Enviar(new MensajeRed { tipo = "controles", controles = c });
    }

    EstadoRed Fotografiar()
    {
        var s = new EstadoRed
        {
            kills0 = gm.GetKills(TeamId.Azul), kills1 = gm.GetKills(TeamId.Rojo),
            rondas0 = gm.GetRoundsWon(TeamId.Azul), rondas1 = gm.GetRoundsWon(TeamId.Rojo),
            ronda = gm.Round, fase = (int)gm.State,
        };

        foreach (Unit u in units)
        {
            var state = new UnidadRed();
            s.unidades.Add(state);
            if (u == null) continue;

            var hook = u.GetComponent<HookAbility>();
            var net = u.GetComponent<NetAbility>();
            var dash = u.GetComponent<DashAbility>();
            var shield = u.GetComponent<ShieldAbility>();
            var sprite = u.GetComponent<SpriteCharacter>();

            state.p = u.transform.position;
            state.giro = u.transform.eulerAngles.y;
            state.vida = u.Health.Current;
            state.vidaMax = u.Health.Max;
            state.viva = u.gameObject.activeSelf;
            state.enRed = u.Status.IsRooted;
            state.enFase = u.Status.IsPhased;
            state.jalado = u.Status.IsPulled;
            state.escudo = shield != null && shield.IsActive;
            state.dibujo = sprite != null ? sprite.Kind : 0;
            state.oro = u.Wallet != null ? u.Wallet.Gold : 0;
            state.recargaGarfio = hook != null ? hook.CooldownPercent : 0f;
            state.recargaRed = net != null ? net.CooldownPercent : 0f;
            state.recargaFase = dash != null ? dash.CooldownPercent : 0f;
            state.esperaEscudo = shield != null ? shield.SecondsLeft : 0f;
        }

        foreach (var h in HookProjectile.Active)
            if (h != null && h.Ability != null)
                s.proyectiles.Add(new ProyectilRed { id = h.gameObject.GetEntityId().GetHashCode(), dueno = units.IndexOf(h.Ability.Owner), p = h.transform.position });
        foreach (var n in NetProjectile.Active)
            if (n != null && n.Owner != null)
                s.proyectiles.Add(new ProyectilRed { id = n.gameObject.GetEntityId().GetHashCode(), dueno = units.IndexOf(n.Owner), p = n.transform.position, esRed = true });
        return s;
    }

    // -----------------------------------------------------------------------------------------
    // Mensajes
    // -----------------------------------------------------------------------------------------

    void Enviar(string type, string text = null, int a = 0, int b = 0, bool si = false) =>
        ServicioRed.Netcode.Enviar(new MensajeRed { tipo = type, texto = text, a = a, b = b, si = si });

    void EnviarChispas(Vector3 p) => ServicioRed.Netcode.Enviar(new MensajeRed { tipo = "chispas", punto = p });

    void Recibir(MensajeRed m)
    {
        if (EsAnfitrion)
        {
            if (m.tipo != "controles" || m.controles == null) return;
            // Los botones se acumulan hasta que el juego los lea
            ControlesRed c = m.controles;
            c.garfio |= Remoto.garfio;
            c.red |= Remoto.red;
            c.fase |= Remoto.fase;
            c.escudo |= Remoto.escudo;
            Remoto = c;
            return;
        }

        switch (m.tipo)
        {
            case "estado": started = true; Aplicar(m.estado); break;
            case "cuenta": gm.AvisarCuenta(m.a); break;
            case "ronda": gm.AvisarRonda(m.a); break;
            case "finRonda": gm.AvisarFinRonda((TeamId)m.a); break;
            case "fin": gm.AvisarFinPartida(m.si); break;
            case "anuncio": gm.Announce(m.texto); break;
            case "kill": gm.AvisarKill(At(m.a), At(m.b)); break;
            case "chispas": Efectos.Chispas(m.punto); AudioManager.Play(Sfx.GarfioPared); break;
        }
    }

    Unit At(int i) => i >= 0 && i < units.Count ? units[i] : null;

    void Aplicar(EstadoRed s)
    {
        for (int i = 0; i < units.Count && i < s.unidades.Count; i++)
        {
            Unit u = units[i];
            UnidadRed state = s.unidades[i];
            if (u == null) continue;

            if (state.viva && !u.gameObject.activeSelf)
            {
                u.gameObject.SetActive(true);
                u.transform.position = state.p;
            }

            var sprite = u.GetComponent<SpriteCharacter>();
            if (sprite != null && sprite.Kind != state.dibujo) sprite.SetKind(state.dibujo);
            u.Health.SetRemote(state.vida, state.vidaMax);
            if (!state.viva) { u.gameObject.SetActive(false); continue; }

            // Interpolación suave hacia la posición del anfitrión
            bool far = (u.transform.position - state.p).sqrMagnitude > 16f;
            u.transform.position = far ? state.p : Vector3.Lerp(u.transform.position, state.p, 0.6f);
            u.transform.rotation = Quaternion.Euler(0f, state.giro, 0f);
            u.Status.SetRemote(state.enRed, state.enFase, state.jalado);
            if (u.Wallet != null) u.Wallet.SetRemote(state.oro);

            u.GetComponent<HookAbility>()?.SetRemoteCooldown(state.recargaGarfio);
            u.GetComponent<NetAbility>()?.SetRemoteCooldown(state.recargaRed);
            u.GetComponent<DashAbility>()?.SetRemoteCooldown(state.recargaFase);
            u.GetComponent<ShieldAbility>()?.SetRemote(state.escudo, state.esperaEscudo);
        }

        gm.ApplyRemote(s.kills0, s.kills1, s.rondas0, s.rondas1, s.ronda, (GameState)s.fase);
        DibujarProyectiles(s.proyectiles);
    }

    // Garfios y redes del invitado: solo el dibujo, sin choques ni daño
    void DibujarProyectiles(List<ProyectilRed> list)
    {
        var alive = new HashSet<int>();
        foreach (ProyectilRed p in list)
        {
            Unit owner = At(p.dueno);
            if (owner == null) continue;
            alive.Add(p.id);

            if (!ghosts.TryGetValue(p.id, out GameObject ghost) || ghost == null)
            {
                ghost = CrearFantasma(owner, p);
                if (ghost == null) continue;
                ghosts[p.id] = ghost;
                owner.GetComponent<SpriteCharacter>()?.OnThrow();
                AudioManager.Play(p.esRed ? Sfx.Red : Sfx.GarfioLanzar);
            }

            Vector3 direction = p.p - ghost.transform.position;
            ghost.transform.position = p.p;
            if (p.esRed) ghost.transform.Rotate(Vector3.up, 540f * SendInterval, Space.World);

            var line = ghost.GetComponent<LineRenderer>();
            var hook = owner.GetComponent<HookAbility>();
            if (!p.esRed && line != null && hook != null)
            {
                line.SetPosition(0, hook.ChainOrigin);
                line.SetPosition(1, p.p);
                Vector3 outward = p.p - hook.ChainOrigin;
                outward.y = 0f;
                if (direction.sqrMagnitude > 0f && outward.sqrMagnitude > 0.01f) ghost.transform.rotation = Quaternion.LookRotation(outward);
            }
        }

        foreach (int id in ghosts.Keys.Where(k => !alive.Contains(k)).ToList())
        {
            if (ghosts[id] != null) Destroy(ghosts[id]);
            ghosts.Remove(id);
        }
    }

    static GameObject CrearFantasma(Unit owner, ProyectilRed p)
    {
        if (p.esRed)
        {
            var net = owner.GetComponent<NetAbility>();
            if (net == null || net.Prefab == null) return null;
            NetProjectile copy = Instantiate(net.Prefab, p.p, Quaternion.identity);
            GameObject go = copy.gameObject;
            DestroyImmediate(copy);
            return go;
        }

        var hook = owner.GetComponent<HookAbility>();
        if (hook == null || hook.Prefab == null) return null;
        HookProjectile projectile = Instantiate(hook.Prefab, p.p, owner.transform.rotation);
        GameObject ghost = projectile.gameObject;
        DestroyImmediate(projectile);
        var line = ghost.GetComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = true;
        return ghost;
    }
}
