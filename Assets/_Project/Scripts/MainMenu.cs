using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Menú principal: intro animada, Jugar, Controles, volumen y Salir.
// "Jugar" es una pasarela de 4 pasos: modo de juego, personaje, dificultad y mapa.
// Los botones nuevos se crean al iniciar, copiando el estilo de los que ya existen en la escena.
public class MainMenu : MonoBehaviour
{
    [SerializeField] GameObject mainPanel;
    [SerializeField] GameObject levelPanel;
    [SerializeField] GameObject controlsPanel;
    [SerializeField] Slider volumeSlider;
    [SerializeField] TMP_Text difficultyText;

    const string ControlsText =
        "<b>W A S D</b>  Moverse      <b>Mouse</b>  Apuntar\n" +
        "<b>Q / Clic izquierdo</b>  Garfio      <b>E / Clic derecho</b>  Red\n" +
        "<b>Espacio</b>  Fase (desapareces 1.5 s)      <b>F</b>  Escudo (refleja el garfio)\n" +
        "<b>B</b>  Tienda      <b>Esc</b>  Pausa\n\n" +
        "<color=#FF8A8A><b>Jugador 2</b> (modo 2 Jugadores)</color>\n" +
        "<b>Flechas</b>  Moverse y apuntar      <b>.</b>  Garfio      <b>,</b>  Red\n" +
        "<b>-</b>  Fase      <b>M</b>  Escudo      (también funciona un mando)\n\n" +
        "Jala a un enemigo hasta tu lado del río para eliminarlo.\n" +
        "<size=75%><color=#9AA6BD>Música: \"Thatched Villagers\" y \"Five Armies\" de Kevin MacLeod (incompetech.com), licencia CC BY 4.0</color></size>";

    static readonly string[] StepTitles = { "MODO DE JUEGO", "PERSONAJE", "DIFICULTAD", "MAPA" };
    static readonly string[] ModeShort =
    {
        "Tú y un aliado contra 2 bots. 10 kills ganan la ronda, mejor de 3.",
        "Oleadas cada vez más fuertes. Tienes 3 vidas.",
        "Uno en cada orilla, en la misma PC. El jugador 2 usa las flechas o un mando.",
    };
    static readonly string[] DifficultyShort =
    {
        "La IA es más lenta y falla más.",
        "Un reto equilibrado.",
        "IA rápida y precisa, que esquiva y usa el escudo.",
    };
    static readonly string[] ArenaNames = { "Pradera", "Desierto", "Nieve" };
    static readonly string[] ArenaShort =
    {
        "Donde empezó la guerra. Rocas y arbustos.",
        "Donde el río se vuelve oasis. Más rocas para cubrirse.",
        "Donde nace el río. Bloques de hielo que se mueven.",
    };

    static readonly Color Normal = new Color(0.16f, 0.32f, 0.58f);
    static readonly Color Selected = new Color(0.95f, 0.55f, 0.15f);

    TMP_Text hintText;
    TMP_Text titleText;
    int step;
    readonly List<Button>[] stepButtons = { new List<Button>(), new List<Button>(), new List<Button>(), new List<Button>() };

    void Start()
    {
        Time.timeScale = 1f;
        BuildExtraUI();
        SetPanels(true, false, false, false);
        Refresh();

        if (volumeSlider != null)
        {
            volumeSlider.value = PlayerPrefs.GetFloat("volumen", 0.8f);
            volumeSlider.onValueChanged.AddListener(AudioManager.SetVolume);
        }

        // La historia se cuenta con una intro animada al abrir el juego
        IntroCinematic.PlayOnce(GetComponentInParent<Canvas>(), difficultyText);
    }

    public void ShowMain() { SetPanels(true, false, false, true); }
    public void ShowControls() { SetPanels(false, false, true, true); }

    public void ShowLevels()
    {
        step = 0;
        SetPanels(false, true, false, true);
        Refresh();
    }

    // En el botón: OnClick > MainMenu.PlayLevel y el nombre de la escena (Arena01, Arena02 o Arena03)
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
        Refresh();
    }

    public void SetMode(int mode)
    {
        GameSession.Mode = (GameMode)mode;
        AudioManager.Play(Sfx.Click);
        GoToStep(1);
    }

    public void SetCharacter(int index)
    {
        GameSession.Character = index;
        AudioManager.Play(Sfx.Click);
        GoToStep(2);
    }

    public void Back()
    {
        AudioManager.Play(Sfx.Click);
        if (step > 0) GoToStep(step - 1);
        else SetPanels(true, false, false, false);
    }

    public void Quit()
    {
        SceneLoader.Quit();
    }

    void GoToStep(int newStep)
    {
        step = Mathf.Clamp(newStep, 0, 3);
        Refresh();
    }

    void Refresh()
    {
        if (titleText != null) titleText.text = $"PASO {step + 1} DE 4  ·  {StepTitles[step]}";

        // Resumen de lo que ya se eligió en los pasos anteriores
        if (difficultyText != null)
        {
            var parts = new List<string>();
            if (step > 0) parts.Add("Modo: " + GameSession.ModeNames[(int)GameSession.Mode]);
            if (step > 1) parts.Add("Personaje: " + GameSession.CurrentCharacter.name);
            if (step > 2) parts.Add("Dificultad: " + GameManager.DifficultyNames[GameManager.MenuDifficulty]);
            difficultyText.text = parts.Count > 0 ? string.Join("   ·   ", parts) : "Elige cómo quieres jugar";
        }

        if (hintText != null)
            hintText.text = step == 3 ? "Elige un mapa para empezar la batalla" : "Elige una opción para continuar";

        int[] selected = { (int)GameSession.Mode, GameSession.Character, GameManager.MenuDifficulty, -1 };
        for (int s = 0; s < stepButtons.Length; s++)
        {
            for (int i = 0; i < stepButtons[s].Count; i++)
            {
                Button b = stepButtons[s][i];
                if (b == null) continue;
                b.gameObject.SetActive(s == step);
                if (s < 3) b.GetComponent<Image>().color = i == selected[s] ? Selected : Normal;
            }
        }

        // El récord de supervivencia se muestra en su tarjeta
        if (stepButtons[0].Count > 1)
        {
            string record = GameManager.SurvivalRecord > 0 ? $" Tu récord: oleada {GameManager.SurvivalRecord}." : "";
            SetCard(stepButtons[0][1], GameSession.ModeNames[1], ModeShort[1] + record);
        }
    }

    void SetPanels(bool main, bool levels, bool controls, bool click)
    {
        if (click) AudioManager.Play(Sfx.Click);
        if (mainPanel != null) mainPanel.SetActive(main);
        if (levelPanel != null) levelPanel.SetActive(levels);
        if (controlsPanel != null) controlsPanel.SetActive(controls);

        // Los integrantes solo se muestran en la pantalla principal (para que no se asomen detrás de los paneles)
        Transform credits = transform.Find("Integrantes");
        if (credits != null) credits.gameObject.SetActive(main);
    }

    // ---------------------------------------------------------------------------------------------
    // Construcción de los elementos nuevos del menú
    // ---------------------------------------------------------------------------------------------

    void BuildExtraUI()
    {
        if (controlsPanel != null) SetPanelText(controlsPanel, ControlsText, 27f);
        BuildWizard();
    }

    static void SetPanelText(GameObject panel, string text, float size)
    {
        Transform t = panel.transform.Find("Texto");
        TMP_Text label = t != null ? t.GetComponent<TMP_Text>() : null;
        if (label == null) return;

        RectTransform prt = (RectTransform)panel.transform;
        prt.sizeDelta = new Vector2(1150f, 640f);
        prt.anchoredPosition = new Vector2(0f, -150f);

        label.text = text;
        label.fontSize = size;
        label.enableAutoSizing = false;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.alignment = TextAlignmentOptions.Top;
        RectTransform rt = label.rectTransform;
        rt.anchoredPosition = new Vector2(0f, -30f);
        rt.sizeDelta = new Vector2(1060f, 500f);
    }

    // Pasarela: en cada paso se ven solo 3 tarjetas grandes
    void BuildWizard()
    {
        if (levelPanel == null) return;
        Transform p = levelPanel.transform;

        RectTransform prt = (RectTransform)p;
        prt.sizeDelta = new Vector2(1300f, 620f);
        prt.anchoredPosition = new Vector2(0f, -160f);

        Transform title = p.Find("Titulo");
        if (title != null)
        {
            titleText = title.GetComponent<TMP_Text>();
            ((RectTransform)title).anchoredPosition = new Vector2(0f, -18f);
        }
        if (difficultyText != null)
        {
            difficultyText.fontSize = 28;
            difficultyText.rectTransform.anchoredPosition = new Vector2(0f, -88f);
            difficultyText.rectTransform.sizeDelta = new Vector2(1220f, 40f);

            hintText = Instantiate(difficultyText, p);
            hintText.name = "Ayuda";
            hintText.fontSize = 24;
            hintText.color = new Color(0.82f, 0.86f, 0.93f);
            hintText.rectTransform.anchoredPosition = new Vector2(0f, -452f);
        }

        Button template = FindButton(p, "Dificultad_0");
        if (template == null) return;
        Vector2 size = new Vector2(390f, 280f);
        float[] xs = { -410f, 0f, 410f };
        const float y = -150f;

        for (int i = 0; i < 3; i++)
        {
            int index = i;

            Button mode = Clone(template, "Modo_" + i, new Vector2(xs[i], y), size, () => SetMode(index));
            SetCard(mode, GameSession.ModeNames[i], ModeShort[i]);
            stepButtons[0].Add(mode);

            CharacterInfo c = GameSession.Characters[i];
            Button character = Clone(template, "Personaje_" + i, new Vector2(xs[i], y), size, () => SetCharacter(index));
            SetCard(character, c.name, $"{c.description}\nVida {c.health:0} · Velocidad {c.speed:0.#} · Garfio {c.hookDamage:0}");
            stepButtons[1].Add(character);
        }

        // Dificultad: se usan los 3 botones que ya existían; al elegir se pasa al mapa
        for (int i = 0; i < 3; i++)
        {
            Button diff = FindButton(p, "Dificultad_" + i);
            if (diff == null) continue;
            Place(diff, new Vector2(xs[i], y), size);
            SetCard(diff, GameManager.DifficultyNames[i], DifficultyShort[i]);
            diff.onClick.AddListener(() => GoToStep(3));
            stepButtons[2].Add(diff);
        }

        // Mapas
        Button a1 = FindButton(p, "Arena1");
        Button a2 = FindButton(p, "Arena2");
        if (a1 != null && a2 != null)
        {
            Button a3 = Clone(a2, "Arena3", Vector2.zero, size, () => PlayLevel("Arena03"));
            a3.GetComponent<Image>().color = new Color(0.35f, 0.62f, 0.8f);
            Button[] arenas = { a1, a2, a3 };
            for (int i = 0; i < 3; i++)
            {
                Place(arenas[i], new Vector2(xs[i], y), size);
                SetCard(arenas[i], ArenaNames[i], $"{ArenaShort[i]}\nIA base: nivel {i + 3}");
                stepButtons[3].Add(arenas[i]);
            }
        }

        Button back = FindButton(p, "Volver");
        if (back != null)
        {
            Place(back, new Vector2(0f, 26f), new Vector2(300f, 64f), bottom: true);
            back.onClick = new Button.ButtonClickedEvent();
            back.onClick.AddListener(Back);
            TMP_Text label = back.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = "Atrás";
        }
    }

    // Tarjeta: nombre grande arriba y una descripción corta debajo
    static void SetCard(Button b, string name, string description)
    {
        TMP_Text text = b != null ? b.GetComponentInChildren<TMP_Text>(true) : null;
        if (text == null) return;

        text.enableAutoSizing = false;
        text.fontSize = 44;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.alignment = TextAlignmentOptions.Center;
        text.margin = new Vector4(22f, 16f, 22f, 16f);
        text.text = $"<b>{name}</b>\n<size=50%><line-height=125%>{description}</line-height></size>";
    }

    static Button FindButton(Transform parent, string name)
    {
        Transform t = parent.Find(name);
        return t != null ? t.GetComponent<Button>() : null;
    }

    static void Place(Button b, Vector2 pos, Vector2 size, bool bottom = false)
    {
        RectTransform rt = (RectTransform)b.transform;
        float y = bottom ? 0f : 1f;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, y);
        rt.pivot = new Vector2(0.5f, y);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static Button Clone(Button source, string name, Vector2 pos, Vector2 size, UnityAction onClick)
    {
        Button b = Instantiate(source, source.transform.parent);
        b.name = name;
        b.onClick = new Button.ButtonClickedEvent(); // Sin las acciones del botón copiado
        b.onClick.AddListener(onClick);
        Place(b, pos, size);
        return b;
    }
}
