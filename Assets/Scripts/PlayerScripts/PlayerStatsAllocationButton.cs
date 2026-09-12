using UnityEngine;
using UnityEngine.UI;

public class PlayerStatsAllocationButton : MonoBehaviour
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
            button.onClick.AddListener(Allocate);
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(Allocate);
    }

    public void Allocate()
    {
        if (statsUI == null)
            return;

        if (allocationType == AllocationType.Attribute)
            statsUI.AllocateAttribute(attribute);
        else
            statsUI.AllocateTrait(trait);
    }
}
