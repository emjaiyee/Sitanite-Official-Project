using UnityEngine;
using UnityEngine.UI;

public class PlayerStatsReturnButton : MonoBehaviour
{
    public enum AllocationType
    {
        Attribute,
        Trait
    }

    [Header("Allocation")]
    [SerializeField] private AllocationType allocationType;
    [SerializeField] private PrimaryAttribute attribute;
    [SerializeField] private SecondaryTrait trait;

    [Header("References")]
    [SerializeField] private PlayerStatsUI statsUI;
    [SerializeField] private Button button;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color emptyColor = Color.gray;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (statsUI == null)
            statsUI = GetComponentInParent<PlayerStatsUI>();
    }

    private void OnEnable()
    {
        if (button != null)
            button.onClick.AddListener(ReturnPoint);

        if (statsUI != null)
            statsUI.PendingAllocationsChanged += RefreshVisual;

        RefreshVisual();
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(ReturnPoint);

        if (statsUI != null)
            statsUI.PendingAllocationsChanged -= RefreshVisual;
    }

    public void ReturnPoint()
    {
        if (statsUI == null)
            return;

        if (allocationType == AllocationType.Attribute)
            statsUI.ReturnAttribute(attribute);
        else
            statsUI.ReturnTrait(trait);
    }

    private void RefreshVisual()
    {
        if (button == null)
            return;

        bool hasPending = allocationType == AllocationType.Attribute
            ? statsUI != null && statsUI.HasPendingAttribute(attribute)
            : statsUI != null && statsUI.HasPendingTrait(trait);

        ColorBlock colors = button.colors;
        colors.normalColor = normalColor;
        colors.disabledColor = emptyColor;
        button.colors = colors;
        button.interactable = hasPending;

        if (button.targetGraphic != null)
            button.targetGraphic.color = hasPending ? normalColor : emptyColor;
    }
}