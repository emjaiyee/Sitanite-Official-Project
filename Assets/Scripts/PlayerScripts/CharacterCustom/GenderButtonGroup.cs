using UnityEngine;

public class GenderButtonGroup : MonoBehaviour
{
    [SerializeField] private GenderButton maleButton;
    [SerializeField] private GenderButton femaleButton;

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
                "GenderButtonGroup: Player.Instance is NULL.",
                this
            );

            return;
        }

        customizationController =
            Player.Instance.GetComponent<CharacterCustomizationController>();

        if (customizationController == null)
        {
            Debug.LogError(
                "GenderButtonGroup: CharacterCustomizationController not found on Player.",
                Player.Instance
            );
        }
    }

    public void Refresh()
    {
        if (maleButton != null)
            maleButton.RefreshVisual();

        if (femaleButton != null)
            femaleButton.RefreshVisual();
    }
}