using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Handles visual, stack text, and rotation transforms
/// for item inside inventory grid or equipment slots.
/// * Used script for item prefab
/// </summary>
public class ItemUIController : MonoBehaviour
{
    #region Serialized Fields
    [Header("UI References")]
    [Tooltip("Rendered icon for sprite")]
    [SerializeField] private Image iconImage;

    [Tooltip("9-slice background highlighting the item's occupied grid cells.")]
    [SerializeField] private Image gridBackgroundImage;

    [Tooltip("Sprite used by the 9-slice item background.")]
    [SerializeField] private Sprite gridBackgroundSprite;

    [Tooltip("TextMeshPro displaying stack amount.")]
    [SerializeField] private TextMeshProUGUI stackText;

    [Tooltip("Prefab containing the InventoryItemActionMenu and its action buttons.")]
    [SerializeField] private GameObject actionMenuPrefab;

    [Tooltip("RectTransform of the prefab")]
    [SerializeField] private RectTransform rectTransform;

    private UIHoverTooltip hoverTooltip;
    private TextMeshProUGUI upgradeLevelText;
    #endregion

    #region Lifecycle
    private void Awake()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (iconImage != null)
            hoverTooltip = iconImage.GetComponent<UIHoverTooltip>();

        if (hoverTooltip == null)
            hoverTooltip = GetComponent<UIHoverTooltip>();

        Transform existingBadge = transform.Find("Upgrade Level");
        if (existingBadge != null)
            upgradeLevelText = existingBadge.GetComponent<TextMeshProUGUI>();
    }
    #endregion

    #region Public API
    /// <summary>
    /// Initializes UI and applies grid layout scaling item.
    /// </summary>
    /// <param name="item">Target inventory item data</param>
    /// <param name="cellSize">Pixel size individual grid cell.</param>
    public void Setup(InventoryItem item, float cellSize)
    {
        if (item == null || item.Data == null) return;

        UpdateIconSprite(item);
        UpdateTooltip(item);
        UpdateGridBackground(item, cellSize);
        UpdateStackText(item);
        UpdateUpgradeLevel(item);
        UpdateLayout(item, cellSize);
    }

    /// <summary>
    /// Configures the UI element for fixed-size equipment slot.
    /// </summary>
    /// <param name="item">Target inventory item data</param>
    /// <param name="slotSize">Pixel dimensions equipment slot.</param>
    public void SetupForEquipment(InventoryItem item, float slotSize = 64f)
    {
        if (item == null || item.Data == null) return;

        rectTransform.sizeDelta = new Vector2(slotSize, slotSize);
        UpdateTooltip(item);
        if (gridBackgroundImage != null)
            gridBackgroundImage.enabled = false;

        if (iconImage != null)
        {
            // Fallback to inventory icon if there is no dedicated equipment slot icon
            Sprite equipSprite = item.Data.equipmentIcon != null ? item.Data.equipmentIcon : item.Data.inventoryIcon;
            iconImage.sprite = equipSprite;

            RectTransform iconRect = iconImage.rectTransform;
            iconRect.sizeDelta = new Vector2(slotSize, slotSize);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.localEulerAngles = Vector3.zero;
            iconImage.preserveAspect = true;
        }

        UpdateStackText(item);
        UpdateUpgradeLevel(item);
    }

    /// <summary>
    /// Recalculates rect dimensions, stack text, and applies sprite transformations for rotated items.
    /// </summary>
    /// <param name="item">Target inventory item data</param>
    /// <param name="cellSize">Pixel size individual grid cell.</param>
    public void UpdateLayout(InventoryItem item, float cellSize)
    {
        if (item == null || item.Data == null) return;

        rectTransform.localScale = Vector3.one;

        float activeWidth = item.GetWidth() * cellSize;
        float activeHeight = item.GetHeight() * cellSize;
        rectTransform.sizeDelta = new Vector2(activeWidth, activeHeight);
        UpdateGridBackground(item, cellSize);

        if (iconImage != null)
        {
            UpdateIconSprite(item);

            float unrotatedWidth = item.Data.gridWidth * cellSize;
            float unrotatedHeight = item.Data.gridHeight * cellSize;

            RectTransform iconRect = iconImage.rectTransform;
            iconRect.sizeDelta = new Vector2(unrotatedWidth, unrotatedHeight);
            iconRect.anchoredPosition = Vector2.zero;
            iconImage.preserveAspect = true;

            // Rotate inner icon transform directly to prevent sprite distortion
            float rotationAngle = -90f * item.RotationIndex;
            iconRect.localEulerAngles = new Vector3(0, 0, rotationAngle);
        }

        UpdateStackText(item);
    }

    public void ShowActionMenu(InventoryItem item, InventoryGrid sourceGrid,
        EquipmentSlotUI sourceSlot, Vector2 screenPosition)
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        InventoryItemActionMenu.Show(actionMenuPrefab, canvas, item, sourceGrid, sourceSlot, screenPosition);
    }
    #endregion

    #region Private Helpers
    private void UpdateIconSprite(InventoryItem item)
    {
        if (iconImage == null || item?.Data == null) return;

        iconImage.sprite = item.Data.inventoryIcon;
    }

    private void UpdateTooltip(InventoryItem item)
    {
        if (hoverTooltip == null || item == null || item.Data == null)
            return;

        hoverTooltip.SetDescription(BuildTooltipText(item));
    }

    private string BuildTooltipText(InventoryItem item)
    {
        ItemData data = item.Data;
        float upgradeMultiplier = item.UpgradeMultiplier;
        StringBuilder text = new StringBuilder();
        text.AppendLine($"<color={GetRarityColor(data.Rarity)}>{data.itemName}</color>");
        text.AppendLine($"Rarity: {FormatEnum(data.Rarity)}");

        if (!string.IsNullOrWhiteSpace(data.itemDescription))
            text.AppendLine(data.itemDescription);

        if (data.EquipmentType != EquipmentType.None)
        {
            text.AppendLine($"Equipment Type: {FormatEnum(data.EquipmentType)}");
            text.AppendLine(
                $"Upgrade Level: {item.UpgradeLevel}/{InventoryItem.MaximumUpgradeLevel}"
            );

            if (data.StatCapType != StatCapType.None)
            {
                string capName = data.StatCapType == StatCapType.PrimaryAttribute
                    ? FormatEnum(data.StatCapAttribute)
                    : FormatEnum(data.StatCapTrait);
                text.AppendLine($"Stat Cap: {capName} {data.StatCapValue}");
            }
        }

        int modifierNumber = 1;
        foreach (EquipmentStat modifier in data.StatModifiers)
        {
            text.AppendLine();
            text.AppendLine($"Modifier {modifierNumber}:");
            text.AppendLine(FormatModifier(modifier, upgradeMultiplier));
            modifierNumber++;
        }

        if (data.EquipmentType == EquipmentType.Weapon)
        {
            int damageNumber = 1;
            foreach (WeaponDamage damage in data.WeaponDamages)
            {
                text.AppendLine();
                text.AppendLine($"Attack Damage {damageNumber}:");
                text.AppendLine(FormatWeaponDamage(damage, upgradeMultiplier));
                damageNumber++;
            }

            if (data.WeaponSkillType != WeaponSkillType.None)
            {
                text.AppendLine();
                text.AppendLine("Skill Damage:");
                text.AppendLine(FormatWeaponDamage(data.SkillDamageEntry, upgradeMultiplier));
            }

            if (data.WeaponSkillType == WeaponSkillType.ChargedArrow ||
                data.WeaponSkillType == WeaponSkillType.Beam)
            {
                text.AppendLine();
                text.AppendLine("Charged Skill Damage Type:");
                text.AppendLine(FormatWeaponDamage(
                    data.ChargedSkillDamageEntry,
                    upgradeMultiplier
                ));
            }
        }

        return text.ToString().TrimEnd();
    }

    private string GetRarityColor(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Uncommon => "#66FF66",
            ItemRarity.Rare => "#66B3FF",
            ItemRarity.Legendary => "#FFB347",
            _ => "#FFFFFF"
        };
    }

    private string FormatWeaponDamage(WeaponDamage damage, float upgradeMultiplier)
    {
        StringBuilder text = new StringBuilder(FormatDamageType(damage.damageType));
        text.Append($" ({FormatEnum(damage.damageSlot)})");

        if (damage.lingeringDamage)
            text.Append(
                $" [Lingering: {damage.lingeringBaseValue * upgradeMultiplier:0.##} base]"
            );

        float value = damage.value * upgradeMultiplier;
        string sign = value > 0f ? "+" : string.Empty;
        string suffix = damage.modifierType == StatModifierType.Percent ? "%" : string.Empty;
        text.Append($": {sign}{value:0.##}{suffix}");

        return $"<color=#00FF00>{text}</color>";
    }

    private string FormatModifier(EquipmentStat modifier, float upgradeMultiplier)
    {
        StringBuilder text = new StringBuilder(FormatEnum(modifier.statType));

        switch (modifier.statType)
        {
            case StatType.Damage:
                bool hasDamageDetails = modifier.damageType != DamageType.None ||
                                        modifier.damageSlot != DamageSlot.Primary;
                if (hasDamageDetails)
                    text.Append(" (");
                if (modifier.damageType != DamageType.None)
                    text.Append(FormatDamageType(modifier.damageType));
                if (modifier.damageType != DamageType.None && modifier.damageSlot != DamageSlot.Primary)
                    text.Append(", ");
                if (modifier.damageSlot != DamageSlot.Primary)
                    text.Append(FormatEnum(modifier.damageSlot));
                if (hasDamageDetails)
                    text.Append(")");
                if (modifier.lingeringDamage)
                    text.Append(
                        $" [Lingering: {modifier.lingeringBaseValue * upgradeMultiplier:0.##} base]"
                    );
                break;
            case StatType.BaseDamageResistance:
            case StatType.DamageResistance:
                if (modifier.damageType != DamageType.None)
                    text.Append($" ({FormatDamageType(modifier.damageType)})");
                break;
            case StatType.AttributeReduction:
                text.Append($" ({FormatEnum(modifier.reducedAttribute)})");
                break;
            case StatType.TraitReduction:
                text.Append($" ({FormatEnum(modifier.reducedTrait)})");
                break;
            case StatType.Rejuvenation:
                text.Append($" ({FormatEnum(modifier.rejuvenationType)})");
                break;
        }

        bool isReduction = modifier.statType == StatType.AttributeReduction ||
                           modifier.statType == StatType.TraitReduction;
        float upgradedValue = modifier.value * upgradeMultiplier;
        float displayValue = isReduction ? -Mathf.Abs(upgradedValue) : upgradedValue;
        string sign = displayValue > 0f ? "+" : string.Empty;
        string suffix = modifier.modifierType == StatModifierType.Percent ? "%" : string.Empty;
        text.Append($": {sign}{displayValue:0.##}{suffix}");

        string color = displayValue > 0f
            ? "#00FF00"
            : displayValue < 0f
                ? "#FF0000"
                : "#FFFFFF";

        return $"<color={color}>{text}</color>";
    }

    private string FormatDamageType(DamageType damageType)
    {
        return FormatEnum(damageType);
    }

    private string FormatEnum<T>(T value) where T : System.Enum
    {
        string name = value.ToString();
        StringBuilder result = new StringBuilder();

        for (int index = 0; index < name.Length; index++)
        {
            if (index > 0 && char.IsUpper(name[index]))
                result.Append(' ');
            result.Append(name[index]);
        }

        return result.ToString();
    }

    private void UpdateGridBackground(InventoryItem item, float cellSize)
    {
        if (gridBackgroundImage == null)
            return;

        gridBackgroundImage.sprite = gridBackgroundSprite;
        gridBackgroundImage.type = Image.Type.Sliced;
        gridBackgroundImage.raycastTarget = false;
        gridBackgroundImage.enabled = gridBackgroundSprite != null;
        gridBackgroundImage.rectTransform.SetAsFirstSibling();
        gridBackgroundImage.rectTransform.sizeDelta = new Vector2(
            item.GetWidth() * cellSize,
            item.GetHeight() * cellSize
        );
    }

    private void UpdateStackText(InventoryItem item)
    {
        if (stackText == null) return;

        if (item.Data.isStackable && item.Quantity > 1)
        {
            stackText.gameObject.SetActive(true);
            stackText.text = item.Quantity.ToString();
        }
        else
        {
            stackText.gameObject.SetActive(false);
        }
    }

    private void UpdateUpgradeLevel(InventoryItem item)
    {
        if (item == null || item.Data == null ||
            item.Data.EquipmentType == EquipmentType.None ||
            item.Data.EquipmentType == EquipmentType.Consumable)
        {
            if (upgradeLevelText != null)
                upgradeLevelText.gameObject.SetActive(false);
            return;
        }

        if (upgradeLevelText == null)
        {
            upgradeLevelText = new GameObject(
                "Upgrade Level",
                typeof(RectTransform),
                typeof(TextMeshProUGUI)
            ).GetComponent<TextMeshProUGUI>();
            upgradeLevelText.font = TMP_Settings.defaultFontAsset;
            if (upgradeLevelText.font == null)
                upgradeLevelText.font = Resources.Load<TMP_FontAsset>(
                    "Fonts & Materials/LiberationSans SDF"
                );
            upgradeLevelText.fontSize = 13;
            upgradeLevelText.fontStyle = FontStyles.Bold;
            upgradeLevelText.alignment = TextAlignmentOptions.Bottom;
            upgradeLevelText.margin = Vector4.zero;
            upgradeLevelText.enableWordWrapping = false;
            upgradeLevelText.raycastTarget = false;
        }

        upgradeLevelText.transform.SetParent(rectTransform, false);
        RectTransform badgeRect = upgradeLevelText.rectTransform;
        badgeRect.anchorMin = new Vector2(0.5f, 0f);
        badgeRect.anchorMax = new Vector2(0.5f, 0f);
        badgeRect.pivot = new Vector2(0.5f, 0f);
        badgeRect.anchoredPosition = new Vector2(0f, 1f);
        badgeRect.sizeDelta = new Vector2(60f, 19f);
        upgradeLevelText.gameObject.SetActive(true);
        upgradeLevelText.text = $"LVL {item.UpgradeLevel}";
        upgradeLevelText.color = GetUpgradeLevelColor(item.UpgradeLevel);
        upgradeLevelText.transform.SetAsLastSibling();
    }

    private static Color GetUpgradeLevelColor(int level)
    {
        return level switch
        {
            0 => Color.white,
            <= 3 => new Color(0.45f, 1f, 0.48f),
            <= 6 => new Color(0.35f, 0.78f, 1f),
            < InventoryItem.MaximumUpgradeLevel => new Color(0.82f, 0.55f, 1f),
            _ => new Color(1f, 0.78f, 0.25f)
        };
    }
    #endregion
}