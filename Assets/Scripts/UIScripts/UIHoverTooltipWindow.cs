using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIHoverTooltipWindow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TMP_Text descriptionText;

    [Header("Layout")]
    [Min(0f)]
    [SerializeField] private float verticalPadding = 16f;

    public RectTransform RectTransform { get; private set; }

    private UIHoverTooltip owner;
    private bool pointerInside;
    private Coroutine closeRoutine;

    private void Awake()
    {
        RectTransform = GetComponent<RectTransform>();

        if (descriptionText == null)
            descriptionText = GetComponentInChildren<TMP_Text>(true);
    }

    public void Initialize(string description, UIHoverTooltip tooltipOwner)
    {
        owner = tooltipOwner;

        if (descriptionText != null)
        {
            descriptionText.text = description;
            ResizeToDescription();
        }
    }

    private void ResizeToDescription()
    {
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(descriptionText.rectTransform);

        Vector2 size = RectTransform.sizeDelta;
        size.y = descriptionText.preferredHeight + verticalPadding;
        RectTransform.sizeDelta = size;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;

        if (closeRoutine != null)
        {
            StopCoroutine(closeRoutine);
            closeRoutine = null;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        BeginClose();
    }

    public void BeginClose()
    {
        if (closeRoutine == null)
            closeRoutine = StartCoroutine(CloseAfterPointerTransition());
    }

    private IEnumerator CloseAfterPointerTransition()
    {
        yield return null;

        if (!pointerInside)
        {
            owner?.ClearTooltip(this);
            Destroy(gameObject);
        }

        closeRoutine = null;
    }
}