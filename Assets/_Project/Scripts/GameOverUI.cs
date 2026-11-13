using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Pantalla final: victoria o derrota, resultado del modo y estadísticas del jugador.
public class GameOverUI : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text detailText;
    [SerializeField] string menuSceneName = "MainMenu";

    void Start()
    {
        panel.SetActive(false);
        if (GameManager.Instance != null) GameManager.Instance.OnGameOver += Show;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnGameOver -= Show;
    }

    void Show(bool playerWon)
    {
        panel.SetActive(true);
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        string title, result;
        Color color = playerWon ? new Color(1f, 0.85f, 0.2f) : new Color(1f, 0.3f, 0.3f);

        if (gm.IsMemory)
        {
            title = gm.NewRecord ? "¡NUEVO RÉCORD!" : "FIN DE LA MEMORIA";
            color = gm.NewRecord ? new Color(1f, 0.85f, 0.2f) : new Color(0.31f, 0.64f, 1f);
            result = $"Secuencia más larga recordada: <b>{MatchStats.MemorySpan}</b> runas   ·   Récord: {MemoryMode.Record} runas";
        }
        else if (gm.IsSurvival)
        {
            title = gm.NewRecord ? "¡NUEVO RÉCORD!" : "FIN DE LA SUPERVIVENCIA";
            result = $"Llegaste a la oleada {gm.Wave}   ·   Récord: oleada {GameManager.SurvivalRecord}";
        }
        else if (gm.IsVersus)
        {
            title = playerWon ? "¡GANA EL JUGADOR 1!" : "¡GANA EL JUGADOR 2!";
            color = playerWon ? new Color(0.31f, 0.64f, 1f) : new Color(1f, 0.35f, 0.35f);
            result = $"Rondas: Azul {gm.GetRoundsWon(TeamId.Azul)} - {gm.GetRoundsWon(TeamId.Rojo)} Rojo";
        }
        else
        {
            title = playerWon ? "¡VICTORIA!" : "DERROTA";
            result = $"Rondas: Azul {gm.GetRoundsWon(TeamId.Azul)} - {gm.GetRoundsWon(TeamId.Rojo)} Rojo";
        }

        Unit player = Unit.FindPlayer();
        int gold = player != null && player.Wallet != null ? player.Wallet.TotalEarned : 0;
        string stats = gm.IsMemory
            ? $"<size=80%><color=#C9D3E6>Tus estadísticas</color>\n" +
              $"Runas acertadas: <b>{MatchStats.MemoryHits}</b>   ·   Errores: <b>{MatchStats.MemoryErrors}</b>   ·   Precisión: <b>{MatchStats.MemoryAccuracy}%</b>\n" +
              $"<color=#9AA6BD>Al final viste otra vez la secuencia correcta para reforzarla.</color></size>"
            :
            $"<size=80%><color=#C9D3E6>Tus estadísticas</color>\n" +
            $"Kills: <b>{MatchStats.Kills}</b>   ·   Muertes: <b>{MatchStats.Deaths}</b>\n" +
            $"Garfios acertados: <b>{MatchStats.HooksHit}/{MatchStats.HooksFired}</b> ({MatchStats.Accuracy}% de precisión)\n" +
            $"Garfios reflejados: <b>{MatchStats.Reflects}</b>   ·   Runas: <b>{MatchStats.Runes}</b>   ·   Oro ganado: <b>{gold}</b></size>";

        if (titleText != null)
        {
            titleText.text = title;
            titleText.color = color;
        }
        if (detailText != null) detailText.text = result + "\n" + stats;

        Layout();
    }

    // Acomoda el título, el detalle y los botones para que entren las estadísticas
    void Layout()
    {
        if (titleText != null)
        {
            titleText.rectTransform.anchoredPosition = new Vector2(0f, 300f);
            titleText.textWrappingMode = TextWrappingModes.NoWrap;
            titleText.enableAutoSizing = true;
            titleText.fontSizeMin = 60;
            titleText.fontSizeMax = 140;
        }
        if (detailText != null)
        {
            detailText.rectTransform.anchoredPosition = new Vector2(0f, 70f);
            detailText.rectTransform.sizeDelta = new Vector2(1500f, 260f);
            detailText.fontSize = 38;
            detailText.alignment = TextAlignmentOptions.Center;
            detailText.textWrappingMode = TextWrappingModes.Normal;
        }

        float y = -130f;
        foreach (Button b in panel.GetComponentsInChildren<Button>(true))
        {
            ((RectTransform)b.transform).anchoredPosition = new Vector2(0f, y);
            y -= 100f;
        }
    }

    public void Retry()
    {
        AudioManager.Play(Sfx.Click);
        SceneLoader.ReloadCurrent();
    }

    public void GoToMenu()
    {
        AudioManager.Play(Sfx.Click);
        SceneLoader.Load(menuSceneName);
    }
}
