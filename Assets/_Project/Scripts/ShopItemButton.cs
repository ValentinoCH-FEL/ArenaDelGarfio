using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Un botón de la tienda (va en el prefab "BotonItem").
public class ShopItemButton : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] Image iconImage;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text descriptionText;
    [SerializeField] TMP_Text costText;
    [SerializeField] TMP_Text levelText;

    ItemData item;
    ShopUI shop;

    public void Setup(ItemData data, ShopUI owner)
    {
        item = data;
        shop = owner;

        if (button == null) button = GetComponent<Button>();
        if (nameText != null) nameText.text = item.itemName;
        if (descriptionText != null) descriptionText.text = item.description;
        if (iconImage != null)
        {
            iconImage.sprite = item.icon;
            iconImage.enabled = item.icon != null;
        }
        button.onClick.AddListener(() => shop.Buy(item));
        Refresh();
    }

    public void Refresh()
    {
        if (item == null || shop == null) return;

        int lvl = shop.GetLevel(item);
        bool maxed = lvl >= item.maxLevel;
        if (levelText != null) levelText.text = $"Nv {lvl}/{item.maxLevel}";
        if (costText != null) costText.text = maxed ? "MAX" : $"{item.cost} oro";
        if (button != null) button.interactable = shop.CanBuy(item);
    }
}
