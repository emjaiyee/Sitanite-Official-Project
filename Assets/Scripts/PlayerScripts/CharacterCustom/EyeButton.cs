using UnityEngine;

public class EyeButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterCustomizationController customizationController;

    [Header("Eye Set")]
    [SerializeField] private CharacterPartDefinition maleEyes;
    [SerializeField] private CharacterPartDefinition femaleEyes;

    public void SelectEyes()
    {
        if (customizationController == null)
        {
            Debug.LogError(
                $"EyeButton '{name}' has no CharacterCustomizationController assigned.",
                this
            );

            return;
        }

        customizationController.SetEyes(
            maleEyes,
            femaleEyes
        );
    }
}