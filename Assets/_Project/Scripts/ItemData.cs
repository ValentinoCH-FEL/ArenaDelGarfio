using UnityEngine;

// Un item de la tienda. Se crea con: clic derecho > Create > Arena del Garfio > Item de tienda.
public enum UpgradeType
{
    DanoGarfio, AlcanceGarfio, VelocidadGarfio, RecargaGarfio,
    VidaMaxima, VelocidadMovimiento, DuracionRed, RecargaDash
}

[CreateAssetMenu(fileName = "NuevoItem", menuName = "Arena del Garfio/Item de tienda")]
public class ItemData : ScriptableObject
{
    public string itemName = "Nuevo item";
    [TextArea] public string description;
    public Sprite icon;
    public int cost = 100;
    public int maxLevel = 3;
    public UpgradeType type;
    public float amount = 5f;

    public void ApplyTo(Unit unit)
    {
        var hook = unit.GetComponent<HookAbility>();
        var net = unit.GetComponent<NetAbility>();
        var dash = unit.GetComponent<DashAbility>();
        var player = unit.GetComponent<PlayerController>();
        var bot = unit.GetComponent<BotController>();

        switch (type)
        {
            case UpgradeType.DanoGarfio: if (hook) hook.damage += amount; break;
            case UpgradeType.AlcanceGarfio: if (hook) hook.range += amount; break;
            case UpgradeType.VelocidadGarfio: if (hook) hook.speed += amount; break;
            case UpgradeType.RecargaGarfio: if (hook) hook.cooldown = Mathf.Max(0.5f, hook.cooldown - amount); break;
            case UpgradeType.VidaMaxima: unit.Health.AddMaxHealth(amount); break;
            case UpgradeType.VelocidadMovimiento:
                if (player) player.moveSpeed += amount;
                if (bot) bot.speedBonus += amount;
                break;
            case UpgradeType.DuracionRed: if (net) net.rootDuration += amount; break;
            case UpgradeType.RecargaDash: if (dash) dash.cooldown = Mathf.Max(1f, dash.cooldown - amount); break;
        }
    }
}
