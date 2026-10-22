using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Interfaz durante la partida: vida, oro, recargas, marcador, rondas, mensajes, kills,
// anuncios de rachas y reapariciones. Cada tipo de mensaje tiene su propia zona para que nunca se encimen:
//   arriba al centro: marcador  ·  debajo: anuncios (rachas)  ·  arriba a la derecha: kills y runas
//   arriba a la izquierda: bots que reaparecen  ·  centro: cuenta regresiva y fin de ronda
//   debajo del centro: "Reapareces en X"  ·  abajo: vida, oro y habilidades
public class HUD : MonoBehaviour
{
    [Header("Jugador")]
    [SerializeField] Slider healthBar;
    [SerializeField] TMP_Text healthText;
    [SerializeField] TMP_Text goldText;

    [Header("Recargas (Image tipo Filled)")]
    [SerializeField] Image hookCooldown;
    [SerializeField] Image netCooldown;
    [SerializeField] Image dashCooldown;

    [Header("Partida")]
    [SerializeField] TMP_Text scoreText;
    [SerializeField] TMP_Text roundText;
    [SerializeField] TMP_Text centerText;
    [SerializeField] TMP_Text killFeedText;
    [SerializeField] int killFeedLines = 5;

    [Header("Se crean solos si están vacíos")]
    [SerializeField] TMP_Text announceText;
    [SerializeField] TMP_Text respawnText;
    [SerializeField] TMP_Text botRespawnText;

    static readonly Color Accent = new Color(0.95f, 0.55f, 0.15f);

    Unit player;
    HookAbility hook;
    NetAbility net;
    DashAbility dash;
    GameManager gm;
    readonly Queue<string> feed = new Queue<string>();
    readonly Queue<string> announcements = new Queue<string>();
    float centerHideTime;
    float announceHideTime;
    readonly StringBuilder sb = new StringBuilder();

    void Start()
    {
        player = Unit.FindPlayer();
        gm = GameManager.Instance;

        CreateMissingTexts();

        if (player != null)
        {
            hook = player.GetComponent<HookAbility>();
            net = player.GetComponent<NetAbility>();
            dash = player.GetComponent<DashAbility>();

            player.Health.OnChanged += UpdateHealth;
            UpdateHealth(player.Health.Current, player.Health.Max);
        }

        if (gm != null)
        {
            gm.OnScoreChanged += UpdateScore;
            gm.OnCountdown += c => ShowCenter(c > 0 ? c.ToString() : "¡YA!", 0.8f);
            gm.OnRoundEnded += t => ShowCenter(t == gm.playerTeam ? "¡Ganaste la ronda!" : "Perdiste la ronda", 2.5f);
            gm.OnGameOver += _ => ClearMessages();
            gm.OnKill += AddKill;
            gm.OnAnnouncement += msg => announcements.Enqueue(msg);
            UpdateScore();
        }

        RuneSpawner.OnRuneSpawned += OnRuneSpawned;
        RuneSpawner.OnRuneTaken += OnRuneTaken;

        ClearMessages();
    }

    void OnDestroy()
    {
        RuneSpawner.OnRuneSpawned -= OnRuneSpawned;
        RuneSpawner.OnRuneTaken -= OnRuneTaken;
    }

    void Update()
    {
        if (hookCooldown != null && hook != null) hookCooldown.fillAmount = hook.CooldownPercent;
        if (netCooldown != null && net != null) netCooldown.fillAmount = net.CooldownPercent;
        if (dashCooldown != null && dash != null) dashCooldown.fillAmount = dash.CooldownPercent;

        if (centerText != null && centerText.text != "" && Time.unscaledTime >= centerHideTime)
            centerText.text = "";

        UpdateAnnouncements();
        UpdateRespawnTexts();
        UpdateGoldAndBuffs();
    }

    // Los anuncios salen de a uno (cola), nunca dos a la vez
    void UpdateAnnouncements()
    {
        if (announceText == null) return;
        if (Time.unscaledTime < announceHideTime) return;

        if (announcements.Count > 0)
        {
            announceText.text = announcements.Dequeue();
            announceHideTime = Time.unscaledTime + 1.8f;
        }
        else if (announceText.text != "")
        {
            announceText.text = "";
        }
    }

    void UpdateRespawnTexts()
    {
        if (gm == null || respawnText == null || botRespawnText == null) return;

        string playerLine = "";
        sb.Clear();
        if (gm.showRespawnTimers && gm.State != GameState.FinPartida)
        {
            foreach (var pair in gm.RespawnTimes)
            {
                if (pair.Key == null) continue;
                int left = Mathf.CeilToInt(pair.Value - Time.time);
                if (left <= 0) continue;

                if (pair.Key.isPlayer) playerLine = $"¡TE ELIMINARON!\n<size=70%>Reapareces en {left}</size>";
                else sb.AppendLine($"{pair.Key.displayName} reaparece en {left}s");
            }
        }

        if (respawnText.text != playerLine) respawnText.text = playerLine;
        string bots = sb.ToString();
        if (botRespawnText.text != bots) botRespawnText.text = bots;
    }

    // Oro y poderes de runa activos
    void UpdateGoldAndBuffs()
    {
        if (goldText == null || player == null || player.Wallet == null) return;
        string buffs = "";
        if (player.Status.HasHaste) buffs += "  <color=#33E6FF>VELOCIDAD</color>";
        if (player.Status.HasDoubleDamage) buffs += "  <color=#FF4040>DOBLE DAÑO</color>";
        goldText.text = player.Wallet.Gold + (buffs.Length > 0 ? "<size=60%>" + buffs + "</size>" : "");
    }

    void UpdateHealth(float current, float max)
    {
        if (healthBar != null)
        {
            healthBar.maxValue = max;
            healthBar.value = current;
        }
        if (healthText != null) healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }

    void UpdateScore()
    {
        if (gm == null) return;
        if (scoreText != null)
            scoreText.text = $"<color=#4FA3FF>AZUL {gm.GetKills(TeamId.Azul)}</color>   -   " +
                             $"<color=#FF5A5A>{gm.GetKills(TeamId.Rojo)} ROJO</color>";
        if (roundText != null)
            roundText.text = $"Ronda {gm.Round}  ·  Rondas {gm.GetRoundsWon(TeamId.Azul)}-{gm.GetRoundsWon(TeamId.Rojo)}" +
                             $"  ·  Meta {gm.killsToWinRound} kills  ·  {GameManager.DifficultyNames[GameManager.MenuDifficulty]} (IA {gm.Difficulty}/6)";
    }

    void OnRuneSpawned(Rune rune) => AddFeedLine($"<color=#FFD24A>¡Runa de {rune.DisplayName} en el río!</color>");
    void OnRuneTaken(Unit unit, Rune rune) => AddFeedLine($"<color=#FFD24A>{unit.displayName} tomó: {rune.DisplayName}</color>");

    void AddKill(Unit killer, Unit victim)
    {
        string line = killer != null
            ? $"{killer.displayName} eliminó a {victim.displayName}"
            : $"{victim.displayName} murió";
        AddFeedLine(line);
    }

    void AddFeedLine(string line)
    {
        feed.Enqueue(line);
        while (feed.Count > killFeedLines) feed.Dequeue();
        if (killFeedText != null) killFeedText.text = string.Join("\n", feed);
    }

    void ShowCenter(string message, float seconds)
    {
        if (centerText == null) return;
        centerText.text = message;
        centerHideTime = Time.unscaledTime + seconds;
    }

    void ClearMessages()
    {
        announcements.Clear();
        if (centerText != null) centerText.text = "";
        if (announceText != null) announceText.text = "";
        if (respawnText != null) respawnText.text = "";
        if (botRespawnText != null) botRespawnText.text = "";
    }

    // Ordena las zonas de texto y crea las que falten
    void CreateMissingTexts()
    {
        Transform parent = transform.Find("HUD") != null ? transform.Find("HUD") : transform;
        TMP_FontAsset font = centerText != null ? centerText.font : null;

        if (killFeedText != null)
        {
            Place(killFeedText.rectTransform, new Vector2(1, 1), new Vector2(-24, -20), new Vector2(460, 190));
            killFeedText.fontSize = 22;
            killFeedText.alignment = TextAlignmentOptions.TopRight;
            killFeedText.textWrappingMode = TextWrappingModes.Normal;
            killFeedText.overflowMode = TextOverflowModes.Truncate;
        }
        if (centerText != null)
        {
            Place(centerText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 140), new Vector2(1400, 160));
            centerText.textWrappingMode = TextWrappingModes.NoWrap;
            centerText.enableAutoSizing = true;
            centerText.fontSizeMin = 40;
            centerText.fontSizeMax = 110;
        }

        if (announceText == null)
            announceText = MakeText("Anuncios", parent, font, new Vector2(0.5f, 1), new Vector2(0, -135), new Vector2(1100, 110), 46, TextAlignmentOptions.Top, Accent);
        if (respawnText == null)
            respawnText = MakeText("Reaparicion", parent, font, new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(1000, 130), 52, TextAlignmentOptions.Center, new Color(1f, 0.35f, 0.3f));
        if (botRespawnText == null)
            botRespawnText = MakeText("ReaparicionBots", parent, font, new Vector2(0, 1), new Vector2(24, -20), new Vector2(460, 150), 22, TextAlignmentOptions.TopLeft, Color.white);
    }

    static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static TMP_Text MakeText(string name, Transform parent, TMP_FontAsset font, Vector2 anchor, Vector2 pos, Vector2 size,
                             float fontSize, TextAlignmentOptions align, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.fontSize = fontSize;
        t.fontStyle = FontStyles.Bold;
        t.alignment = align;
        t.color = color;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.overflowMode = TextOverflowModes.Overflow;
        Place((RectTransform)go.transform, anchor, pos, size);
        return t;
    }
}
