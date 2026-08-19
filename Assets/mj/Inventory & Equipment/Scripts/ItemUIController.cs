using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemUIController : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI stackText;
    [SerializeField] private RectTransform rectTransform;

    private void Awake()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();
    }

    public void Setup(InventoryItem item, float cellSize)
    {
        if (item == null || item.Data == null) return;

        UpdateIconSprite(item);
        UpdateStackText(item);
        UpdateLayout(item, cellSize);
    }

    public void SetupForEquipment(InventoryItem item, float slotSize = 64f)
    {
        if (item == null || item.Data == null) return;

        rectTransform.sizeDelta = new Vector2(slotSize, slotSize);

        if (iconImage != null)
        {
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
            iconImage.rectTransform.sizeDelta = new Vector2(unrotatedWidth, unrotatedHeight);
            iconImage.rectTransform.anchoredPosition = Vector2.zero;

            iconImage.preserveAspect = true;

            float rotationAngle = -90f * item.RotationIndex;
            iconImage.rectTransform.localEulerAngles = new Vector3(0, 0, rotationAngle);
        }

        UpdateStackText(item);
    }

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
}