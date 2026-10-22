using TMPro;
using UnityEngine;

// Pantalla de victoria o derrota al terminar la partida.
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

        if (titleText != null)
        {
            titleText.text = playerWon ? "¡VICTORIA!" : "DERROTA";
            titleText.color = playerWon ? new Color(1f, 0.85f, 0.2f) : new Color(1f, 0.3f, 0.3f);
        }
        if (detailText != null && gm != null)
            detailText.text = $"Rondas: Azul {gm.GetRoundsWon(TeamId.Azul)} - {gm.GetRoundsWon(TeamId.Rojo)} Rojo";
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
