using UnityEngine;

[RequireComponent(typeof(PlayerStats))]
public class PlayerAttributesNTraits : MonoBehaviour
{
    [Header("Primary Attributes")]
    [Min(0)]
    [SerializeField] private int strength = 5;

    [Min(0)]
    [SerializeField] private int dexterity = 5;

    [Min(0)]
    [SerializeField] private int intelligence = 5;

    public int Strength => strength;
    public int Dexterity => dexterity;
    public int Intelligence => intelligence;

    public int GetDamageModifier(DamageType damageType)
    {
        int strengthModifier = strength - 5;
        int dexterityModifier = dexterity - 5;
        int intelligenceModifier = intelligence - 5;
        int modifier = 0;

        if ((damageType & (DamageType.Slash | DamageType.Blunt | DamageType.Physical)) != 0)
            modifier += strengthModifier * 5;

        if ((damageType & (DamageType.Pierce | DamageType.Stab)) != 0)
            modifier += dexterityModifier * 5;

        if ((damageType & (DamageType.Burning | DamageType.Frost | DamageType.Poison |
                           DamageType.Lightning | DamageType.Psychic)) != 0)
        {
            modifier += intelligenceModifier * 3;
        }

        return modifier;
    }

    public int MaxHealthModifier => (strength - 5) * 10;
    public int MaxManaModifier => (intelligence - 5) * 10;
    public int MaxStaminaModifier => (dexterity - 5) * 10;
}