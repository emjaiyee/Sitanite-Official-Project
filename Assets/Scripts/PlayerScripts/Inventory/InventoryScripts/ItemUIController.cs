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

    [Tooltip("TextMeshPro displaying stack amount.")]
    [SerializeField] private TextMeshProUGUI stackText;

    [Tooltip("RectTransform of the prefab")]
    [SerializeField] private RectTransform rectTransform;
    #endregion

    #region Lifecycle
    private void Awake()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();
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