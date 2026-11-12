using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Reglas de la partida: cuenta regresiva, kills, rondas, respawn, oro, pausa y fin del juego.
// Modos: Clásico (rondas), Supervivencia (oleadas y vidas) y 2 Jugadores (un humano en cada orilla).
public enum GameState { Cuenta, Jugando, FinRonda, FinPartida }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Reglas")]
    public int killsToWinRound = 10;
    public int roundsToWin = 2;          // Mejor de 3
    public float respawnDelay = 5f;
    public int countdownSeconds = 3;
    public TeamId playerTeam = TeamId.Azul;

    [Header("Reaparición al azar (cada equipo en su lado del río)")]
    public bool randomRespawn = true;
    public float respawnHalfWidth = 16f;   // Qué tan a la izquierda o derecha (eje X)
    public float respawnMinDistance = 3f;  // Distancia mínima al río (eje Z)
    public float respawnMaxDistance = 11f; // Distancia máxima al río
    public bool showRespawnTimers = true;  // Mensajes de reaparición en pantalla

    [Header("Oro")]
    public int goldPerKill = 100;
    public int goldPerRound = 150;
    public float goldPerSecond = 5f;     // Oro que todos ganan cada segundo mientras se juega

    [Header("Dificultad de la IA (1 a 6)")]
    [Range(1, 6)] public int baseDifficulty = 3;   // Arena01: 3 · Arena02: 4 · Arena03: 5
    public bool increasePerRound = true;            // +1 por cada ronda nueva
    public int leadForExtraDifficulty = 2;          // Si el jugador saca esta ventaja en kills, +1
    public List<ItemData> botItems = new List<ItemData>(); // Mejoras que los bots compran al final de cada ronda

    [Header("Supervivencia")]
    public int survivalLives = 3;
    public int maxSurvivalEnemies = 5;

    [Header("Efectos")]
    public GameObject deathEffectPrefab;

    public GameState State { get; private set; }
    public bool IsPaused { get; private set; }
    public bool IsPlaying => State == GameState.Jugando && !IsPaused;
    public int Round { get; private set; }

    // Supervivencia
    public const string RecordKey = "record_oleada";
    public bool IsSurvival => GameSession.Mode == GameMode.Supervivencia;
    public bool IsVersus => GameSession.Mode == GameMode.Versus;
    public int Wave { get; private set; } = 1;
    public int WaveKills { get; private set; }
    public int WaveGoal => 4 + 2 * (Wave - 1);
    public int Lives { get; private set; }
    public bool NewRecord { get; private set; }
    public static int SurvivalRecord => PlayerPrefs.GetInt(RecordKey, 0);

    public event Action OnScoreChanged;
    public event Action<int> OnCountdown;      // 3, 2, 1 y 0 (0 = ¡YA!)
    public event Action<int> OnRoundStarted;
    public event Action<TeamId> OnRoundEnded;  // equipo ganador
    public event Action<bool> OnGameOver;      // true = ganó el jugador (o nuevo récord en supervivencia)
    public event Action<Unit, Unit> OnKill;    // asesino (puede ser null), víctima

    public event Action<int> OnDifficultyChanged;
    public event Action<string> OnAnnouncement;    // "Jugador: ¡DOBLE KILL!", "¡PRIMERA SANGRE!"...

    // Rachas de kills
    readonly Dictionary<Unit, int> killStreak = new Dictionary<Unit, int>();
    readonly Dictionary<Unit, int> multiKill = new Dictionary<Unit, int>();
    readonly Dictionary<Unit, float> lastKillTime = new Dictionary<Unit, float>();
    bool firstBlood;
    public float multiKillWindow = 8f;

    readonly int[] kills = new int[2];
    readonly int[] roundsWon = new int[2];
    readonly HashSet<Unit> registered = new HashSet<Unit>();
    int roundToken;
    int appliedDifficulty = -1;
    readonly Dictionary<Unit, float> respawnAt = new Dictionary<Unit, float>();
    float goldAccumulator;
    int pendingSpawns;
    int enemyCounter;
    float nextSpawnTry;

    // El HUD lee esto para mostrar "Reapareces en X" (Time.time en que reaparece cada unidad)
    public IReadOnlyDictionary<Unit, float> RespawnTimes => respawnAt;

    void Update()
    {
        if (!IsPlaying) return;

        // Supervivencia: enemigos extra que entran con cada oleada
        if (pendingSpawns > 0 && Time.time >= nextSpawnTry)
        {
            nextSpawnTry = Time.time + 0.5f;
            if (SpawnEnemyClone()) pendingSpawns--;
        }

        // Oro por segundo para todos (también para los bots, que lo gastan al final de la ronda)
        if (goldPerSecond <= 0f) return;
        goldAccumulator += goldPerSecond * Time.deltaTime;
        if (goldAccumulator < 1f) return;

        int gold = (int)goldAccumulator;
        goldAccumulator -= gold;
        foreach (var u in Unit.All)
            if (u != null && u.Wallet != null) u.Wallet.Add(gold);
    }

    public int GetKills(TeamId t) => kills[(int)t];
    public int GetRoundsWon(TeamId t) => roundsWon[(int)t];

    // Dificultad elegida en el menú principal (se guarda en PlayerPrefs)
    public const string DifficultyKey = "dificultad";
    public static readonly string[] DifficultyNames = { "Fácil", "Normal", "Difícil" };
    static readonly int[] DifficultyOffsets = { -2, 0, 2 };
    public static int MenuDifficulty => Mathf.Clamp(PlayerPrefs.GetInt(DifficultyKey, 1), 0, 2);

    // Dificultad actual de los bots enemigos
    public int Difficulty
    {
        get
        {
            if (IsSurvival) return Mathf.Clamp(Wave + MenuDifficulty, 1, 6); // Sube con cada oleada

            int d = baseDifficulty + DifficultyOffsets[MenuDifficulty] + (increasePerRound ? Mathf.Max(0, Round - 1) : 0);
            int playerLead = kills[(int)playerTeam] - kills[(int)Other(playerTeam)];
            if (playerLead >= leadForExtraDifficulty && !IsVersus) d++;
            return Mathf.Clamp(d, 1, 6);
        }
    }

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        Lives = survivalLives;
        MatchStats.Reset();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        if (IsVersus) SetupSecondPlayer();
        ApplyCharacter();

        foreach (var u in Unit.All.ToArray()) Register(u);
        StartCoroutine(StartRoundRoutine());
    }

    void Register(Unit unit)
    {
        if (unit == null || !registered.Add(unit)) return;
        unit.Health.OnDied += (h, killer) => HandleDeath(unit, killer);
    }

    // Modo 2 Jugadores: un bot rojo pasa a ser controlado por el segundo jugador
    void SetupSecondPlayer()
    {
        foreach (var u in Unit.All)
        {
            if (u == null || u.team == playerTeam) continue;
            BotController bot = u.GetComponent<BotController>();
            if (bot == null) continue;

            NavMeshAgent agent = u.GetComponent<NavMeshAgent>();
            CapsuleCollider capsule = u.GetComponent<CapsuleCollider>();
            DestroyImmediate(bot);
            if (agent != null) DestroyImmediate(agent);
            if (capsule != null) DestroyImmediate(capsule);

            var cc = u.gameObject.AddComponent<CharacterController>();
            cc.center = Vector3.zero;
            cc.height = 2f;
            cc.radius = 0.5f;
            var pc = u.gameObject.AddComponent<PlayerController>();
            pc.playerIndex = 1;

            var hook = u.GetComponent<HookAbility>(); if (hook != null) hook.usePlayerInput = true;
            var net = u.GetComponent<NetAbility>(); if (net != null) net.usePlayerInput = true;
            var dash = u.GetComponent<DashAbility>(); if (dash != null) dash.usePlayerInput = true;
            var shield = u.GetComponent<ShieldAbility>(); if (shield != null) shield.usePlayerInput = true;

            u.displayName = "Jugador 2";
            u.Status.Refresh();
            return;
        }
    }

    // Aplica al jugador 1 el personaje elegido en el menú (vida, velocidad, garfio y modelo)
    void ApplyCharacter()
    {
        Unit player = Unit.FindPlayer();
        CharacterInfo info = GameSession.CurrentCharacter;
        if (player == null || GameSession.Character == 0) return;

        player.Health.AddMaxHealth(info.health - player.Health.Max);
        var pc = player.GetComponent<PlayerController>();
        if (pc != null) pc.moveSpeed = info.speed;
        var hook = player.GetComponent<HookAbility>();
        if (hook != null) { hook.damage = info.hookDamage; hook.cooldown = info.hookCooldown; }

        GameObject prefab = string.IsNullOrEmpty(info.model) ? null : Resources.Load<GameObject>("Personajes/" + info.model);
        if (prefab == null) return;

        Transform old = player.transform.Find("Modelo");
        if (old != null)
        {
            old.gameObject.SetActive(false);
            Destroy(old.gameObject);
        }
        GameObject model = Instantiate(prefab, player.transform);
        model.name = "Modelo";
        model.transform.localPosition = new Vector3(0f, -1f, 0f);
        model.transform.localRotation = Quaternion.identity;
        foreach (Transform t in model.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = player.gameObject.layer;

        var anim = player.GetComponent<CharacterAnimator>();
        if (anim != null) anim.Rebind();
        player.Status.Refresh();
    }

    IEnumerator StartRoundRoutine()
    {
        yield return null; // Esperar un frame para que la UI se suscriba a los eventos

        roundToken++;
        Round++;
        kills[0] = kills[1] = 0;

        StopEverything();
        killStreak.Clear();
        multiKill.Clear();
        lastKillTime.Clear();
        firstBlood = false;
        foreach (var u in Unit.All)
            if (u != null) u.ResetToSpawn();

        ApplyDifficulty();
        OnScoreChanged?.Invoke();
        State = GameState.Cuenta;

        for (int i = countdownSeconds; i > 0; i--)
        {
            OnCountdown?.Invoke(i);
            yield return new WaitForSeconds(1f);
        }

        OnCountdown?.Invoke(0);
        State = GameState.Jugando;
        OnRoundStarted?.Invoke(Round);
        AudioManager.Play(Sfx.InicioRonda);
    }

    void HandleDeath(Unit victim, GameObject killerObj)
    {
        Unit killer = killerObj != null ? killerObj.GetComponent<Unit>() : null;
        bool validKill = killer != null && killer.IsEnemyOf(victim);
        TeamId scoringTeam = validKill ? killer.team : Other(victim.team);

        if (State == GameState.Jugando)
        {
            kills[(int)scoringTeam]++;
            if (validKill && killer.Wallet != null)
            {
                killer.Wallet.Add(goldPerKill);
                if (killer.isPlayer) AudioManager.Play(Sfx.Moneda);
            }
            if (validKill) AnnounceKill(killer);
            if (validKill && killer.isPlayer) MatchStats.Kills++;
            if (victim.isPlayer) MatchStats.Deaths++;
        }
        killStreak[victim] = 0;
        if (victim.isPlayer) CameraFollow.Shake(0.5f, 0.3f);
        else if (killer != null && killer.isPlayer) CameraFollow.Shake(0.3f, 0.2f);

        ApplyDifficulty();
        OnKill?.Invoke(killer, victim);
        AudioManager.Play(Sfx.Muerte);
        if (deathEffectPrefab != null)
            Instantiate(deathEffectPrefab, victim.transform.position, Quaternion.identity);

        victim.gameObject.SetActive(false);

        if (IsSurvival)
        {
            if (State == GameState.Jugando) HandleSurvivalDeath(victim);
            OnScoreChanged?.Invoke();
            if (State == GameState.FinPartida) return;
        }
        else
        {
            OnScoreChanged?.Invoke();
            if (State == GameState.Jugando && kills[(int)scoringTeam] >= killsToWinRound)
            {
                EndRound(scoringTeam);
                return;
            }
        }

        float delay = RespawnDelayFor(victim);
        respawnAt[victim] = Time.time + delay;
        StartCoroutine(RespawnRoutine(victim, roundToken, delay));
    }

    float RespawnDelayFor(Unit unit)
    {
        if (!IsSurvival) return respawnDelay;
        if (unit.team == playerTeam) return 3f;
        return Mathf.Max(1.5f, 4f - 0.4f * Wave); // Los enemigos vuelven cada vez más rápido
    }

    // ---------------------------------------------------------------------------------------------
    // Supervivencia
    // ---------------------------------------------------------------------------------------------

    void HandleSurvivalDeath(Unit victim)
    {
        if (victim.team != playerTeam)
        {
            WaveKills++;
            if (WaveKills >= WaveGoal) NextWave();
        }
        else if (victim.isPlayer)
        {
            Lives--;
            if (Lives <= 0) EndSurvival();
            else Announce(Lives == 1 ? "¡Te queda 1 vida!" : $"Te quedan {Lives} vidas");
        }
    }

    void NextWave()
    {
        Wave++;
        WaveKills = 0;

        int enemies = 0;
        foreach (var u in Unit.All)
            if (u != null && u.team != playerTeam) enemies++;
        int wanted = Mathf.Min(2 + Wave / 2, maxSurvivalEnemies);
        pendingSpawns = Mathf.Max(0, wanted - enemies);

        foreach (var u in Unit.All)
            if (u != null && u.team == playerTeam && u.Wallet != null) u.Wallet.Add(goldPerRound);

        ApplyDifficulty();
        Announce($"¡OLEADA {Wave}!");
        AudioManager.Play(Sfx.InicioRonda);
    }

    // Copia a un enemigo vivo para sumar uno más a la oleada
    bool SpawnEnemyClone()
    {
        Unit source = null;
        foreach (var u in Unit.All)
        {
            if (u == null || u.team == playerTeam || !u.IsAlive || !u.Status.CanMove) continue;
            if (u.GetComponent<BotController>() == null) continue;
            source = u;
            break;
        }
        if (source == null) return false;

        GameObject go = Instantiate(source.gameObject);
        Unit unit = go.GetComponent<Unit>();
        enemyCounter++;
        unit.displayName = "Refuerzo " + enemyCounter;
        go.name = unit.displayName;
        Register(unit);

        unit.Revive(RandomRespawnPoint(unit, out Quaternion rot), rot);
        BotController bot = go.GetComponent<BotController>();
        if (bot != null) bot.ApplyDifficulty(Difficulty);
        return true;
    }

    void EndSurvival()
    {
        State = GameState.FinPartida;
        StopEverything();

        if (Wave > 1 && Wave > SurvivalRecord) // Llegar solo a la oleada 1 no cuenta como récord
        {
            NewRecord = true;
            PlayerPrefs.SetInt(RecordKey, Wave);
            PlayerPrefs.Save();
        }
        OnGameOver?.Invoke(NewRecord);
        AudioManager.Play(NewRecord ? Sfx.Victoria : Sfx.Derrota);
    }

    // ---------------------------------------------------------------------------------------------

    public void Announce(string message) => OnAnnouncement?.Invoke(message);

    void AnnounceKill(Unit killer)
    {
        int streak = (killStreak.TryGetValue(killer, out int s) ? s : 0) + 1;
        killStreak[killer] = streak;

        float last = lastKillTime.TryGetValue(killer, out float t) ? t : -999f;
        int multi = Time.time - last <= multiKillWindow ? (multiKill.TryGetValue(killer, out int m) ? m : 1) + 1 : 1;
        multiKill[killer] = multi;
        lastKillTime[killer] = Time.time;

        string message = null;
        if (!firstBlood) { firstBlood = true; message = "¡PRIMERA SANGRE!"; }
        if (multi == 2) message = "¡DOBLE KILL!";
        else if (multi == 3) message = "¡TRIPLE KILL!";
        else if (multi >= 4) message = "¡MASACRE!";
        else if (streak == 3) message = "¡EN RACHA!";
        else if (streak == 5) message = "¡IMPARABLE!";
        else if (streak == 7) message = "¡LEYENDA!";

        if (message != null) OnAnnouncement?.Invoke($"{killer.displayName}\n{message}");
    }

    IEnumerator RespawnRoutine(Unit unit, int token, float delay)
    {
        yield return new WaitForSeconds(delay);
        respawnAt.Remove(unit);
        if (unit == null || token != roundToken || State == GameState.FinPartida) yield break;

        if (randomRespawn) unit.Revive(RandomRespawnPoint(unit, out Quaternion rot), rot);
        else unit.ResetToSpawn();
    }

    // Punto al azar del lado del río de esa unidad, mirando hacia el río
    Vector3 RandomRespawnPoint(Unit unit, out Quaternion rotation)
    {
        float side = unit.SpawnPosition.z >= 0f ? 1f : -1f;
        rotation = Quaternion.LookRotation(new Vector3(0f, 0f, -side));

        for (int i = 0; i < 20; i++)
        {
            Vector3 p = new Vector3(
                UnityEngine.Random.Range(-respawnHalfWidth, respawnHalfWidth),
                unit.SpawnPosition.y,
                side * UnityEngine.Random.Range(respawnMinDistance, respawnMaxDistance));

            if (NavMesh.SamplePosition(p, out NavMeshHit hit, 1.5f, NavMesh.AllAreas) && hit.position.z * side > 0f)
                return new Vector3(hit.position.x, unit.SpawnPosition.y, hit.position.z);
        }
        return unit.SpawnPosition;
    }

    // Al terminar una ronda o la partida se limpia todo: garfios, redes y runas en el aire
    void StopEverything()
    {
        foreach (var h in HookProjectile.Active.ToArray())
            if (h != null) Destroy(h.gameObject);
        foreach (var n in NetProjectile.Active.ToArray())
            if (n != null) Destroy(n.gameObject);
        foreach (var r in Rune.Active.ToArray())
            if (r != null) Destroy(r.gameObject);

        foreach (var u in Unit.All)
            if (u != null && u.IsAlive) u.Status.ClearAll();

        roundToken++;        // Cancela las reapariciones pendientes
        respawnAt.Clear();
    }

    void EndRound(TeamId winner)
    {
        State = GameState.FinRonda;
        StopEverything();
        roundsWon[(int)winner]++;

        foreach (var u in Unit.All)
        {
            if (u == null || u.Wallet == null) continue;
            u.Wallet.Add(goldPerRound);
            BotController bot = u.GetComponent<BotController>();
            if (bot != null && u.team != playerTeam) bot.BuyUpgrades(botItems);
        }

        OnRoundEnded?.Invoke(winner);
        OnScoreChanged?.Invoke();

        if (roundsWon[(int)winner] >= roundsToWin)
        {
            State = GameState.FinPartida;
            bool playerWon = winner == playerTeam;
            OnGameOver?.Invoke(playerWon);
            AudioManager.Play(playerWon || IsVersus ? Sfx.Victoria : Sfx.Derrota);
        }
        else
        {
            StartCoroutine(NextRoundRoutine());
        }
    }

    IEnumerator NextRoundRoutine()
    {
        yield return new WaitForSeconds(3f);
        yield return StartCoroutine(StartRoundRoutine());
    }

    // Los enemigos usan la dificultad actual; el bot aliado va un nivel más abajo
    void ApplyDifficulty()
    {
        int d = Difficulty;
        if (d == appliedDifficulty) return;
        appliedDifficulty = d;

        foreach (var u in Unit.All)
        {
            if (u == null) continue;
            BotController bot = u.GetComponent<BotController>();
            if (bot == null) continue;
            bool ally = u.team == playerTeam && !IsVersus;
            bot.ApplyDifficulty(ally ? Mathf.Max(1, d - 1) : d);
        }

        OnDifficultyChanged?.Invoke(d);
    }

    public void SetPaused(bool paused)
    {
        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
    }

    static TeamId Other(TeamId t) => t == TeamId.Azul ? TeamId.Rojo : TeamId.Azul;
}
