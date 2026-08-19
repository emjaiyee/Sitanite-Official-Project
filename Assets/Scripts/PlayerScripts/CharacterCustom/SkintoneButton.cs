using UnityEngine;

public class SkinToneButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterCustomizationController customizationController;

    [Header("Skin Tone")]
    [SerializeField] private CharacterPartDefinition maleSkinTone;
    [SerializeField] private CharacterPartDefinition femaleSkinTone;

    public void SelectSkinTone()
    {
        if (customizationController == null)
        {
            Debug.LogError(
                $"SkinToneButton '{name}' has no CharacterCustomizationController assigned.",
                this
            );

            return;
        }

        customizationController.SetSkinTone(
            maleSkinTone,
            femaleSkinTone
        );
    }
}