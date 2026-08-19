using UnityEngine;

public class SkinToneButton : MonoBehaviour
{
    [Header("Skin Tone")]
    [SerializeField] private CharacterPartDefinition maleSkinTone;
    [SerializeField] private CharacterPartDefinition femaleSkinTone;

    public void SelectSkinTone()
    {
        if (Player.Instance == null)
        {
            Debug.LogError(
                $"SkinToneButton '{name}': Player.Instance is NULL.",
                this
            );

            return;
        }

        CharacterCustomizationController customizationController =
            Player.Instance.GetComponent<CharacterCustomizationController>();

        if (customizationController == null)
        {
            Debug.LogError(
                "SkinToneButton: CharacterCustomizationController not found on Player.",
                Player.Instance
            );

            return;
        }

        customizationController.SetSkinTone(
            maleSkinTone,
            femaleSkinTone
        );
    }
}