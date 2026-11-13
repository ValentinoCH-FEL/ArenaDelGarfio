using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Modo Memoria · Runas del Ancla.
// 6 runas flotan en el río. Se encienden en un orden (fase MEMORIZA) y, tras una pausa de retención,
// el jugador debe engancharlas con el garfio en el mismo orden (fase REPITE).
// Si acierta toda la secuencia, la siguiente tiene una runa más (como el test de bloques de Corsi o Simon).
// Si se equivoca pierde una vida y se repite la misma secuencia. Con 0 vidas termina la partida.
public class MemoryMode : MonoBehaviour
{
    public static MemoryMode Instance { get; private set; }

    public enum Phase { Preparando, Memoriza, Retencion, Repite, Fin }

    public const string RecordKey = "record_memoria";
    public static int Record => PlayerPrefs.GetInt(RecordKey, 0);

    public const int RuneCount = 6;
    // Ninguna runa es verde ni roja: esos colores se reservan para el acierto y el error
    static readonly Color[] RuneColors =
    {
        new Color(0.2f, 0.9f, 1f),    // Celeste
        new Color(1f, 0.45f, 0.75f),  // Rosa
        new Color(1f, 0.85f, 0.2f),   // Amarillo
        new Color(0.55f, 0.33f, 0.15f), // Café
        new Color(0.75f, 0.4f, 1f),   // Morado
        new Color(1f, 0.55f, 0.15f),  // Naranja
    };
    public static readonly string[] ColorNames = { "Celeste", "Rosa", "Amarilla", "Cafe", "Morada", "Naranja" };

    // Por dificultad (Fácil, Normal, Difícil): secuencia inicial y segundos que se ve encendida cada runa
    static readonly int[] StartLength = { 2, 3, 3 };
    static readonly float[] ShowSeconds = { 0.9f, 0.7f, 0.5f };

    public int lives = 3;
    public float spacing = 5f;
    public float height = 1.3f;

    public Phase CurrentPhase { get; private set; } = Phase.Preparando;
    public int Level { get; private set; } = 1;
    public int SequenceLength => sequence.Count;
    public int Progress { get; private set; }      // Cuántas runas de la secuencia ya acertó
    public int Lives { get; private set; }
    public int BestSpan { get; private set; }      // Secuencia más larga completada
    public bool NewRecord { get; private set; }

    public event Action OnChanged;

    readonly List<Rune> runes = new List<Rune>();
    readonly List<int> sequence = new List<int>();
    Rune runePrefab;
    GameManager gm;

    public void Setup(Rune prefab)
    {
        Instance = this;
        runePrefab = prefab;
        Lives = lives;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        gm = GameManager.Instance;
        CreateRunes();
        for (int i = 0; i < StartLength[GameManager.MenuDifficulty]; i++) AddStep();
        if (gm != null) gm.OnRoundStarted += _ => StartCoroutine(ShowSequence());
    }

    void CreateRunes()
    {
        if (runePrefab == null) return;
        float start = -spacing * (RuneCount - 1) / 2f;
        for (int i = 0; i < RuneCount; i++)
        {
            Rune r = Instantiate(runePrefab, new Vector3(start + i * spacing, height, 0f), Quaternion.identity);
            r.name = "RunaMemoria_" + ColorNames[i];
            r.Setup(RuneType.Oro);
            r.SetColor(RuneColors[i]);
            r.transform.localScale *= 1.3f;
            runes.Add(r);
        }
    }

    // Nunca la misma runa dos veces seguidas, para que cada paso se distinga
    void AddStep()
    {
        int next;
        do next = UnityEngine.Random.Range(0, RuneCount);
        while (sequence.Count > 0 && next == sequence[sequence.Count - 1]);
        sequence.Add(next);
    }

    // Fase MEMORIZA: se encienden las runas en orden. Luego una pausa de retención que crece con el nivel.
    IEnumerator ShowSequence()
    {
        Progress = 0;
        CurrentPhase = Phase.Memoriza;
        Changed();
        Announce($"MEMORIZA\n<size=70%>{sequence.Count} runas</size>");
        yield return new WaitForSeconds(1.6f);

        float show = ShowSeconds[GameManager.MenuDifficulty];
        foreach (int index in sequence)
        {
            AudioManager.Play(Sfx.Click);
            yield return Flash(runes[index], Color.white, show, 1.6f);
            yield return new WaitForSeconds(show * 0.4f);
        }

        CurrentPhase = Phase.Retencion;
        Changed();
        yield return new WaitForSeconds(Mathf.Min(0.5f + 0.25f * (Level - 1), 3f));

        CurrentPhase = Phase.Repite;
        Changed();
        Announce("¡TU TURNO!\n<size=70%>Engancha las runas en el mismo orden</size>");
    }

    // El garfio tocó una runa (lo llama HookProjectile)
    public void OnRuneHooked(Rune rune)
    {
        int index = runes.IndexOf(rune);
        if (index < 0 || CurrentPhase != Phase.Repite) return;

        if (index == sequence[Progress])
        {
            Progress++;
            MatchStats.MemoryHits++;
            AudioManager.Play(Sfx.Moneda);
            StartCoroutine(Flash(rune, Color.green, 0.35f, 1.4f));
            Changed();
            if (Progress >= sequence.Count) StartCoroutine(LevelComplete());
        }
        else
        {
            MatchStats.MemoryErrors++;
            Lives--;
            AudioManager.Play(Sfx.GarfioPared);
            StartCoroutine(Flash(rune, Color.red, 0.5f, 1.4f));
            CameraFollow.Shake(0.3f, 0.2f);
            Changed();
            if (Lives <= 0) StartCoroutine(GameOver());
            else StartCoroutine(Retry());
        }
    }

    IEnumerator LevelComplete()
    {
        CurrentPhase = Phase.Preparando;
        BestSpan = Mathf.Max(BestSpan, sequence.Count);
        Level++;
        AddStep();
        Changed();
        AudioManager.Play(Sfx.InicioRonda);
        Announce($"¡CORRECTO!\n<size=70%>Nivel {Level}: ahora son {sequence.Count} runas</size>");
        yield return new WaitForSeconds(2f);
        yield return ShowSequence();
    }

    IEnumerator Retry()
    {
        CurrentPhase = Phase.Preparando;
        Announce(Lives == 1 ? "¡Error! Te queda 1 vida\n<size=70%>Mira otra vez la secuencia</size>"
                            : $"¡Error! Te quedan {Lives} vidas\n<size=70%>Mira otra vez la secuencia</size>");
        yield return new WaitForSeconds(2f);
        yield return ShowSequence();
    }

    IEnumerator GameOver()
    {
        CurrentPhase = Phase.Fin;
        MatchStats.MemorySpan = BestSpan;
        if (BestSpan > Record)
        {
            NewRecord = true;
            PlayerPrefs.SetInt(RecordKey, BestSpan);
            PlayerPrefs.Save();
        }
        Changed();
        // Al final se muestra la secuencia correcta, para reforzarla
        yield return new WaitForSeconds(0.6f);
        foreach (int index in sequence) yield return Flash(runes[index], Color.white, 0.35f, 1.5f);
        if (gm != null) gm.EndMemory(NewRecord);
    }

    // Enciende una runa: la agranda y la pinta de un color por un momento
    IEnumerator Flash(Rune rune, Color color, float seconds, float scale)
    {
        if (rune == null) yield break;
        Vector3 baseScale = rune.transform.localScale;
        rune.SetHighlight(color, true);
        rune.transform.localScale = baseScale * scale;
        yield return new WaitForSeconds(seconds);
        if (rune == null) yield break;
        rune.transform.localScale = baseScale;
        rune.SetHighlight(color, false);
    }

    // Índice de la runa que toca enganchar ahora (lo usa la prueba automática)
    public Rune ExpectedRune => CurrentPhase == Phase.Repite && Progress < sequence.Count ? runes[sequence[Progress]] : null;
    public Rune WrongRune => ExpectedRune == null ? null : runes[(sequence[Progress] + 1) % RuneCount];

    void Announce(string msg) { if (gm != null) gm.Announce(msg); }
    void Changed() { OnChanged?.Invoke(); if (gm != null) gm.NotifyScoreChanged(); }
}
