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
        "<color=#FF8A8A><b>Jugador 2</b> (modo 2 Jugadores, Quito)</color>\n" +
        "<b>Flechas</b>  Moverse y apuntar      <b>.</b>  Garfio      <b>,</b>  Red\n" +
        "<b>-</b>  Fase      <b>M</b>  Escudo      (también funciona un mando)\n\n" +
        "Jala a un enemigo hasta tu lado del río para eliminarlo.\n\n" +
        "<color=#F2BE3C><b>Voz</b></color>  En el menú: <b>Voz > Hablar</b> y el juego repite tu frase. En la partida pulsa <b>V</b>\n" +
        "y di: arriba, abajo, izquierda, derecha, alto, garfio, red, escudo, fase, tienda, pausa...\n" +
        "<color=#F2BE3C><b>En línea</b></color>  Uno crea la sala y comparte el código; el otro lo escribe y pulsa Unirse.\n" +
        "<size=75%><color=#9AA6BD>Música: \"Thatched Villagers\" y \"Five Armies\" de Kevin MacLeod (incompetech.com), licencia CC BY 4.0</color></size>";

    static readonly string[] StepTitles = { "MODO DE JUEGO", "PERSONAJE", "DIFICULTAD", "MAPA" };
    static readonly string[] ModeShort =
    {
        "Tú y un aliado contra 2 bots. 10 kills ganan la ronda, mejor de 3.",
        "Oleadas cada vez más fuertes. Tienes 3 vidas.",
        "Uno en cada orilla, en la misma PC. El jugador 2 usa las flechas o un mando.",
        "Memoriza el orden en que se encienden las runas y engánchalas igual. Cada nivel, una runa más.",
    };
    static readonly string[] DifficultyShort =
    {
        "La IA es más lenta y falla más.",
        "Un reto equilibrado.",
        "IA rápida y precisa, que esquiva y usa el escudo.",
    };
    static readonly Color Normal = new Color(0.16f, 0.32f, 0.58f);
    static readonly Color Selected = new Color(0.95f, 0.55f, 0.15f);

    static readonly Color Locked = new Color(0.25f, 0.25f, 0.28f);
    static readonly Color[] LevelColors =
    {
        new Color(0.24f, 0.5f, 0.28f), new Color(0.72f, 0.45f, 0.2f), new Color(0.35f, 0.62f, 0.8f),
    };

    TMP_Text hintText;
    TMP_Text welcomeText;
    TMP_Text titleText;
    int step;
    readonly List<Button>[] stepButtons = { new List<Button>(), new List<Button>(), new List<Button>(), new List<Button>() };

    void Start()
    {
        Time.timeScale = 1f;
        if (PartidaEnLinea.Activa) PartidaEnLinea.Terminar(); // Volver al menú cierra la sala
        BuildExtraUI();
        SetPanels(true, false, false, false);
        Refresh();

        if (volumeSlider != null)
        {
            volumeSlider.value = PlayerPrefs.GetFloat("volumen", 0.8f);
            volumeSlider.onValueChanged.AddListener(AudioManager.SetVolume);
        }

        // Antes del menú se piden los datos del DNI (solo si todavía no están guardados)
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && !PerfilJugador.EstaRegistrado)
            PantallaRegistro.Mostrar(canvas.transform, Font, ShowWelcome);

        // La historia se cuenta con una intro animada al abrir el juego (queda encima del registro)
        IntroCinematic.PlayOnce(canvas, difficultyText);
    }

    TMP_FontAsset Font => difficultyText != null ? difficultyText.font : null;

    void ShowWelcome()
    {
        if (welcomeText != null)
            welcomeText.text = PerfilJugador.EstaRegistrado ? $"Bienvenido, <noparse>{PerfilJugador.Nombre}</noparse>" : "";
    }

    void ShowVoice()
    {
        AudioManager.Play(Sfx.Click);
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null) PantallaVoz.Mostrar(canvas.transform, Font);
    }

    void ShowOnline()
    {
        AudioManager.Play(Sfx.Click);
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null) PantallaEnLinea.Mostrar(canvas.transform, Font);
    }

    // Botones "En línea" y "Voz", saludo y portada en pixel art
    void BuildMainScreen()
    {
        if (mainPanel != null)
        {
            Button play = FindButton(mainPanel.transform, "Jugar");
            if (play != null)
            {
                AddMainButton(play, "EnLinea", "En línea", ShowOnline);
                AddMainButton(play, "Voz", "Voz", ShowVoice);
            }

            string[] order = { "Jugar", "EnLinea", "Voz", "Controles", "Salir" };
            for (int i = 0; i < order.Length; i++)
            {
                Button b = FindButton(mainPanel.transform, order[i]);
                if (b != null) Place(b, new Vector2(0f, -78f * i), new Vector2(460f, 66f));
            }
            Move("TextoVolumen", -394f);
            Move("SliderVolumen", -436f);

            Image box = mainPanel.GetComponent<Image>();
            if (box != null) box.enabled = false;
        }

        Portada.Construir(transform, Font);

        welcomeText = UIPixel.Texto(transform, "Bienvenida", "", 34f, new Vector2(0f, -285f), new Vector2(1400f, 50f), Font,
                                    new Color(1f, 0.86f, 0.35f));
        welcomeText.outlineWidth = 0.25f;
        welcomeText.outlineColor = Color.black;
        if (mainPanel != null) welcomeText.transform.SetSiblingIndex(mainPanel.transform.GetSiblingIndex());
        ShowWelcome();
    }

    static void AddMainButton(Button template, string name, string text, UnityAction onClick)
    {
        Button b = Clone(template, name, Vector2.zero, Vector2.zero, onClick);
        TMP_Text label = b.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = text;
    }

    void Move(string child, float y)
    {
        var rt = mainPanel.transform.Find(child) as RectTransform;
        if (rt != null) rt.anchoredPosition = new Vector2(0f, y);
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
            hintText.text = step != 3 ? "Elige una opción para continuar"
                : GameSession.Mode == GameMode.Clasico ? "Gana cada batalla para abrir la siguiente"
                : "Elige un mapa para empezar la batalla";

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

        RefreshLevels();

        // El récord de supervivencia se muestra en su tarjeta
        if (stepButtons[0].Count > 1)
        {
            string record = GameManager.SurvivalRecord > 0 ? $" Tu récord: oleada {GameManager.SurvivalRecord}." : "";
            SetCard(stepButtons[0][1], GameSession.ModeNames[1], ModeShort[1] + record, 32f);
        }
        if (stepButtons[0].Count > 3 && MemoryMode.Record > 0)
            SetCard(stepButtons[0][3], GameSession.ModeNames[3], ModeShort[3] + $" Tu récord: {MemoryMode.Record} runas.", 32f);
    }

    // Paso 4: tarjetas de los niveles. En Clásico los que faltan abrir salen con candado
    void RefreshLevels()
    {
        for (int i = 0; i < stepButtons[3].Count; i++)
        {
            Button b = stepButtons[3][i];
            bool open = Niveles.EstaAbierto(i);
            b.interactable = open;
            b.GetComponent<Image>().color = open ? LevelColors[i] : Locked;
            SetCard(b, $"Nivel {i + 1}\n<size=75%>{Niveles.Nombres[i]}</size>",
                open ? $"{Niveles.Descripciones[i]}\nIA base: nivel {i + 3}" : $"BLOQUEADO\nGana el nivel {i} para abrirlo.", 40f);

            Transform candado = b.transform.Find("Candado");
            if (candado == null && !open)
            {
                var img = new GameObject("Candado", typeof(RectTransform)).AddComponent<Image>();
                img.transform.SetParent(b.transform, false);
                img.sprite = PixelSprites.Candado;
                img.raycastTarget = false;
                RectTransform rt = img.rectTransform;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0f, 14f);
                rt.sizeDelta = new Vector2(56f, 64f);
                candado = img.transform;
            }
            if (candado != null) candado.gameObject.SetActive(!open);
        }
    }

    void SetPanels(bool main, bool levels, bool controls, bool click)
    {
        if (click) AudioManager.Play(Sfx.Click);
        if (mainPanel != null) mainPanel.SetActive(main);
        if (levelPanel != null) levelPanel.SetActive(levels);
        if (controlsPanel != null) controlsPanel.SetActive(controls);

        // Los integrantes solo se muestran en la pantalla principal (para que no se asomen detrás de los paneles)
        foreach (string name in new[] { "Integrantes", "Bienvenida", "GuerreroCusco", "GuerreroQuito" })
        {
            Transform t = transform.Find(name);
            if (t != null) t.gameObject.SetActive(main);
        }
    }

    // ---------------------------------------------------------------------------------------------
    // Construcción de los elementos nuevos del menú
    // ---------------------------------------------------------------------------------------------

    void BuildExtraUI()
    {
        if (controlsPanel != null) SetPanelText(controlsPanel, ControlsText, 24f);
        BuildWizard();
        BuildMainScreen();
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

        // Paso 1: 4 modos (Clásico, Supervivencia, 2 Jugadores y Memoria), tarjetas más angostas
        float[] modeXs = { -480f, -160f, 160f, 480f };
        for (int i = 0; i < GameSession.ModeNames.Length; i++)
        {
            int index = i;
            Button mode = Clone(template, "Modo_" + i, new Vector2(modeXs[i], y), new Vector2(300f, 280f), () => SetMode(index));
            SetCard(mode, GameSession.ModeNames[i], ModeShort[i], 32f);
            stepButtons[0].Add(mode);
        }

        for (int i = 0; i < 3; i++)
        {
            int index = i;

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
            Button[] arenas = { a1, a2, a3 };
            for (int i = 0; i < 3; i++)
            {
                Place(arenas[i], new Vector2(xs[i], y), size);
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
    static void SetCard(Button b, string name, string description, float size = 44f)
    {
        TMP_Text text = b != null ? b.GetComponentInChildren<TMP_Text>(true) : null;
        if (text == null) return;

        text.enableAutoSizing = false;
        text.fontSize = size;
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
