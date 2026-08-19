using UnityEngine;

[CreateAssetMenu(menuName = "Character/Headwear")]
public class HeadwearDefinition : CharacterPartDefinition
{
    public bool hidesHair;

    private void OnValidate()
    {
        type = CharacterPartType.Headwear;
    }
}