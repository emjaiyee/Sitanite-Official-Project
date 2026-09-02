using UnityEngine;

public class ClassButtonGroup : MonoBehaviour
{
    [SerializeField] private ClassButton warriorButton;
    [SerializeField] private ClassButton rangerButton;
    [SerializeField] private ClassButton mageButton;

    private CharacterCustomizationController customizationController;

    private void Start()
    {
        FindCustomizationController();

        if (customizationController != null)
        {
            customizationController.OnAppearanceChanged += Refresh;
        }

        Refresh();
    }

    private void OnDestroy()
    {
        if (customizationController != null)
        {
            customizationController.OnAppearanceChanged -= Refresh;
        }
    }

    private void FindCustomizationController()
    {
        if (Player.Instance == null)
        {
            Debug.LogError(
                "ClassButtonGroup: Player.Instance is NULL.",
                this
            );

            return;
        }

        customizationController =
            Player.Instance.GetComponent<CharacterCustomizationController>();

        if (customizationController == null)
        {
            Debug.LogError(
                "ClassButtonGroup: CharacterCustomizationController not found on Player.",
                Player.Instance
            );
        }
    }

    public void Refresh()
    {
        if (warriorButton != null)
            warriorButton.RefreshVisual();

        if (rangerButton != null)
            rangerButton.RefreshVisual();

        if (mageButton != null)
            mageButton.RefreshVisual();
    }
}