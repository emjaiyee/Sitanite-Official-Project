using UnityEngine;

public class HairButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HairSelectionUI hairSelectionUI;

    public void OpenHairSelection()
    {
        if (Player.Instance == null)
        {
            Debug.LogError(
                $"HairButton '{name}': Player.Instance is NULL.",
                this
            );

            return;
        }

        CharacterCustomizationController customizationController =
            Player.Instance.GetComponent<CharacterCustomizationController>();

        if (customizationController == null)
        {
            Debug.LogError(
                "HairButton: CharacterCustomizationController not found on Player.",
                Player.Instance
            );

            return;
        }

        if (hairSelectionUI == null)
        {
            Debug.LogError(
                $"HairButton '{name}': HairSelectionUI is not assigned.",
                this
            );

            return;
        }

        CharacterGender gender =
            customizationController.GetGender();

        switch (gender)
        {
            case CharacterGender.Male:
                hairSelectionUI.OpenMaleHair();
                break;

            case CharacterGender.Female:
                hairSelectionUI.OpenFemaleHair();
                break;
        }
    }
}