using TMPro;
using UnityEngine;

public class FloorTextView : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI floorText;
    [SerializeField] private RectTransform textRect;

    public CanvasGroup CanvasGroup => canvasGroup;
    public TextMeshProUGUI FloorText => floorText;
    public RectTransform TextRect =>
        textRect != null ? textRect : transform as RectTransform;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (floorText == null)
            floorText = GetComponentInChildren<TextMeshProUGUI>(true);

        if (textRect == null && floorText != null)
            textRect = floorText.rectTransform;
    }
}