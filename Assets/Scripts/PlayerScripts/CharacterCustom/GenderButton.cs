using UnityEngine;
using UnityEngine.UI;

public class GenderButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterCustomizationController customizationController;
    [SerializeField] private Image buttonImage;

    [Header("Gender")]
    [SerializeField] private CharacterGender gender;

    [Header("Sprites")]
    [SerializeField] private Sprite activeSprite;
    [SerializeField] private Sprite inactiveSprite;

    private void Start()
    {
        RefreshVisual();
    }

    public void SelectGender()
    {
        if (customizationController == null)
        {
            Debug.LogError(
                $"GenderButton '{name}' has no CharacterCustomizationController assigned.",
                this
            );

            return;
        }

        Debug.Log($"Gender button selected: {gender}");

        customizationController.SetGender(gender);

        GenderButtonGroup group =
            GetComponentInParent<GenderButtonGroup>();

        if (group != null)
        {
            group.Refresh();
        }
    }

    public void RefreshVisual()
    {
        if (customizationController == null || buttonImage == null)
            return;

        bool isActive =
            customizationController.GetGender() == gender;

        buttonImage.sprite = isActive
            ? activeSprite
            : inactiveSprite;
    }
}