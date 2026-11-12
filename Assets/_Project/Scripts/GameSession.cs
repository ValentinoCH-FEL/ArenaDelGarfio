using UnityEngine;
using UnityEngine.InputSystem;

// Lo que el jugador elige en el menú (modo, personaje) y los controles de cada jugador.
public enum GameMode { Clasico = 0, Supervivencia = 1, Versus = 2 }

public class CharacterInfo
{
    public string name, description, model;
    public float health, speed, hookDamage, hookCooldown;
}

public static class GameSession
{
    public const string ModeKey = "modo";
    public const string CharacterKey = "personaje";

    public static readonly string[] ModeNames = { "Clásico", "Supervivencia", "2 Jugadores" };
    public static readonly string[] ModeDescriptions =
    {
        "Clásico: tú y un aliado contra 2 bots. 10 kills ganan la ronda, mejor de 3.",
        "Supervivencia: oleadas cada vez más fuertes. Tienes 3 vidas. ¿Hasta qué oleada llegas?",
        "2 Jugadores en la misma PC. J2: flechas, punto = garfio, coma = red, guion = fase, M = escudo (o un mando).",
    };

    public static readonly CharacterInfo[] Characters =
    {
        new CharacterInfo { name = "Equilibrado", model = null, health = 100, speed = 6f, hookDamage = 35, hookCooldown = 3f,
            description = "Aldeano del clan Marea: 100 de vida, sin puntos débiles." },
        new CharacterInfo { name = "Veloz", model = "Modelo_Veloz", health = 75, speed = 7.5f, hookDamage = 30, hookCooldown = 2.4f,
            description = "Ninja de la orilla: 75 de vida, muy rápido y con recarga corta." },
        new CharacterInfo { name = "Tanque", model = "Modelo_Tanque", health = 150, speed = 5f, hookDamage = 45, hookCooldown = 3.4f,
            description = "Gólem de hierro: 150 de vida y garfio pesado, pero lento." },
    };

    public static GameMode Mode
    {
        get => (GameMode)Mathf.Clamp(PlayerPrefs.GetInt(ModeKey, 0), 0, 2);
        set { PlayerPrefs.SetInt(ModeKey, (int)value); PlayerPrefs.Save(); }
    }

    public static int Character
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(CharacterKey, 0), 0, Characters.Length - 1);
        set { PlayerPrefs.SetInt(CharacterKey, value); PlayerPrefs.Save(); }
    }

    public static CharacterInfo CurrentCharacter => Characters[Character];
}

// Estadísticas del jugador 1 durante la partida (se muestran al final).
public static class MatchStats
{
    public static int Kills, Deaths, HooksFired, HooksHit, Reflects, Runes;

    public static int Accuracy => HooksFired == 0 ? 0 : Mathf.RoundToInt(100f * HooksHit / HooksFired);

    public static void Reset() { Kills = Deaths = HooksFired = HooksHit = Reflects = Runes = 0; }
}

// Controles. Jugador 1: WASD + mouse. Jugador 2: flechas y las teclas punto, coma, guion y M, o un mando.
public static class InputMap
{
    static Gamepad Pad(int player) => player == 1 ? Gamepad.current : null;

    public static Vector2 Move(int player)
    {
        Vector2 v = Vector2.zero;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (player == 0)
            {
                if (kb.wKey.isPressed) v.y += 1;
                if (kb.sKey.isPressed) v.y -= 1;
                if (kb.dKey.isPressed) v.x += 1;
                if (kb.aKey.isPressed) v.x -= 1;
            }
            else
            {
                if (kb.upArrowKey.isPressed) v.y += 1;
                if (kb.downArrowKey.isPressed) v.y -= 1;
                if (kb.rightArrowKey.isPressed) v.x += 1;
                if (kb.leftArrowKey.isPressed) v.x -= 1;
            }
        }
        var pad = Pad(player);
        if (pad != null && pad.leftStick.ReadValue().magnitude > 0.25f) v = pad.leftStick.ReadValue();
        return Vector2.ClampMagnitude(v, 1f);
    }

    // Solo el mando: palanca derecha para apuntar
    public static Vector2 AimStick(int player)
    {
        var pad = Pad(player);
        return pad != null ? pad.rightStick.ReadValue() : Vector2.zero;
    }

    public static bool Hook(int player)
    {
        var kb = Keyboard.current; var pad = Pad(player);
        if (player == 0)
            return (kb != null && kb.qKey.wasPressedThisFrame) || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
        return (kb != null && kb.periodKey.wasPressedThisFrame) || (pad != null && pad.rightTrigger.wasPressedThisFrame);
    }

    public static bool Net(int player)
    {
        var kb = Keyboard.current; var pad = Pad(player);
        if (player == 0)
            return (kb != null && kb.eKey.wasPressedThisFrame) || (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame);
        return (kb != null && kb.commaKey.wasPressedThisFrame) || (pad != null && pad.leftTrigger.wasPressedThisFrame);
    }

    public static bool Phase(int player)
    {
        var kb = Keyboard.current; var pad = Pad(player);
        if (player == 0) return kb != null && kb.spaceKey.wasPressedThisFrame;
        return (kb != null && kb.slashKey.wasPressedThisFrame) || (pad != null && pad.buttonSouth.wasPressedThisFrame);
    }

    public static bool Shield(int player)
    {
        var kb = Keyboard.current; var pad = Pad(player);
        if (player == 0) return kb != null && kb.fKey.wasPressedThisFrame;
        return (kb != null && kb.mKey.wasPressedThisFrame) || (pad != null && pad.buttonEast.wasPressedThisFrame);
    }
}
