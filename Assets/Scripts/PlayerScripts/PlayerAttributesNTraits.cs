using System;
using UnityEngine;

[System.Serializable]
public class AttributeTraitDefaults
{
    [Tooltip("Increases Slash, Blunt, and Physical damage, plus maximum health.")]
    [Min(0)] public int strength = 5;
    [Tooltip("Increases Pierce and Stab damage, plus maximum stamina.")]
    [Min(0)] public int dexterity = 5;
    [Tooltip("Increases maximum mana by 7 per point above 5, and magical damage, including Frost, Poison, Lightning, Psychic, Necrosis, Water, Earth, Fire, and Air, by 3.")]
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

    public int Strength => GetAttributeValue(PrimaryAttribute.Strength);
    public int Dexterity => GetAttributeValue(PrimaryAttribute.Dexterity);
    public int Intelligence => GetAttributeValue(PrimaryAttribute.Intelligence);
    public int Vitality => GetTraitValue(SecondaryTrait.Vitality);
    public int Focus => GetTraitValue(SecondaryTrait.Focus);
    public int Endurance => GetTraitValue(SecondaryTrait.Endurance);
    public int Agility => GetTraitValue(SecondaryTrait.Agility);
    public int Vigor => GetTraitValue(SecondaryTrait.Vigor);
    public int Haste => GetTraitValue(SecondaryTrait.Haste);
    public int Attunement => GetTraitValue(SecondaryTrait.Attunement);
    public int Mundane => GetTraitValue(SecondaryTrait.Mundane);
    public int Arcane => GetTraitValue(SecondaryTrait.Arcane);
    public int Elemental => GetTraitValue(SecondaryTrait.Elemental);
    public int Precision => GetTraitValue(SecondaryTrait.Precision);
    public int Fortitude => GetTraitValue(SecondaryTrait.Fortitude);
    public int Willpower => GetTraitValue(SecondaryTrait.Willpower);
    public int AvailableAttributePoints => availableAttributePoints;
    public int AvailableTraitPoints => availableTraitPoints;

    public event Action<PlayerAttributesNTraits> Changed;

    public float MaxHealthModifier => (Strength - 5) * 7f + (Vitality - 1) * 2f;
    public float MaxManaModifier => (Intelligence - 5) * 7f + (Focus - 1) * 2f;
    public float MaxStaminaModifier => (Dexterity - 5) * 7f + (Agility - 1) * 2f;
    public float HealthRegenModifier => (Vigor - 1) * 0.2f;
    public float ManaRegenModifier => (Attunement - 1) * 0.2f;
    public float StaminaRegenModifier => (Endurance - 1) * 0.2f;
    public float MovementSpeedModifier => (Haste - 1) * 0.02f;
    public float CooldownReduction => (Haste - 1) * 0.002f;
    public float DamageResistanceModifier => (Fortitude - 1) * 0.5f;
    public float MagicalResistanceModifier => (Willpower - 1) * 0.5f;

    public void NotifyEquipmentChanged()
    {
        Changed?.Invoke(this);
    }

    public void AddAllocationPoints(int attributePoints, int traitPoints)
    {
        if (attributePoints <= 0 && traitPoints <= 0)
            return;

        availableAttributePoints = Mathf.Max(0, availableAttributePoints + attributePoints);
        availableTraitPoints = Mathf.Max(0, availableTraitPoints + traitPoints);
        Changed?.Invoke(this);
    }

    public int GetAttributeValue(PrimaryAttribute attribute)
    {
        int value = attribute switch
        {
            PrimaryAttribute.Strength => strength,
            PrimaryAttribute.Dexterity => dexterity,
            PrimaryAttribute.Intelligence => intelligence,
            _ => 0
        };

        return Mathf.FloorToInt(EquipmentManager.Instance == null
            ? value
            : EquipmentManager.Instance.GetAttributeReduction(attribute, value));
    }

    public int GetTraitValue(SecondaryTrait trait)
    {
        int value = trait switch
        {
            SecondaryTrait.Vitality => vitality,
            SecondaryTrait.Focus => focus,
            SecondaryTrait.Endurance => endurance,
            SecondaryTrait.Agility => agility,
            SecondaryTrait.Vigor => vigor,
            SecondaryTrait.Haste => haste,
            SecondaryTrait.Attunement => attunement,
            SecondaryTrait.Mundane => mundane,
            SecondaryTrait.Arcane => arcane,
            SecondaryTrait.Elemental => elemental,
            SecondaryTrait.Precision => precision,
            SecondaryTrait.Fortitude => fortitude,
            SecondaryTrait.Willpower => willpower,
            _ => 1
        };

        return Mathf.Max(1, Mathf.FloorToInt(EquipmentManager.Instance == null
            ? value
            : EquipmentManager.Instance.GetTraitReduction(trait, value)));
    }

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
        float strengthModifier = Strength - 5;
        float dexterityModifier = Dexterity - 5;
        float intelligenceModifier = Intelligence - 5;
        float modifier = 0f;

        if ((damageType & (DamageType.Slash | DamageType.Blunt | DamageType.Physical)) != 0)
            modifier += strengthModifier * 5f;

        if ((damageType & (DamageType.Pierce | DamageType.Stab)) != 0)
            modifier += dexterityModifier * 5f;

        if ((damageType & (DamageType.Slash | DamageType.Blunt | DamageType.Stab | DamageType.Physical)) != 0)
            modifier += Mundane - 1;

        if ((damageType & DamageType.Pierce) != 0)
            modifier += Precision - 1;

        if ((damageType & (DamageType.Frost | DamageType.Poison |
                           DamageType.Lightning | DamageType.Psychic | DamageType.Necrosis |
                           DamageType.Water | DamageType.Earth | DamageType.Fire | DamageType.Air)) != 0)
            modifier += intelligenceModifier * 3f;

        if ((damageType & (DamageType.Poison | DamageType.Lightning | DamageType.Frost |
                           DamageType.Psychic | DamageType.Necrosis)) != 0)
            modifier += Arcane - 1;

        if ((damageType & (DamageType.Water | DamageType.Earth | DamageType.Fire | DamageType.Air)) != 0)
            modifier += Elemental - 2;

        return modifier;
    }

}