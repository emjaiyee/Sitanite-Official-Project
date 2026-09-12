using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeadsUpTextView : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private RectTransform backgroundRect;
    [SerializeField] private TextMeshProUGUI messageText;

    public CanvasGroup CanvasGroup => canvasGroup;
    public Image BackgroundImage => backgroundImage;
    public RectTransform BackgroundRect => backgroundRect != null ? backgroundRect : transform as RectTransform;
    public TextMeshProUGUI MessageText => messageText;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        if (backgroundRect == null)
            backgroundRect = transform as RectTransform;

        if (messageText == null)
            messageText = GetComponentInChildren<TextMeshProUGUI>(true);
    }
}