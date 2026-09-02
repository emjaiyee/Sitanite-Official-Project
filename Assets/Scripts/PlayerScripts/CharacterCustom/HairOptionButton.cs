using UnityEngine;

public class HairOptionButton : MonoBehaviour
{
    [Header("Hair")]
    [SerializeField] private CharacterPartDefinition hair;

    public void SelectHair()
    {
        if (Player.Instance == null)
        {
            Debug.LogError(
                $"HairOptionButton '{name}': Player.Instance is NULL.",
                this
            );

            return;
        }

        CharacterCustomizationController customizationController =
            Player.Instance.GetComponent<CharacterCustomizationController>();

        if (customizationController == null)
        {
            Debug.LogError(
                "HairOptionButton: CharacterCustomizationController not found on Player.",
                Player.Instance
            );

            return;
        }

        if (hair == null)
        {
            Debug.LogError(
                $"HairOptionButton '{name}' has no hair assigned.",
                this
            );

            return;
        }

        customizationController.SetHair(hair);
    }
}