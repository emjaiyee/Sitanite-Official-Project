using UnityEngine;
using UnityEngine.UI;

public class HeadwearVanityButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image buttonImage;

    [Header("Button Sprites")]
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

    public void ToggleVanity()
    {
        FindCustomizationController();

        if (customizationController == null)
            return;

        bool currentlyHidden =
            customizationController.IsHeadwearHidden();

        customizationController.SetHeadwearHidden(
            !currentlyHidden
        );
    }

    public void RefreshVisual()
    {
        if (customizationController == null)
        {
            FindCustomizationController();
        }

        if (customizationController == null ||
            buttonImage == null)
            return;

        bool isHidden =
            customizationController.IsHeadwearHidden();

        buttonImage.sprite = isHidden
            ? activeSprite
            : inactiveSprite;
    }

    private void FindCustomizationController()
    {
        if (Player.Instance == null)
        {
            Debug.LogError(
                $"HeadwearVanityButton '{name}': Player.Instance is NULL.",
                this
            );

            return;
        }

        customizationController =
            Player.Instance.GetComponent<CharacterCustomizationController>();

        if (customizationController == null)
        {
            Debug.LogError(
                $"HeadwearVanityButton '{name}': CharacterCustomizationController not found on Player.",
                Player.Instance
            );
        }
    }
}