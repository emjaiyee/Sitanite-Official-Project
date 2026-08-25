using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIHoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [Header("Tooltip")]
    [TextArea(2, 6)]
    [SerializeField] private string description;
    [Tooltip("Prefab containing the tooltip window and its TMP_Text description field.")]
    [SerializeField] private GameObject tooltipPrefab;

    [Header("Position")]
    [SerializeField] private Vector2 pointerOffset = Vector2.zero;

    private UIHoverTooltipWindow activeTooltip;
    private RectTransform canvasRect;
    private Camera canvasCamera;

    private void Awake()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvasRect = canvas.GetComponent<RectTransform>();
            canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowTooltip(eventData.position);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (activeTooltip != null)
            SetTooltipPosition(eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (activeTooltip != null)
            activeTooltip.BeginClose();
    }

    public void ShowTooltip()
    {
        ShowTooltip(Input.mousePosition);
    }

    public void HideTooltip()
    {
        if (activeTooltip != null)
            Destroy(activeTooltip.gameObject);

        activeTooltip = null;
    }

    private void ShowTooltip(Vector2 screenPosition)
    {
        if (tooltipPrefab == null || canvasRect == null)
            return;

        if (activeTooltip == null)
        {
            GameObject tooltipObject = Instantiate(
                tooltipPrefab,
                canvasRect
            );

            activeTooltip = tooltipObject.GetComponent<UIHoverTooltipWindow>();
            if (activeTooltip == null)
            {
                Debug.LogError(
                    "UIHoverTooltip: Tooltip prefab requires a UIHoverTooltipWindow component.",
                    tooltipObject
                );
                Destroy(tooltipObject);
                return;
            }

            activeTooltip.Initialize(description, this);
        }

        SetTooltipPosition(screenPosition);
    }

    private void SetTooltipPosition(Vector2 screenPosition)
    {
        if (activeTooltip == null)
            return;

        RectTransform tooltipRect = activeTooltip.RectTransform;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition,
            canvasCamera,
            out Vector2 localPosition
        );

        tooltipRect.localPosition = localPosition + pointerOffset;
    }

    public void ClearTooltip(UIHoverTooltipWindow tooltip)
    {
        if (activeTooltip == tooltip)
            activeTooltip = null;
    }
}
