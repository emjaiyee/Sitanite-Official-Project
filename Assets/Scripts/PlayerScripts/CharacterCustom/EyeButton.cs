using UnityEngine;

public class EyeButton : MonoBehaviour
{
    [Header("Eye Set")]
    [SerializeField] private CharacterPartDefinition maleEyes;
    [SerializeField] private CharacterPartDefinition femaleEyes;

    public void SelectEyes()
    {
        if (Player.Instance == null)
        {
            Debug.LogError(
                $"EyeButton '{name}': Player.Instance is NULL.",
                this
            );

            return;
        }

        CharacterCustomizationController customizationController =
            Player.Instance.GetComponent<CharacterCustomizationController>();

        if (customizationController == null)
        {
            Debug.LogError(
                "EyeButton: CharacterCustomizationController not found on Player.",
                Player.Instance
            );

            return;
        }

        customizationController.SetEyes(
            maleEyes,
            femaleEyes
        );
    }
}