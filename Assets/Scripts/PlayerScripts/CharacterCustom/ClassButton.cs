using UnityEngine;
using UnityEngine.UI;

public class ClassButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image buttonImage;

    [Header("Class")]
    [SerializeField] private PlayerClass playerClass;

    [Header("Sprites")]
    [SerializeField] private Sprite activeSprite;
    [SerializeField] private Sprite inactiveSprite;

    private CharacterCustomizationController customizationController;

    private void Start()
    {
        FindCustomizationController();

        if (customizationController != null)
        {
            customizationController.OnAppearanceChanged += RefreshVisual;
        }

        RefreshVisual();
    }

    private void OnDestroy()
    {
        if (customizationController != null)
        {
            customizationController.OnAppearanceChanged -= RefreshVisual;
        }
    }

    public void SelectClass()
    {
        FindCustomizationController();

        if (customizationController == null)
        {
            Debug.LogError(
                $"ClassButton '{name}': CharacterCustomizationController could not be found.",
                this
            );

            return;
        }

        Debug.Log(
            $"Class button selected: {playerClass}"
        );

        customizationController.SetClass(playerClass);
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
            customizationController.GetClass() == playerClass;

        buttonImage.sprite = isActive
            ? activeSprite
            : inactiveSprite;
    }

    private void FindCustomizationController()
    {
        if (Player.Instance == null)
        {
            Debug.LogError(
                $"ClassButton '{name}': Player.Instance is NULL.",
                this
            );

            return;
        }

        customizationController =
            Player.Instance.GetComponent<CharacterCustomizationController>();

        if (customizationController == null)
        {
            Debug.LogError(
                $"ClassButton '{name}': CharacterCustomizationController not found on Player.",
                Player.Instance
            );
        }
    }
}