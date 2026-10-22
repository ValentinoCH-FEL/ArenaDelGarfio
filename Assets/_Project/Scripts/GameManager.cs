using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Reglas de la partida: cuenta regresiva, kills, rondas, respawn, oro, pausa y fin del juego.
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
    [Range(1, 6)] public int baseDifficulty = 3;   // Arena01: 3 · Arena02: 4
    public bool increasePerRound = true;            // +1 por cada ronda nueva
    public int leadForExtraDifficulty = 2;          // Si el jugador saca esta ventaja en kills, +1
    public List<ItemData> botItems = new List<ItemData>(); // Mejoras que los bots compran al final de cada ronda

    [Header("Efectos")]
    public GameObject deathEffectPrefab;

    public GameState State { get; private set; }
    public bool IsPaused { get; private set; }
    public bool IsPlaying => State == GameState.Jugando && !IsPaused;
    public int Round { get; private set; }

    public event Action OnScoreChanged;
    public event Action<int> OnCountdown;      // 3, 2, 1 y 0 (0 = ¡YA!)
    public event Action<int> OnRoundStarted;
    public event Action<TeamId> OnRoundEnded;  // equipo ganador
    public event Action<bool> OnGameOver;      // true = ganó el jugador
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
    int roundToken;
    int appliedDifficulty = -1;
    readonly Dictionary<Unit, float> respawnAt = new Dictionary<Unit, float>();
    float goldAccumulator;

    // El HUD lee esto para mostrar "Reapareces en X" (Time.time en que reaparece cada unidad)
    public IReadOnlyDictionary<Unit, float> RespawnTimes => respawnAt;

    // Oro por segundo para todos (también para los bots, que lo gastan al final de la ronda)
    void Update()
    {
        if (!IsPlaying || goldPerSecond <= 0f) return;

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
            int d = baseDifficulty + DifficultyOffsets[MenuDifficulty] + (increasePerRound ? Mathf.Max(0, Round - 1) : 0);
            int playerLead = kills[(int)playerTeam] - kills[(int)Other(playerTeam)];
            if (playerLead >= leadForExtraDifficulty) d++;
            return Mathf.Clamp(d, 1, 6);
        }
    }

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        foreach (var u in Unit.All)
        {
            Unit unit = u;
            unit.Health.OnDied += (h, killer) => HandleDeath(unit, killer);
        }
        StartCoroutine(StartRoundRoutine());
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
        }
        killStreak[victim] = 0;
        if (victim.isPlayer) CameraFollow.Shake(0.5f, 0.3f);
        else if (killer != null && killer.isPlayer) CameraFollow.Shake(0.3f, 0.2f);

        ApplyDifficulty();
        OnKill?.Invoke(killer, victim);
        OnScoreChanged?.Invoke();
        AudioManager.Play(Sfx.Muerte);
        if (deathEffectPrefab != null)
            Instantiate(deathEffectPrefab, victim.transform.position, Quaternion.identity);

        victim.gameObject.SetActive(false);

        if (State == GameState.Jugando && kills[(int)scoringTeam] >= killsToWinRound)
        {
            EndRound(scoringTeam);
            return;
        }
        respawnAt[victim] = Time.time + respawnDelay;
        StartCoroutine(RespawnRoutine(victim, roundToken));
    }

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

    IEnumerator RespawnRoutine(Unit unit, int token)
    {
        yield return new WaitForSeconds(respawnDelay);
        respawnAt.Remove(unit);
        if (token != roundToken || State == GameState.FinPartida) yield break;

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
            AudioManager.Play(playerWon ? Sfx.Victoria : Sfx.Derrota);
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
            bot.ApplyDifficulty(u.team == playerTeam ? Mathf.Max(1, d - 1) : d);
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
