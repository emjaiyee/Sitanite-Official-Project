using System;
using UnityEngine;

[System.Serializable]
public class AttributeTraitDefaults
{
    [Tooltip("Increases Slash, Blunt, and Physical damage, plus maximum health.")]
    [Min(0)] public int strength = 5;
    [Tooltip("Increases Pierce and Stab damage, plus maximum stamina.")]
    [Min(0)] public int dexterity = 5;
    [Tooltip("Increases maximum mana by 7 per point above 5, and magical damage, including Burning, Frost, Poison, Lightning, Psychic, Necrosis, Water, Earth, Fire, and Air, by 3.")]
    [Min(0)] public int intelligence = 5;

    [Tooltip("Increases maximum health by 2 per point above 1.")]
    [Min(1)] public int vitality = 1;
    [Tooltip("Increases maximum mana by 2 per point above 1.")]
    [Min(1)] public int focus = 1;
    [Tooltip("Increases stamina regeneration by 0.2 per point above 1.")]
    [Min(1)] public int endurance = 1;
    [Tooltip("Increases maximum stamina by 2 per point above 1.")]
    [Min(1)] public int agility = 1;
    [Tooltip("Increases health regeneration by 0.2 per point above 1.")]
    [Min(1)] public int vigor = 1;
    [Tooltip("Increases movement, sprint, and dash speed by 0.02 per point above 1.")]
    [Min(1)] public int haste = 1;
    [Tooltip("Increases mana regeneration by 0.2 per point above 1.")]
    [Min(1)] public int attunement = 1;
    [Tooltip("Increases Slash, Blunt, Stab, and Physical damage by 1 per point above 1.")]
    [Min(1)] public int mundane = 1;
    [Tooltip("Increases Poison, Lightning, Frost, Psychic, and Necrosis damage by 1 per point above 1.")]
    [Min(1)] public int arcane = 1;
    [Tooltip("Increases Water, Earth, Fire, and Air damage by 1 per point above 1.")]
    [Min(1)] public int elemental = 1;
    [Tooltip("Increases Pierce damage by 1 per point above 1.")]
    [Min(1)] public int precision = 1;
    [Tooltip("Increases Slash, Blunt, Pierce, Stab, and Physical resistance by 0.5 per point above 1.")]
    [Min(1)] public int fortitude = 1;
    [Tooltip("Increases Poison, Lightning, Frost, Psychic, Necrosis, Water, Earth, Fire, and Air resistance by 0.5 per point above 1.")]
    [Min(1)] public int willpower = 1;
}

[RequireComponent(typeof(PlayerStats))]
public class PlayerAttributesNTraits : MonoBehaviour
{
    [Header("Allocation Points")]
    [Min(0)] [SerializeField] private int starterAttributePoints = 5;
    [Min(0)] [SerializeField] private int starterTraitPoints = 5;
    [SerializeField] private int availableAttributePoints;
    [SerializeField] private int availableTraitPoints;

    [Header("Primary Attributes")]
    [Min(0)]
    [SerializeField] private int strength = 5;

    [Min(0)]
    [SerializeField] private int dexterity = 5;

    [Min(0)]
    [SerializeField] private int intelligence = 5;

    [Header("Secondary Traits")]
    [Min(1)] [SerializeField] private int vitality = 1;
    [Min(1)] [SerializeField] private int focus = 1;
    [Min(1)] [SerializeField] private int endurance = 1;
    [Min(1)] [SerializeField] private int agility = 1;
    [Min(1)] [SerializeField] private int vigor = 1;
    [Min(1)] [SerializeField] private int haste = 1;
    [Min(1)] [SerializeField] private int attunement = 1;
    [Min(1)] [SerializeField] private int mundane = 1;
    [Min(1)] [SerializeField] private int arcane = 1;
    [Min(1)] [SerializeField] private int elemental = 1;
    [Min(1)] [SerializeField] private int precision = 1;
    [Min(1)] [SerializeField] private int fortitude = 1;
    [Min(1)] [SerializeField] private int willpower = 1;

    [Header("Class Defaults")]
    [SerializeField] private AttributeTraitDefaults warriorDefaults = new AttributeTraitDefaults
    {
        strength = 7,
        vitality = 2,
        vigor = 2,
        mundane = 2,
        fortitude = 2
    };

    [SerializeField] private AttributeTraitDefaults rangerDefaults = new AttributeTraitDefaults
    {
        dexterity = 7,
        agility = 2,
        haste = 2,
        precision = 2
    };

    [SerializeField] private AttributeTraitDefaults mageDefaults = new AttributeTraitDefaults
    {
        intelligence = 7,
        focus = 2,
        attunement = 2,
        arcane = 2,
        elemental = 2,
        willpower = 2
    };

    public int Strength => strength;
    public int Dexterity => dexterity;
    public int Intelligence => intelligence;
    public int Vitality => vitality;
    public int Focus => focus;
    public int Endurance => endurance;
    public int Agility => agility;
    public int Vigor => vigor;
    public int Haste => haste;
    public int Attunement => attunement;
    public int Mundane => mundane;
    public int Arcane => arcane;
    public int Elemental => elemental;
    public int Precision => precision;
    public int Fortitude => fortitude;
    public int Willpower => willpower;
    public int AvailableAttributePoints => availableAttributePoints;
    public int AvailableTraitPoints => availableTraitPoints;

    public event Action<PlayerAttributesNTraits> Changed;

    public float MaxHealthModifier => (strength - 5) * 7f + (vitality - 1) * 2f;
    public float MaxManaModifier => (intelligence - 5) * 7f + (focus - 1) * 2f;
    public float MaxStaminaModifier => (dexterity - 5) * 7f + (agility - 1) * 2f;
    public float HealthRegenModifier => (vigor - 1) * 0.2f;
    public float ManaRegenModifier => (attunement - 1) * 0.2f;
    public float StaminaRegenModifier => (endurance - 1) * 0.2f;
    public float MovementSpeedModifier => (haste - 1) * 0.02f;
    public float DamageResistanceModifier => (fortitude - 1) * 0.5f;
    public float MagicalResistanceModifier => (willpower - 1) * 0.5f;

    private void Awake()
    {
        availableAttributePoints = Mathf.Max(0, starterAttributePoints);
        availableTraitPoints = Mathf.Max(0, starterTraitPoints);
    }

    public void ApplyClassDefaults(PlayerClass playerClass)
    {
        AttributeTraitDefaults defaults = playerClass switch
        {
            PlayerClass.Warrior => warriorDefaults,
            PlayerClass.Ranger => rangerDefaults,
            PlayerClass.Mage => mageDefaults,
            _ => null
        };

        if (defaults == null)
            return;

        strength = defaults.strength;
        dexterity = defaults.dexterity;
        intelligence = defaults.intelligence;
        vitality = defaults.vitality;
        focus = defaults.focus;
        endurance = defaults.endurance;
        agility = defaults.agility;
        vigor = defaults.vigor;
        haste = defaults.haste;
        attunement = defaults.attunement;
        mundane = defaults.mundane;
        arcane = defaults.arcane;
        elemental = defaults.elemental;
        precision = defaults.precision;
        fortitude = defaults.fortitude;
        willpower = defaults.willpower;
        availableAttributePoints = Mathf.Max(0, starterAttributePoints);
        availableTraitPoints = Mathf.Max(0, starterTraitPoints);
        Changed?.Invoke(this);
    }

    public bool TryAllocate(PrimaryAttribute attribute)
    {
        if (availableAttributePoints <= 0)
            return false;

        switch (attribute)
        {
            case PrimaryAttribute.Strength:
                strength++;
                break;
            case PrimaryAttribute.Dexterity:
                dexterity++;
                break;
            case PrimaryAttribute.Intelligence:
                intelligence++;
                break;
            default:
                return false;
        }

        availableAttributePoints--;
        Changed?.Invoke(this);
        return true;
    }

    public bool TryAllocate(SecondaryTrait trait)
    {
        if (availableTraitPoints <= 0)
            return false;

        switch (trait)
        {
            case SecondaryTrait.Vitality: vitality++; break;
            case SecondaryTrait.Focus: focus++; break;
            case SecondaryTrait.Endurance: endurance++; break;
            case SecondaryTrait.Agility: agility++; break;
            case SecondaryTrait.Vigor: vigor++; break;
            case SecondaryTrait.Haste: haste++; break;
            case SecondaryTrait.Attunement: attunement++; break;
            case SecondaryTrait.Mundane: mundane++; break;
            case SecondaryTrait.Arcane: arcane++; break;
            case SecondaryTrait.Elemental: elemental++; break;
            case SecondaryTrait.Precision: precision++; break;
            case SecondaryTrait.Fortitude: fortitude++; break;
            case SecondaryTrait.Willpower: willpower++; break;
            default: return false;
        }

        availableTraitPoints--;
        Changed?.Invoke(this);
        return true;
    }

    public float GetDamageModifier(DamageType damageType)
    {
        float strengthModifier = strength - 5;
        float dexterityModifier = dexterity - 5;
        float intelligenceModifier = intelligence - 5;
        float modifier = 0f;

        if ((damageType & (DamageType.Slash | DamageType.Blunt | DamageType.Physical)) != 0)
            modifier += strengthModifier * 5f;

        if ((damageType & (DamageType.Pierce | DamageType.Stab)) != 0)
            modifier += dexterityModifier * 5f;

        if ((damageType & (DamageType.Slash | DamageType.Blunt | DamageType.Stab | DamageType.Physical)) != 0)
            modifier += mundane - 1;

        if ((damageType & DamageType.Pierce) != 0)
            modifier += precision - 1;

        if ((damageType & (DamageType.Burning | DamageType.Frost | DamageType.Poison |
                           DamageType.Lightning | DamageType.Psychic | DamageType.Necrosis |
                           DamageType.Water | DamageType.Earth | DamageType.Fire | DamageType.Air)) != 0)
            modifier += intelligenceModifier * 3f;

        if ((damageType & (DamageType.Poison | DamageType.Lightning | DamageType.Frost |
                           DamageType.Psychic | DamageType.Necrosis)) != 0)
            modifier += arcane - 1;

        if ((damageType & (DamageType.Water | DamageType.Earth | DamageType.Fire | DamageType.Air)) != 0)
            modifier += elemental - 1;

        return modifier;
    }

}