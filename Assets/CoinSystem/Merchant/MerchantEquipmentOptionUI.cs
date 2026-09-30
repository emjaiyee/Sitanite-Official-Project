using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class MerchantEquipmentOptionUI : MonoBehaviour
{
    [Header("Option UI")]
    [SerializeField] private Button button;
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI upgradeLevelText;

    [Header("Selection Appearance")]
    [SerializeField] private Graphic selectionGraphic;
    [SerializeField] private Color normalColor = new Color(0.12f, 0.15f, 0.2f, 1f);
    [SerializeField] private Color selectedColor = new Color(0.31f, 0.28f, 0.19f, 1f);

    public InventoryItem Item { get; private set; }

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();
    }

    public void Bind(InventoryItem item, Action<InventoryItem> onSelected, bool isEquipped)
    {
        if (button == null)
        {
            Debug.LogError("MerchantEquipmentOptionUI requires a Button component.", this);
            return;
        }

        Item = item;
        if (item == null || item.Data == null)
        {
            Debug.LogError("MerchantEquipmentOptionUI cannot display an item without item data.", this);
            button.interactable = false;
            return;
        }

        Sprite icon = item.Data.inventoryIcon != null
            ? item.Data.inventoryIcon
            : item.Data.equipmentIcon;

        if (itemIcon != null)
        {
            itemIcon.sprite = icon;
            itemIcon.enabled = icon != null;
        }

        if (itemNameText != null)
            itemNameText.text = item.Data.itemName;

        if (upgradeLevelText != null)
            upgradeLevelText.text = isEquipped
                ? $"LVL {item.UpgradeLevel}  |  EQUIPPED"
                : $"LVL {item.UpgradeLevel}  |  IN BAG";

        button.onClick.AddListener(() => onSelected?.Invoke(item));
        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (selectionGraphic != null)
            selectionGraphic.color = selected ? selectedColor : normalColor;
    }
}
