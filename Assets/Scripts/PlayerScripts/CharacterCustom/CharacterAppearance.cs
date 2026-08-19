using UnityEngine;

[System.Serializable]
public class CharacterAppearance
{
    [Header("Character")]
    public CharacterGender gender;

    [Header("Appearance")]
    public CharacterPartDefinition body;
    public CharacterPartDefinition legs;
    public CharacterPartDefinition torso;
    public CharacterPartDefinition eyes;
    public CharacterPartDefinition hair;
    public HeadwearDefinition headwear;
    public CharacterPartDefinition weapon;
}