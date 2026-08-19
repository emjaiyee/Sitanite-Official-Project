using UnityEngine;
using UnityEngine.UI;

public class GenderButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image buttonImage;

    [Header("Gender")]
    [SerializeField] private CharacterGender gender;

    [Header("Sprites")]
    [SerializeField] private Sprite activeSprite;
    [SerializeField] private Sprite inactiveSprite;

    private CharacterCustomizationController customizationController;

    private void Start()
    {
        FindCustomizationController();
        RefreshVisual();
    }

    public void SelectGender()
    {
        FindCustomizationController();

        if (customizationController == null)
        {
            Debug.LogError(
                $"GenderButton '{name}': CharacterCustomizationController could not be found.",
                this
            );

            return;
        }

        Debug.Log($"Gender button selected: {gender}");

        customizationController.SetGender(gender);
    }

    public void RefreshVisual()
    {
        if (customizationController == null)
        {
            FindCustomizationController();
        }

        if (customizationController == null || buttonImage == null)
            return;

        bool isActive =
            customizationController.GetGender() == gender;

        buttonImage.sprite = isActive
            ? activeSprite
            : inactiveSprite;
    }

    private void FindCustomizationController()
    {
        if (Player.Instance == null)
        {
            Debug.LogError(
                $"GenderButton '{name}': Player.Instance is NULL.",
                this
            );

            return;
        }

        customizationController =
            Player.Instance.GetComponent<CharacterCustomizationController>();

        if (customizationController == null)
        {
            Debug.LogError(
                $"GenderButton '{name}': CharacterCustomizationController not found on Player.",
                Player.Instance
            );
        }
    }
}