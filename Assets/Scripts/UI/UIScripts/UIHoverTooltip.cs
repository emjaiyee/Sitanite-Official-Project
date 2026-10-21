using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIHoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    private static readonly HashSet<UIHoverTooltip> Instances = new HashSet<UIHoverTooltip>();

    [Header("Tooltip")]
    [TextArea(2, 6)]
    [SerializeField] private string description;
    [Tooltip("Prefab containing the tooltip window and its TMP_Text description field.")]
    [SerializeField] private GameObject tooltipPrefab;
    [Min(0f)]
    [SerializeField] private float showDelay = 0.25f;

    [Header("Position")]
    [SerializeField] private Vector2 pointerOffset = Vector2.zero;
    [Tooltip("Minimum gap between the tooltip and the canvas edges.")]
    [Min(0f)]
    [SerializeField] private float canvasEdgeMargin = 8f;

    public bool IsPointerOver { get; private set; }

    private UIHoverTooltipWindow activeTooltip;
    private Coroutine showRoutine;
    private Vector2 pendingScreenPosition;
    private Vector2 tooltipAnchor;
    private RectTransform canvasRect;
    private Camera canvasCamera;

    private void Awake()
    {
        Instances.Add(this);
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            // Use the root canvas so the tooltip isn't confined to a nested canvas.
            canvas = canvas.rootCanvas;
            canvasRect = canvas.GetComponent<RectTransform>();
            canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;
        }
    }

    private void OnDisable()
    {
        IsPointerOver = false;
        HideTooltip();
    }

    private void OnDestroy()
    {
        Instances.Remove(this);
    }

    public static void HideAll()
    {
        foreach (UIHoverTooltip tooltip in new List<UIHoverTooltip>(Instances))
            tooltip.HideTooltip();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        IsPointerOver = true;

        if (activeTooltip != null)
            activeTooltip.CancelClose();
        else
            ShowTooltip(eventData.position);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (activeTooltip == null)
            pendingScreenPosition = eventData.position;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        IsPointerOver = false;
        CancelShowRoutine();

        if (activeTooltip != null)
            activeTooltip.BeginClose();
    }

    public void ShowTooltip()
    {
        ShowTooltip(Input.mousePosition);
    }

    public void HideTooltip()
    {
        CancelShowRoutine();

        if (activeTooltip != null)
            Destroy(activeTooltip.gameObject);

        activeTooltip = null;
    }

    public void SetDescription(string newDescription)
    {
        description = newDescription;

        if (activeTooltip != null)
        {
            activeTooltip.SetDescription(description);
            ClampTooltipToCanvas();
        }
    }

    private void ShowTooltip(Vector2 screenPosition)
    {
        if (tooltipPrefab == null || canvasRect == null || activeTooltip != null || showRoutine != null)
            return;

        pendingScreenPosition = screenPosition;
        showRoutine = StartCoroutine(ShowAfterDelay());
    }

    private IEnumerator ShowAfterDelay()
    {
        if (showDelay > 0f)
            yield return new WaitForSeconds(showDelay);

        showRoutine = null;

        GameObject tooltipObject = Instantiate(tooltipPrefab, canvasRect);
        activeTooltip = tooltipObject.GetComponent<UIHoverTooltipWindow>();
        if (activeTooltip == null)
        {
            Debug.LogError(
                "UIHoverTooltip: Tooltip prefab requires a UIHoverTooltipWindow component.",
                tooltipObject
            );
            Destroy(tooltipObject);
            yield break;
        }

        Vector2 availableCanvasSize = canvasRect.rect.size - Vector2.one * (canvasEdgeMargin * 2f);
        activeTooltip.Initialize(description, this, Vector2.Max(availableCanvasSize, Vector2.one));

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            pendingScreenPosition,
            canvasCamera,
            out Vector2 localPosition
        );

        tooltipAnchor = localPosition + pointerOffset;
        ClampTooltipToCanvas();
    }

    private void CancelShowRoutine()
    {
        if (showRoutine == null)
            return;

        StopCoroutine(showRoutine);
        showRoutine = null;
    }

    private void ClampTooltipToCanvas()
    {
        if (activeTooltip == null)
            return;

        RectTransform tooltipRect = activeTooltip.RectTransform;
        Rect bounds = canvasRect.rect;
        Vector3 scale = tooltipRect.localScale;
        Vector2 scaledSize = new Vector2(
            tooltipRect.rect.width * Mathf.Abs(scale.x),
            tooltipRect.rect.height * Mathf.Abs(scale.y)
        );
        Vector2 pivot = tooltipRect.pivot;

        float minX = bounds.xMin + canvasEdgeMargin + scaledSize.x * pivot.x;
        float maxX = bounds.xMax - canvasEdgeMargin - scaledSize.x * (1f - pivot.x);
        float minY = bounds.yMin + canvasEdgeMargin + scaledSize.y * pivot.y;
        float maxY = bounds.yMax - canvasEdgeMargin - scaledSize.y * (1f - pivot.y);
        Vector2 position = new Vector2(
            Mathf.Clamp(tooltipAnchor.x, minX, Mathf.Max(minX, maxX)),
            Mathf.Clamp(tooltipAnchor.y, minY, Mathf.Max(minY, maxY))
        );

        tooltipRect.localPosition = new Vector3(position.x, position.y, tooltipRect.localPosition.z);
    }

    public void ClearTooltip(UIHoverTooltipWindow tooltip)
    {
        if (activeTooltip == tooltip)
            activeTooltip = null;
    }
}
