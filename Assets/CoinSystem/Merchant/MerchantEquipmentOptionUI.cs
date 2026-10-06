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
    [SerializeField] private Image rarityBorderImage;

    [Header("Selection Appearance")]
    [SerializeField] private Graphic selectionGraphic;
    [SerializeField] private Color normalColor = new Color(0.12f, 0.15f, 0.2f, 1f);
    [SerializeField] private Color selectedColor = new Color(0.31f, 0.28f, 0.19f, 1f);

    public InventoryItem Item { get; private set; }

    private Color rarityBackgroundColor = Color.white;

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

        ConfigureCompactLayout();
        ApplyRarityAppearance(item);

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

    private void ConfigureCompactLayout()
    {
        if (itemIcon != null)
        {
            RectTransform iconRect = itemIcon.rectTransform;
            iconRect.sizeDelta = new Vector2(30f, 30f);
            iconRect.anchoredPosition = new Vector2(-77f, 0f);
        }

        if (itemNameText != null)
        {
            RectTransform nameRect = itemNameText.rectTransform;
            nameRect.sizeDelta = new Vector2(142f, 20f);
            nameRect.anchoredPosition = new Vector2(26f, 9f);
            itemNameText.alignment = TextAlignmentOptions.Left;
        }

        if (upgradeLevelText != null)
        {
            RectTransform levelRect = upgradeLevelText.rectTransform;
            levelRect.sizeDelta = new Vector2(142f, 16f);
            levelRect.anchoredPosition = new Vector2(26f, -10f);
            upgradeLevelText.fontSize = 10f;
        }
    }

    public void SetSelected(bool selected)
    {
        if (selectionGraphic != null)
            selectionGraphic.color = (selected ? selectedColor : normalColor) * rarityBackgroundColor;
    }

    private void ApplyRarityAppearance(InventoryItem item)
    {
        ItemUIController rarityStyleSource = EquipmentManager.Instance?.ItemPrefab != null
            ? EquipmentManager.Instance.ItemPrefab.GetComponent<ItemUIController>()
            : FindFirstObjectByType<ItemUIController>();

        if (rarityStyleSource == null)
        {
            Debug.LogWarning("Unable to apply merchant item rarity styling because no ItemUIController is available.", this);
            return;
        }

        rarityBackgroundColor = rarityStyleSource.GetRarityBackgroundColor(item.Data.Rarity);
        if (selectionGraphic != null)
            selectionGraphic.color = normalColor * rarityBackgroundColor;

        Sprite borderSprite = rarityStyleSource.GetRarityBorderSprite(item.Data.Rarity);
        if (borderSprite == null)
        {
            if (rarityBorderImage != null)
                rarityBorderImage.enabled = false;
            return;
        }

        if (rarityBorderImage == null)
        {
            GameObject borderObject = new GameObject(
                "Rarity Border",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );
            RectTransform borderRect = borderObject.GetComponent<RectTransform>();
            borderRect.SetParent(transform, false);
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;
            rarityBorderImage = borderObject.GetComponent<Image>();
        }

        rarityBorderImage.sprite = borderSprite;
        rarityBorderImage.color = Color.white;
        rarityBorderImage.preserveAspect = false;
        rarityBorderImage.raycastTarget = false;
        rarityBorderImage.enabled = true;
        rarityBorderImage.transform.SetAsLastSibling();
    }
}
