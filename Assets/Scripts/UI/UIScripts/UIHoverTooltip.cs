using System.Collections;
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
    [Min(0f)]
    [SerializeField] private float showDelay = 0.25f;

    [Header("Position")]
    [SerializeField] private Vector2 pointerOffset = Vector2.zero;

    private UIHoverTooltipWindow activeTooltip;
    private Coroutine showRoutine;
    private Vector2 pendingScreenPosition;
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
        pendingScreenPosition = eventData.position;
        ShowTooltip(eventData.position);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        pendingScreenPosition = eventData.position;

        if (activeTooltip != null)
            SetTooltipPosition(eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
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
            activeTooltip.SetDescription(description);
    }

    private void ShowTooltip(Vector2 screenPosition)
    {
        if (tooltipPrefab == null || canvasRect == null)
            return;

        pendingScreenPosition = screenPosition;

        if (activeTooltip == null && showRoutine == null)
            showRoutine = StartCoroutine(ShowAfterDelay());
        else if (activeTooltip != null)
            SetTooltipPosition(screenPosition);
    }

    private IEnumerator ShowAfterDelay()
    {
        if (showDelay > 0f)
            yield return new WaitForSeconds(showDelay);

        showRoutine = null;

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
                yield break;
            }

            activeTooltip.Initialize(description, this);
        }

        SetTooltipPosition(pendingScreenPosition);
    }

    private void CancelShowRoutine()
    {
        if (showRoutine == null)
            return;

        StopCoroutine(showRoutine);
        showRoutine = null;
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
