using UnityEngine;

[CreateAssetMenu(menuName = "Character/Weapon")]
public class WeaponDefinition : CharacterPartDefinition
{
    private void OnValidate()
    {
        type = CharacterPartType.Weapon;
    }
}