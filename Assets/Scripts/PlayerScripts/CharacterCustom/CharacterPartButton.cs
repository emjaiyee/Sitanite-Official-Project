using UnityEngine;

public class CharacterPartButton : MonoBehaviour
{
    [SerializeField] private CharacterRenderer characterRenderer;
    [SerializeField] private CharacterPartDefinition part;

    public void SelectPart()
    {
        CharacterAppearance appearance = characterRenderer.Appearance;

        switch (part.Type)
        {
            case CharacterPartType.Body:
                appearance.body = part;
                break;

            case CharacterPartType.Legs:
                appearance.legs = part;
                break;

            case CharacterPartType.Torso:
                appearance.torso = part;
                break;

            case CharacterPartType.Eyes:
                appearance.eyes = part;
                break;

            case CharacterPartType.Hair:
                appearance.hair = part;
                break;

            case CharacterPartType.Headwear:
                HeadwearDefinition headwear = part as HeadwearDefinition;

                if (headwear != null)
                {
                    appearance.headwear = headwear;
                }

                break;

            case CharacterPartType.Weapon:
                appearance.weapon = part as WeaponDefinition;
                break;
        }

        characterRenderer.UpdateAppearance();
    }
}