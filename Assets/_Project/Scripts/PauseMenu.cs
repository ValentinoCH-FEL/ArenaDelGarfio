using UnityEngine;
using UnityEngine.InputSystem;

// Menú de pausa con la tecla Esc.
// IMPORTANTE: pon este script en un objeto que siempre esté activo (no en el panel que se esconde).
public class PauseMenu : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] string menuSceneName = "MainMenu";

    bool open;

    void Start()
    {
        panel.SetActive(false);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (open) Resume();
            else Pause();
        }
    }

    void Pause()
    {
        GameManager gm = GameManager.Instance;
        if (ShopUI.IsOpen) return;
        if (gm != null && gm.State == GameState.FinPartida) return;

        open = true;
        panel.SetActive(true);
        if (gm != null) gm.SetPaused(true);
    }

    public void Resume()
    {
        open = false;
        panel.SetActive(false);
        if (GameManager.Instance != null) GameManager.Instance.SetPaused(false);
        AudioManager.Play(Sfx.Click);
    }

    public void GoToMenu()
    {
        AudioManager.Play(Sfx.Click);
        SceneLoader.Load(menuSceneName);
    }

    public void Quit()
    {
        SceneLoader.Quit();
    }
}
