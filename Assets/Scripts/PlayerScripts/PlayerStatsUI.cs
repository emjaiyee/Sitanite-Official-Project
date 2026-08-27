using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerStatsUI : MonoBehaviour
{
    [Header("Window")]
    [SerializeField] private GameObject statsWindow;
    [SerializeField] private InputActionReference statsAction;
    [SerializeField] private bool startOpen = false;

    [Header("Point Text")]
    [SerializeField] private TMP_Text attributePointsText;
    [SerializeField] private TMP_Text traitPointsText;

    [Header("Attributes & Traits Value Text")]
    [SerializeField] private TMP_Text strengthText;
    [SerializeField] private TMP_Text dexterityText;
    [SerializeField] private TMP_Text intelligenceText;
    [SerializeField] private TMP_Text vitalityText;
    [SerializeField] private TMP_Text focusText;
    [SerializeField] private TMP_Text enduranceText;
    [SerializeField] private TMP_Text agilityText;
    [SerializeField] private TMP_Text vigorText;
    [SerializeField] private TMP_Text hasteText;
    [SerializeField] private TMP_Text attunementText;
    [SerializeField] private TMP_Text mundaneText;
    [SerializeField] private TMP_Text arcaneText;
    [SerializeField] private TMP_Text elementalText;
    [SerializeField] private TMP_Text precisionText;
    [SerializeField] private TMP_Text fortitudeText;
    [SerializeField] private TMP_Text willpowerText;

    [Header("Player Resources UI")]
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text manaText;
    [SerializeField] private TMP_Text staminaText;

    [Header("Player Movement UI")]
    [SerializeField] private TMP_Text moveSpeedText;
    [SerializeField] private TMP_Text sprintSpeedText;
    [SerializeField] private TMP_Text dashSpeedText;

    [Header("Player Regen UI")]
    [SerializeField] private TMP_Text healthRegenText;
    [SerializeField] private TMP_Text manaRegenText;
    [SerializeField] private TMP_Text staminaRegenText;

    [Header("Player Damage UI")]
    [SerializeField] private TMP_Text pierceDamageText;
    [SerializeField] private TMP_Text stabDamageText;
    [SerializeField] private TMP_Text slashDamageText;
    [SerializeField] private TMP_Text bluntDamageText;
    [SerializeField] private TMP_Text physicalDamageText;
    [SerializeField] private TMP_Text frostDamageText;
    [SerializeField] private TMP_Text poisonDamageText;
    [SerializeField] private TMP_Text lightningDamageText;
    [SerializeField] private TMP_Text psychicDamageText;
    [SerializeField] private TMP_Text necrosisDamageText;
    [SerializeField] private TMP_Text waterDamageText;
    [SerializeField] private TMP_Text earthDamageText;
    [SerializeField] private TMP_Text fireDamageText;
    [SerializeField] private TMP_Text airDamageText;

    [Header("Player Resistance UI")]
    [SerializeField] private TMP_Text pierceResistanceText;
    [SerializeField] private TMP_Text stabResistanceText;
    [SerializeField] private TMP_Text slashResistanceText;
    [SerializeField] private TMP_Text bluntResistanceText;
    [SerializeField] private TMP_Text physicalResistanceText;
    [SerializeField] private TMP_Text frostResistanceText;
    [SerializeField] private TMP_Text poisonResistanceText;
    [SerializeField] private TMP_Text lightningResistanceText;
    [SerializeField] private TMP_Text psychicResistanceText;
    [SerializeField] private TMP_Text necrosisResistanceText;
    [SerializeField] private TMP_Text waterResistanceText;
    [SerializeField] private TMP_Text earthResistanceText;
    [SerializeField] private TMP_Text fireResistanceText;
    [SerializeField] private TMP_Text airResistanceText;

    [Header("UI Suppression")]
    [SerializeField] private UISuppressor[] suppressedUIs;
    [Tooltip("GameObjects with UISuppressor components that must remain unsuppressed.")]
    [SerializeField] private GameObject[] exceptions;

    private PlayerAttributesNTraits attributes;
    private PlayerStats playerStats;
    private PlayerInventory inventory;
    private bool subscribed;
    public event Action PendingAllocationsChanged;
    private readonly Dictionary<PrimaryAttribute, int> pendingAttributes = new Dictionary<PrimaryAttribute, int>();
    private readonly Dictionary<SecondaryTrait, int> pendingTraits = new Dictionary<SecondaryTrait, int>();

    private void Awake()
    {
        ResolvePlayerReferences();
        ResolveInventoryReference();
        AssignSuppressedUIs();
    }

    private void OnEnable()
    {
        if (statsAction != null)
        {
            statsAction.action.Enable();
            statsAction.action.performed += OnStatsPerformed;
        }

        SubscribeToPlayerEvents();
    }

    private void Start()
    {
        ResolvePlayerReferences();
        ResolveInventoryReference();
        SubscribeToPlayerEvents();
        SetStatsWindowState(startOpen);
        RefreshAllUI();
    }

    private void OnDisable()
    {
        if (statsAction != null)
        {
            statsAction.action.performed -= OnStatsPerformed;
            statsAction.action.Disable();
        }

        UnsubscribeFromPlayerEvents();
    }

    private void OnStatsPerformed(InputAction.CallbackContext context)
    {
        ToggleStatsWindow();
    }

    public void ToggleStatsWindow()
    {
        SetStatsWindowState(!IsOpen);
    }

    public void SetStatsWindowState(bool isOpen)
    {
        if (statsWindow == null)
            return;

        if (isOpen && inventory != null && inventory.IsOpen)
            inventory.SetInventoryState(false);

        statsWindow.SetActive(isOpen);
        SetSuppressedUIState(isOpen);

        if (isOpen)
            RefreshAllUI();
    }

    public bool IsOpen => statsWindow != null && statsWindow.activeSelf;

    public bool AllocateAttribute(PrimaryAttribute attribute)
    {
        if (attributes == null || GetRemainingAttributePoints() <= 0)
            return false;

        AddPendingAttribute(attribute, 1);
        NotifyPendingAllocationsChanged();
        return true;
    }

    public bool AllocateTrait(SecondaryTrait trait)
    {
        if (attributes == null || GetRemainingTraitPoints() <= 0)
            return false;

        AddPendingTrait(trait, 1);
        NotifyPendingAllocationsChanged();
        return true;
    }

    public bool ReturnAttribute(PrimaryAttribute attribute)
    {
        if (!pendingAttributes.TryGetValue(attribute, out int pending) || pending <= 0)
            return false;

        pendingAttributes[attribute] = pending - 1;
        NotifyPendingAllocationsChanged();
        return true;
    }

    public bool ReturnTrait(SecondaryTrait trait)
    {
        if (!pendingTraits.TryGetValue(trait, out int pending) || pending <= 0)
            return false;

        pendingTraits[trait] = pending - 1;
        NotifyPendingAllocationsChanged();
        return true;
    }

    public int GetPendingAttribute(PrimaryAttribute attribute)
    {
        return pendingAttributes.TryGetValue(attribute, out int value) ? value : 0;
    }

    public int GetPendingTrait(SecondaryTrait trait)
    {
        return pendingTraits.TryGetValue(trait, out int value) ? value : 0;
    }

    public bool HasPendingAttribute(PrimaryAttribute attribute)
    {
        return GetPendingAttribute(attribute) > 0;
    }

    public bool HasPendingTrait(SecondaryTrait trait)
    {
        return GetPendingTrait(trait) > 0;
    }

    public void ApplyAllocatedPoints()
    {
        if (attributes == null)
            return;

        foreach (KeyValuePair<PrimaryAttribute, int> allocation in pendingAttributes)
        {
            for (int i = 0; i < allocation.Value; i++)
                attributes.TryAllocate(allocation.Key);
        }

        foreach (KeyValuePair<SecondaryTrait, int> allocation in pendingTraits)
        {
            for (int i = 0; i < allocation.Value; i++)
                attributes.TryAllocate(allocation.Key);
        }

        if (playerStats != null)
            playerStats.NotifyStatsChanged();

        ClearPendingAllocations();
    }

    public void ResetAllocatedPoints()
    {
        ClearPendingAllocations();
    }

    private void ClearPendingAllocations()
    {
        pendingAttributes.Clear();
        pendingTraits.Clear();
        NotifyPendingAllocationsChanged();
    }

    private void NotifyPendingAllocationsChanged()
    {
        RefreshAllUI();
        PendingAllocationsChanged?.Invoke();
    }

    private int GetRemainingAttributePoints()
    {
        if (attributes == null) return 0;
        int pending = 0;
        foreach (int value in pendingAttributes.Values)
            pending += value;

        return attributes.AvailableAttributePoints - pending;
    }

    private int GetRemainingTraitPoints()
    {
        if (attributes == null) return 0;
        int pending = 0;
        foreach (int value in pendingTraits.Values)
            pending += value;

        return attributes.AvailableTraitPoints - pending;
    }

    private void AddPendingAttribute(PrimaryAttribute attribute, int amount)
    {
        pendingAttributes[attribute] = GetPendingAttribute(attribute) + amount;
    }

    private void AddPendingTrait(SecondaryTrait trait, int amount)
    {
        pendingTraits[trait] = GetPendingTrait(trait) + amount;
    }

    private void ResolvePlayerReferences()
    {
        if (Player.Instance == null)
            return;

        attributes = Player.Instance.GetComponent<PlayerAttributesNTraits>();
        playerStats = Player.Instance.GetComponent<PlayerStats>();
    }

    private void ResolveInventoryReference()
    {
        if (Player.Instance != null)
            inventory = Player.Instance.GetComponent<PlayerInventory>();
    }

    private void AssignSuppressedUIs()
    {
        UISuppressor[] sceneSuppressors = FindObjectsByType<UISuppressor>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        List<UISuppressor> eligibleSuppressors = new List<UISuppressor>();
        foreach (UISuppressor suppressor in sceneSuppressors)
        {
            if (suppressor != null && !IsException(suppressor.gameObject))
                eligibleSuppressors.Add(suppressor);
        }

        suppressedUIs = eligibleSuppressors.ToArray();
    }

    private bool IsException(GameObject target)
    {
        if (exceptions == null)
            return false;

        foreach (GameObject exception in exceptions)
        {
            if (exception == target)
                return true;
        }

        return false;
    }

    private void SubscribeToPlayerEvents()
    {
        if (subscribed) return;

        if (attributes != null)
            attributes.Changed += OnAttributesChanged;

        if (playerStats != null)
            playerStats.Changed += OnPlayerStatsChanged;

        subscribed = true;
    }

    private void UnsubscribeFromPlayerEvents()
    {
        if (!subscribed) return;

        if (attributes != null)
            attributes.Changed -= OnAttributesChanged;

        if (playerStats != null)
            playerStats.Changed -= OnPlayerStatsChanged;

        subscribed = false;
    }

    private void OnAttributesChanged(PlayerAttributesNTraits source)
    {
        RefreshAllUI();
    }

    private void OnPlayerStatsChanged(PlayerStats source)
    {
        RefreshAllUI();
    }

    private void RefreshAllUI()
    {
        RefreshAttributesAndTraits();
        RefreshPlayerStats();
    }

    private void RefreshAttributesAndTraits()
    {
        if (attributes == null)
            return;

        if (attributePointsText != null)
            attributePointsText.text = $"Attribute Points: {GetRemainingAttributePoints()}";

        if (traitPointsText != null)
            traitPointsText.text = $"Trait Points: {GetRemainingTraitPoints()}";

        SetValue(strengthText, attributes.Strength, GetPendingAttribute(PrimaryAttribute.Strength));
        SetValue(dexterityText, attributes.Dexterity, GetPendingAttribute(PrimaryAttribute.Dexterity));
        SetValue(intelligenceText, attributes.Intelligence, GetPendingAttribute(PrimaryAttribute.Intelligence));
        SetValue(vitalityText, attributes.Vitality, GetPendingTrait(SecondaryTrait.Vitality));
        SetValue(focusText, attributes.Focus, GetPendingTrait(SecondaryTrait.Focus));
        SetValue(enduranceText, attributes.Endurance, GetPendingTrait(SecondaryTrait.Endurance));
        SetValue(agilityText, attributes.Agility, GetPendingTrait(SecondaryTrait.Agility));
        SetValue(vigorText, attributes.Vigor, GetPendingTrait(SecondaryTrait.Vigor));
        SetValue(hasteText, attributes.Haste, GetPendingTrait(SecondaryTrait.Haste));
        SetValue(attunementText, attributes.Attunement, GetPendingTrait(SecondaryTrait.Attunement));
        SetValue(mundaneText, attributes.Mundane, GetPendingTrait(SecondaryTrait.Mundane));
        SetValue(arcaneText, attributes.Arcane, GetPendingTrait(SecondaryTrait.Arcane));
        SetValue(elementalText, attributes.Elemental, GetPendingTrait(SecondaryTrait.Elemental));
        SetValue(precisionText, attributes.Precision, GetPendingTrait(SecondaryTrait.Precision));
        SetValue(fortitudeText, attributes.Fortitude, GetPendingTrait(SecondaryTrait.Fortitude));
        SetValue(willpowerText, attributes.Willpower, GetPendingTrait(SecondaryTrait.Willpower));
    }

    private void RefreshPlayerStats()
    {
        if (playerStats == null)
            return;

        // Resources
        SetText(healthText, "Health", playerStats.MaxHealth, playerStats.PreEquipmentMaxHealth, $"{Mathf.CeilToInt(playerStats.CurrentHealth)} / ");
        SetText(manaText, "Mana", playerStats.MaxMana, playerStats.PreEquipmentMaxMana, $"{Mathf.CeilToInt(playerStats.CurrentMana)} / ");
        SetText(staminaText, "Stamina", playerStats.MaxStamina, playerStats.PreEquipmentMaxStamina, $"{Mathf.CeilToInt(playerStats.CurrentStamina)} / ");

        // Movement
        SetText(moveSpeedText, "MoveSpeed", playerStats.MoveSpeed, playerStats.PreEquipmentMoveSpeed, "F1");
        SetText(sprintSpeedText, "SprintSpeed", playerStats.SprintSpeed, playerStats.PreEquipmentSprintSpeed, "F1");
        SetText(dashSpeedText, "DashSpeed", playerStats.DashSpeed, playerStats.PreEquipmentDashSpeed, "F1");

        // Regeneration
        SetText(healthRegenText, "HealthRegen", playerStats.HealthRegen, playerStats.PreEquipmentHealthRegen, "F1", "/s");
        SetText(manaRegenText, "ManaRegen", playerStats.ManaRegen, playerStats.PreEquipmentManaRegen, "F1", "/s");
        SetText(staminaRegenText, "StaminaRegen", playerStats.StaminaRegen, playerStats.PreEquipmentStaminaRegen, "F1", "/s");

        // Base/Effective Damage
        SetDamage(pierceDamageText, "Pierce", playerStats.PierceDamage, playerStats.GetPreEquipmentDamage(DamageType.Pierce));
        SetDamage(stabDamageText, "Stab", playerStats.StabDamage, playerStats.GetPreEquipmentDamage(DamageType.Stab));
        SetDamage(slashDamageText, "Slash", playerStats.SlashDamage, playerStats.GetPreEquipmentDamage(DamageType.Slash));
        SetDamage(bluntDamageText, "Blunt", playerStats.BluntDamage, playerStats.GetPreEquipmentDamage(DamageType.Blunt));
        SetDamage(physicalDamageText, "Physical", playerStats.PhysicalDamage, playerStats.GetPreEquipmentDamage(DamageType.Physical));
        SetDamage(frostDamageText, "Frost", playerStats.FrostDamage, playerStats.GetPreEquipmentDamage(DamageType.Frost));
        SetDamage(poisonDamageText, "Poison", playerStats.PoisonDamage, playerStats.GetPreEquipmentDamage(DamageType.Poison));
        SetDamage(lightningDamageText, "Lightning", playerStats.LightningDamage, playerStats.GetPreEquipmentDamage(DamageType.Lightning));
        SetDamage(psychicDamageText, "Psychic", playerStats.PsychicDamage, playerStats.GetPreEquipmentDamage(DamageType.Psychic));
        SetDamage(necrosisDamageText, "Necrosis", playerStats.NecrosisDamage, playerStats.GetPreEquipmentDamage(DamageType.Necrosis));
        SetDamage(waterDamageText, "Water", playerStats.WaterDamage, playerStats.GetPreEquipmentDamage(DamageType.Water));
        SetDamage(earthDamageText, "Earth", playerStats.EarthDamage, playerStats.GetPreEquipmentDamage(DamageType.Earth));
        SetDamage(fireDamageText, "Fire", playerStats.FireDamage, playerStats.GetPreEquipmentDamage(DamageType.Fire));
        SetDamage(airDamageText, "Air", playerStats.AirDamage, playerStats.GetPreEquipmentDamage(DamageType.Air));

        // Resistance
        SetDamage(pierceResistanceText, "Pierce Res", playerStats.PierceResistance, playerStats.GetPreEquipmentResistance(DamageType.Pierce));
        SetDamage(stabResistanceText, "Stab Res", playerStats.StabResistance, playerStats.GetPreEquipmentResistance(DamageType.Stab));
        SetDamage(slashResistanceText, "Slash Res", playerStats.SlashResistance, playerStats.GetPreEquipmentResistance(DamageType.Slash));
        SetDamage(bluntResistanceText, "Blunt Res", playerStats.BluntResistance, playerStats.GetPreEquipmentResistance(DamageType.Blunt));
        SetDamage(physicalResistanceText, "Physical Res", playerStats.PhysicalResistance, playerStats.GetPreEquipmentResistance(DamageType.Physical));
        SetDamage(frostResistanceText, "Frost Res", playerStats.FrostResistance, playerStats.GetPreEquipmentResistance(DamageType.Frost));
        SetDamage(poisonResistanceText, "Poison Res", playerStats.PoisonResistance, playerStats.GetPreEquipmentResistance(DamageType.Poison));
        SetDamage(lightningResistanceText, "Lightning Res", playerStats.LightningResistance, playerStats.GetPreEquipmentResistance(DamageType.Lightning));
        SetDamage(psychicResistanceText, "Psychic Res", playerStats.PsychicResistance, playerStats.GetPreEquipmentResistance(DamageType.Psychic));
        SetDamage(necrosisResistanceText, "Necrosis Res", playerStats.NecrosisResistance, playerStats.GetPreEquipmentResistance(DamageType.Necrosis));
        SetDamage(waterResistanceText, "Water Res", playerStats.WaterResistance, playerStats.GetPreEquipmentResistance(DamageType.Water));
        SetDamage(earthResistanceText, "Earth Res", playerStats.EarthResistance, playerStats.GetPreEquipmentResistance(DamageType.Earth));
        SetDamage(fireResistanceText, "Fire Res", playerStats.FireResistance, playerStats.GetPreEquipmentResistance(DamageType.Fire));
        SetDamage(airResistanceText, "Air Res", playerStats.AirResistance, playerStats.GetPreEquipmentResistance(DamageType.Air));
    }

    // Main number includes base + attribute/trait modifiers; the colored
    // suffix is the equipment (ItemData) contribution only, e.g.
    // "Slash: 12 <color=green>(+5)</color>" or "Slash: 12 <color=red>(-3)</color>".
    private static void SetDamage(TMP_Text target, string label, float effective, float baseValue)
    {
        if (target == null)
            return;

        target.text = $"{label}: {baseValue:F0}{ModifierSuffix(effective, baseValue, "F0")}";
    }

    // For movement/regen where a format/suffix string is needed.
    private static void SetText(TMP_Text target, string label, float effective, float baseValue, string format, string suffix = "")
    {
        if (target == null)
            return;

        target.text = $"{label}: {baseValue.ToString(format)}{ModifierSuffix(effective, baseValue, format)}{suffix}";
    }

    // For resource lines that show "current / max".
    private static void SetText(TMP_Text target, string label, float effectiveMax, float baseMax, string currentPrefix)
    {
        if (target == null)
            return;

        target.text = $"{label}: {currentPrefix}{Mathf.CeilToInt(baseMax)}{ModifierSuffix(effectiveMax, baseMax, "F0")}";
    }

    // Builds the colored "(+N)" / "(-N)" combined-modifier suffix.
    // Returns empty when there is no meaningful difference.
    private static string ModifierSuffix(float effective, float baseValue, string format)
    {
        float delta = effective - baseValue;

        if (Mathf.Abs(delta) < 0.05f)
            return string.Empty;

        string magnitude = Mathf.Abs(delta).ToString(format);

        return delta > 0f
            ? $" <color=green>(+{magnitude})</color>"
            : $" <color=red>(-{magnitude})</color>";
    }

    private static void SetValue(TMP_Text target, int currentValue, int allocatedValue)
    {
        if (target != null)
            target.text = allocatedValue > 0
                ? $"{currentValue} <color=green>+ {allocatedValue}</color>"
                : currentValue.ToString();
    }

    private static void SetText(TMP_Text target, string text)
    {
        if (target != null)
            target.text = text;
    }

    private void SetSuppressedUIState(bool statsOpen)
    {
        if (suppressedUIs == null)
            return;

        foreach (UISuppressor suppressor in suppressedUIs)
        {
            if (suppressor == null)
                continue;

            if (statsOpen)
                suppressor.Suppress();
            else
                suppressor.Restore();
        }
    }
}