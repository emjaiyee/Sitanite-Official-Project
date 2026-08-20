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

        
        if (iconImage != null)
        {
            iconImage.sprite = item.Data.icon;
        }

        UpdateStackText(item);
        UpdateLayout(item, cellSize);
    }

    public void UpdateLayout(InventoryItem item, float cellSize)
    {
        if (item == null || item.Data == null) return;

        float activeWidth = item.GetWidth() * cellSize;
        float activeHeight = item.GetHeight() * cellSize;
        rectTransform.sizeDelta = new Vector2(activeWidth, activeHeight);

        if (iconImage != null)
        {
            float unrotatedWidth = item.Data.gridWidth * cellSize;
            float unrotatedHeight = item.Data.gridHeight * cellSize;
            
            iconImage.rectTransform.sizeDelta = new Vector2(unrotatedWidth, unrotatedHeight);
            iconImage.rectTransform.anchoredPosition = Vector2.zero;

            float rotationAngle = -90f * item.RotationIndex;
            iconImage.rectTransform.localEulerAngles = new Vector3(0, 0, rotationAngle);
        }

        UpdateStackText(item);
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