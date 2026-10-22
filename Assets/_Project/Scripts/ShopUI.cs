using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// Tienda de mejoras. Se abre y se cierra con la tecla B. Pausa el juego mientras está abierta.
// IMPORTANTE: pon este script en un objeto que siempre esté activo (no en el panel que se esconde).
public class ShopUI : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    [SerializeField] GameObject panel;
    [SerializeField] Transform itemsContainer;
    [SerializeField] ShopItemButton itemButtonPrefab;
    [SerializeField] TMP_Text goldText;
    [SerializeField] List<ItemData> items = new List<ItemData>();

    Unit player;
    readonly Dictionary<ItemData, int> levels = new Dictionary<ItemData, int>();
    readonly List<ShopItemButton> buttons = new List<ShopItemButton>();

    void Start()
    {
        player = Unit.FindPlayer();

        foreach (var item in items)
        {
            if (item == null) continue;
            ShopItemButton b = Instantiate(itemButtonPrefab, itemsContainer);
            b.Setup(item, this);
            buttons.Add(b);
        }

        if (player != null && player.Wallet != null)
            player.Wallet.OnGoldChanged += _ => Refresh();

        panel.SetActive(false);
        IsOpen = false;
        Refresh();
    }

    void OnDestroy() { IsOpen = false; }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame)
        {
            if (IsOpen) Close();
            else Open();
        }
    }

    public void Open()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null && (gm.IsPaused || gm.State == GameState.FinPartida)) return;

        panel.SetActive(true);
        IsOpen = true;
        if (gm != null) gm.SetPaused(true);
        AudioManager.Play(Sfx.Click);
        Refresh();
    }

    public void Close()
    {
        if (!IsOpen) return;
        panel.SetActive(false);
        IsOpen = false;
        if (GameManager.Instance != null) GameManager.Instance.SetPaused(false);
        AudioManager.Play(Sfx.Click);
    }

    public int GetLevel(ItemData item) => levels.TryGetValue(item, out int lvl) ? lvl : 0;

    public bool CanBuy(ItemData item) =>
        player != null && player.Wallet != null &&
        GetLevel(item) < item.maxLevel && player.Wallet.Gold >= item.cost;

    public void Buy(ItemData item)
    {
        if (!CanBuy(item)) return;
        if (!player.Wallet.TrySpend(item.cost)) return;

        item.ApplyTo(player);
        levels[item] = GetLevel(item) + 1;
        AudioManager.Play(Sfx.Comprar);
        Refresh();
    }

    void Refresh()
    {
        if (goldText != null && player != null && player.Wallet != null)
            goldText.text = $"Oro: {player.Wallet.Gold}";
        foreach (var b in buttons) b.Refresh();
    }
}
