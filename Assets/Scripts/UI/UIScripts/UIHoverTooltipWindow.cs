using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIHoverTooltipWindow : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TMP_Text descriptionText;

    [Header("Layout")]
    [Min(0f)]
    [SerializeField] private float verticalPadding = 16f;
    [Tooltip("Smallest font size used when the tooltip must shrink to fit the canvas.")]
    [Min(1f)]
    [SerializeField] private float minFontSize = 8f;

    [Header("Sorting")]
    [Tooltip("Sorting order of the tooltip's own canvas so it draws above all other UI.")]
    [SerializeField] private int sortingOrder = 30000;

    public RectTransform RectTransform { get; private set; }

    private UIHoverTooltip owner;
    private Button ownerButton;
    private bool pointerInside;
    private Coroutine closeRoutine;
    private Vector2 maxCanvasSize = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
    private float defaultWidth;
    private float defaultFontSize;
    private float defaultFontSizeMin;
    private float defaultFontSizeMax;
    private bool defaultAutoSizing;
    private TextOverflowModes defaultOverflowMode;

    private void Awake()
    {
        RectTransform = GetComponent<RectTransform>();
        defaultWidth = RectTransform.rect.width;

        if (descriptionText == null)
            descriptionText = GetComponentInChildren<TMP_Text>(true);

        if (descriptionText != null)
        {
            defaultFontSize = descriptionText.fontSize;
            defaultFontSizeMin = descriptionText.fontSizeMin;
            defaultFontSizeMax = descriptionText.fontSizeMax;
            defaultAutoSizing = descriptionText.enableAutoSizing;
            defaultOverflowMode = descriptionText.overflowMode;

            RectTransform textRect = descriptionText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.offsetMin = new Vector2(0f, verticalPadding * 0.5f);
            textRect.offsetMax = new Vector2(0f, -verticalPadding * 0.5f);
        }
    }

    public void Initialize(string description, UIHoverTooltip tooltipOwner, Vector2 availableCanvasSize)
    {
        owner = tooltipOwner;
        ownerButton = owner == null ? null : owner.GetComponent<Button>();
        maxCanvasSize = availableCanvasSize;
        DrawAboveOtherUI();
        SetRaycastTargets(true);
        SetDescription(description);
    }

    public void SetDescription(string description)
    {
        if (descriptionText == null)
            return;

        descriptionText.text = description;
        ResizeToDescription();
    }

    private void ResizeToDescription()
    {
        Canvas.ForceUpdateCanvases();

        Vector3 scale = RectTransform.localScale;
        float maxWidth = maxCanvasSize.x / Mathf.Max(Mathf.Abs(scale.x), 0.0001f);
        float maxHeight = maxCanvasSize.y / Mathf.Max(Mathf.Abs(scale.y), 0.0001f);
        float width = Mathf.Min(defaultWidth, maxWidth);
        RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);

        descriptionText.enableAutoSizing = false;
        descriptionText.fontSize = defaultFontSize;
        descriptionText.fontSizeMin = defaultFontSizeMin;
        descriptionText.fontSizeMax = defaultFontSizeMax;
        descriptionText.overflowMode = defaultOverflowMode;
        Canvas.ForceUpdateCanvases();

        RectTransform textRect = descriptionText.rectTransform;
        float preferredHeight = descriptionText.GetPreferredValues(
            descriptionText.text,
            textRect.rect.width,
            float.PositiveInfinity
        ).y;
        float height = preferredHeight + verticalPadding;

        if (height > maxHeight)
        {
            height = maxHeight;
            descriptionText.fontSizeMin = Mathf.Min(minFontSize, defaultFontSize);
            descriptionText.fontSizeMax = defaultFontSize;
            descriptionText.enableAutoSizing = true;
            descriptionText.overflowMode = TextOverflowModes.Ellipsis;
        }

        RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        descriptionText.ForceMeshUpdate();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        CancelClose();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        BeginClose();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left &&
            ownerButton != null && ownerButton.interactable)
        {
            ownerButton.onClick.Invoke();
        }
    }

    public void BeginClose()
    {
        if (closeRoutine == null && isActiveAndEnabled)
            closeRoutine = StartCoroutine(CloseAfterPointerTransition());
    }

    public void CancelClose()
    {
        if (closeRoutine == null)
            return;

        StopCoroutine(closeRoutine);
        closeRoutine = null;
    }

    private IEnumerator CloseAfterPointerTransition()
    {
        yield return null;
        closeRoutine = null;

        if (!pointerInside && (owner == null || !owner.IsPointerOver))
        {
            owner?.ClearTooltip(this);
            Destroy(gameObject);
        }
    }

    private void DrawAboveOtherUI()
    {
        transform.SetAsLastSibling();

        Canvas parentCanvas = transform.parent == null ? null : transform.parent.GetComponentInParent<Canvas>();
        Canvas tooltipCanvas = GetComponent<Canvas>();
        if (tooltipCanvas == null)
            tooltipCanvas = gameObject.AddComponent<Canvas>();

        tooltipCanvas.overrideSorting = true;
        tooltipCanvas.sortingOrder = sortingOrder;
        if (parentCanvas != null)
        {
            tooltipCanvas.sortingLayerID = parentCanvas.rootCanvas.sortingLayerID;
            tooltipCanvas.additionalShaderChannels |= parentCanvas.rootCanvas.additionalShaderChannels;
        }

        // A sorted sub-canvas needs its own raycaster to receive hover/click events.
        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();
    }

    private void SetRaycastTargets(bool enabled)
    {
        foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = enabled;
    }
}
