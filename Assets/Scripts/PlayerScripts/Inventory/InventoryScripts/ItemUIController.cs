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

    [Tooltip("RectTransform of the prefab")]
    [SerializeField] private RectTransform rectTransform;

    private UIHoverTooltip hoverTooltip;
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

        hoverTooltip.SetDescription(BuildTooltipText(item.Data));
    }

    private string BuildTooltipText(ItemData data)
    {
        StringBuilder text = new StringBuilder();
        text.AppendLine($"<color=#FFFF00>{data.itemName}</color>");

        if (!string.IsNullOrWhiteSpace(data.itemDescription))
            text.AppendLine(data.itemDescription);

        if (data.EquipmentType != EquipmentType.None)
        {
            text.AppendLine($"Equipment Type: {FormatEnum(data.EquipmentType)}");

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
            text.AppendLine(FormatModifier(modifier));
            modifierNumber++;
        }

        return text.ToString().TrimEnd();
    }

    private string FormatModifier(EquipmentStat modifier)
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
        }

        bool isReduction = modifier.statType == StatType.AttributeReduction ||
                           modifier.statType == StatType.TraitReduction;
        float displayValue = isReduction ? -Mathf.Abs(modifier.value) : modifier.value;
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
    #endregion
}