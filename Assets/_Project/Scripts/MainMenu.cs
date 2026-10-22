using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Menú principal: Jugar (elegir arena y dificultad), Controles, volumen y Salir.
public class MainMenu : MonoBehaviour
{
    [SerializeField] GameObject mainPanel;
    [SerializeField] GameObject levelPanel;
    [SerializeField] GameObject controlsPanel;
    [SerializeField] Slider volumeSlider;
    [SerializeField] TMP_Text difficultyText;

    void Start()
    {
        Time.timeScale = 1f;
        SetPanels(true, false, false, false);
        UpdateDifficultyText();

        if (volumeSlider != null)
        {
            volumeSlider.value = PlayerPrefs.GetFloat("volumen", 0.8f);
            volumeSlider.onValueChanged.AddListener(AudioManager.SetVolume);
        }
    }

    public void ShowMain() { SetPanels(true, false, false, true); }
    public void ShowLevels() { SetPanels(false, true, false, true); }
    public void ShowControls() { SetPanels(false, false, true, true); }

    // En el botón: OnClick > MainMenu.PlayLevel y escribe el nombre de la escena (Arena01 o Arena02)
    public void PlayLevel(string sceneName)
    {
        AudioManager.Play(Sfx.Click);
        SceneLoader.Load(sceneName);
    }

    // 0 = Fácil, 1 = Normal, 2 = Difícil (el GameManager lo lee al jugar)
    public void SetDifficulty(int level)
    {
        PlayerPrefs.SetInt(GameManager.DifficultyKey, Mathf.Clamp(level, 0, 2));
        PlayerPrefs.Save();
        AudioManager.Play(Sfx.Click);
        UpdateDifficultyText();
    }

    public void Quit()
    {
        SceneLoader.Quit();
    }

    void UpdateDifficultyText()
    {
        if (difficultyText != null)
            difficultyText.text = "Dificultad: " + GameManager.DifficultyNames[GameManager.MenuDifficulty];
    }

    void SetPanels(bool main, bool levels, bool controls, bool click)
    {
        if (click) AudioManager.Play(Sfx.Click);
        if (mainPanel != null) mainPanel.SetActive(main);
        if (levelPanel != null) levelPanel.SetActive(levels);
        if (controlsPanel != null) controlsPanel.SetActive(controls);
    }
}
