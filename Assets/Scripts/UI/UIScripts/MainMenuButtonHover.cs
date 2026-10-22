using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenuButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private static float nextHoverSfxTime;

    [SerializeField] private RectTransform textTransform;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Sprite leftIndicatorSprite;
    [SerializeField] private Sprite rightIndicatorSprite;
    [SerializeField] private float hoveredScale = 1.12f;
    [Min(0f)]
    [SerializeField] private float transitionSpeed = 12f;
    [Min(1f)]
    [SerializeField] private float indicatorSize = 18f;
    [Min(0f)]
    [SerializeField] private float indicatorGap = 10f;
    [Min(0f)]
    [SerializeField] private float indicatorHoverOffset = 4f;
    [SerializeField] private Color hoverColor = Color.yellow;
    [SerializeField] private AudioClip hoverSfx;
    [Range(0f, 1f)]
    [SerializeField] private float hoverSfxVolume = 1f;
    [Min(0.1f)]
    [SerializeField] private float hoverSfxMinPitch = 0.7f;
    [Min(0.1f)]
    [SerializeField] private float hoverSfxMaxPitch = 1.3f;
    [Min(0f)]
    [SerializeField] private float hoverSfxCooldown = 0.18f;

    private Vector3 normalScale;
    private Color normalColor;
    private Image leftIndicator;
    private Image rightIndicator;
    private Vector2 leftIndicatorPosition;
    private Vector2 rightIndicatorPosition;
    private bool isHovered;
    private bool hasCachedAppearance;

    private void Awake()
    {
        if (textTransform == null || label == null ||
            leftIndicatorSprite == null || rightIndicatorSprite == null)
        {
            Debug.LogError(
                "MainMenuButtonHover requires a text transform, label, and both indicator sprites.",
                this
            );
            enabled = false;
            return;
        }

        normalScale = textTransform.localScale;
        normalColor = label.color;
        CreateIndicators();
        hasCachedAppearance = true;
    }

    private void Update()
    {
        float interpolation = Mathf.Clamp01(
            1f - Mathf.Exp(-Mathf.Max(0f, transitionSpeed) * Time.unscaledDeltaTime)
        );
        Vector3 targetScale = isHovered ? normalScale * hoveredScale : normalScale;
        Color targetColor = isHovered ? hoverColor : normalColor;

        textTransform.localScale = Vector3.Lerp(textTransform.localScale, targetScale, interpolation);
        label.color = Color.Lerp(label.color, targetColor, interpolation);
        UpdateIndicator(leftIndicator, leftIndicatorPosition, Vector2.left, interpolation);
        UpdateIndicator(rightIndicator, rightIndicatorPosition, Vector2.right, interpolation);
    }

    private void OnDisable()
    {
        isHovered = false;

        if (!hasCachedAppearance)
            return;

        if (textTransform != null)
            textTransform.localScale = normalScale;

        if (label != null)
            label.color = normalColor;

        ResetIndicator(leftIndicator, leftIndicatorPosition);
        ResetIndicator(rightIndicator, rightIndicatorPosition);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isHovered && hoverSfx != null)
            PlayHoverSfx();

        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }

    private void PlayHoverSfx()
    {
        if (Time.unscaledTime < nextHoverSfxTime)
            return;

        float minimumPitch = Mathf.Max(0.1f, Mathf.Min(hoverSfxMinPitch, hoverSfxMaxPitch));
        float maximumPitch = Mathf.Max(minimumPitch, Mathf.Max(hoverSfxMinPitch, hoverSfxMaxPitch));
        float pitch = Random.Range(minimumPitch, maximumPitch);

        GameObject sfxObject = new GameObject("Menu Hover SFX");
        AudioSource sfxSource = sfxObject.AddComponent<AudioSource>();
        sfxSource.clip = hoverSfx;
        sfxSource.volume = hoverSfxVolume;
        sfxSource.pitch = pitch;
        sfxSource.spatialBlend = 0f;
        AudioManager.RegisterSfxSource(sfxSource);
        sfxSource.Play();
        nextHoverSfxTime = Time.unscaledTime + hoverSfxCooldown;

        Destroy(sfxObject, hoverSfx.length / pitch);
    }

    private void CreateIndicators()
    {
        float halfLabelWidth = label.GetPreferredValues().x * textTransform.localScale.x * 0.5f;
        float halfIconSize = indicatorSize * 0.5f;
        float centerX = textTransform.anchoredPosition.x;
        float centerY = textTransform.anchoredPosition.y;
        float distance = halfLabelWidth + indicatorGap + halfIconSize;

        leftIndicatorPosition = new Vector2(centerX - distance, centerY);
        rightIndicatorPosition = new Vector2(centerX + distance, centerY);

        leftIndicator = CreateIndicator(
            "Left Hover Indicator",
            leftIndicatorSprite,
            leftIndicatorPosition
        );
        rightIndicator = CreateIndicator(
            "Right Hover Indicator",
            rightIndicatorSprite,
            rightIndicatorPosition
        );
    }

    private Image CreateIndicator(string indicatorName, Sprite sprite, Vector2 position)
    {
        GameObject indicatorObject = new GameObject(
            indicatorName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        RectTransform indicatorTransform = indicatorObject.GetComponent<RectTransform>();
        indicatorTransform.SetParent(transform, false);
        indicatorTransform.anchorMin = new Vector2(0.5f, 0.5f);
        indicatorTransform.anchorMax = new Vector2(0.5f, 0.5f);
        indicatorTransform.pivot = new Vector2(0.5f, 0.5f);
        indicatorTransform.anchoredPosition = position;
        indicatorTransform.sizeDelta = Vector2.one * indicatorSize;
        indicatorTransform.localScale = Vector3.one * 0.7f;

        Image indicator = indicatorObject.GetComponent<Image>();
        indicator.sprite = sprite;
        indicator.preserveAspect = true;
        indicator.raycastTarget = false;
        indicator.color = new Color(1f, 1f, 1f, 0f);
        return indicator;
    }

    private void UpdateIndicator(
        Image indicator,
        Vector2 position,
        Vector2 direction,
        float interpolation
    )
    {
        if (indicator == null)
            return;

        RectTransform indicatorTransform = indicator.rectTransform;
        Vector2 targetPosition = isHovered
            ? position + direction * indicatorHoverOffset
            : position;
        float targetAlpha = isHovered ? 1f : 0f;
        float targetScale = isHovered ? 1f : 0.7f;

        indicatorTransform.anchoredPosition = Vector2.Lerp(
            indicatorTransform.anchoredPosition,
            targetPosition,
            interpolation
        );
        indicatorTransform.localScale = Vector3.Lerp(
            indicatorTransform.localScale,
            Vector3.one * targetScale,
            interpolation
        );

        Color indicatorColor = indicator.color;
        indicatorColor.a = Mathf.Lerp(indicatorColor.a, targetAlpha, interpolation);
        indicator.color = indicatorColor;
    }

    private static void ResetIndicator(Image indicator, Vector2 position)
    {
        if (indicator == null)
            return;

        indicator.rectTransform.anchoredPosition = position;
        indicator.rectTransform.localScale = Vector3.one * 0.7f;
        Color indicatorColor = indicator.color;
        indicatorColor.a = 0f;
        indicator.color = indicatorColor;
    }
}
