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

    [Header("Value Text")]
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

    [Header("UI Suppression")]
    [SerializeField] private UISuppressor[] suppressedUIs;
    [Tooltip("GameObjects with UISuppressor components that must remain unsuppressed.")]
    [SerializeField] private GameObject[] exceptions;

    private PlayerAttributesNTraits attributes;
    private PlayerInventory inventory;
    private bool subscribed;
    public event Action PendingAllocationsChanged;
    private readonly Dictionary<PrimaryAttribute, int> pendingAttributes =
        new Dictionary<PrimaryAttribute, int>();
    private readonly Dictionary<SecondaryTrait, int> pendingTraits =
        new Dictionary<SecondaryTrait, int>();

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

        SubscribeToAttributes();
    }

    private void Start()
    {
        ResolvePlayerReferences();
        ResolveInventoryReference();
        SubscribeToAttributes();
        SetStatsWindowState(startOpen);
        Refresh(attributes);
    }

    private void OnDisable()
    {
        if (statsAction != null)
        {
            statsAction.action.performed -= OnStatsPerformed;
            statsAction.action.Disable();
        }

        if (subscribed && attributes != null)
            attributes.Changed -= Refresh;

        subscribed = false;
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


        PlayerStats playerStats = Player.Instance != null
            ? Player.Instance.GetComponent<PlayerStats>()
            : null;

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
        Refresh(attributes);
        PendingAllocationsChanged?.Invoke();
    }

    private int GetRemainingAttributePoints()
    {
        int pending = 0;
        foreach (int value in pendingAttributes.Values)
            pending += value;

        return attributes.AvailableAttributePoints - pending;
    }

    private int GetRemainingTraitPoints()
    {
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

    private void SubscribeToAttributes()
    {
        if (!subscribed && attributes != null)
        {
            attributes.Changed += Refresh;
            subscribed = true;
        }
    }

    private void Refresh(PlayerAttributesNTraits source)
    {
        if (source == null)
            return;

        if (attributePointsText != null)
            attributePointsText.text = $"Attribute Points: {GetRemainingAttributePoints()}";

        if (traitPointsText != null)
            traitPointsText.text = $"Trait Points: {GetRemainingTraitPoints()}";

        SetValue(strengthText, source.Strength, GetPendingAttribute(PrimaryAttribute.Strength));
        SetValue(dexterityText, source.Dexterity, GetPendingAttribute(PrimaryAttribute.Dexterity));
        SetValue(intelligenceText, source.Intelligence, GetPendingAttribute(PrimaryAttribute.Intelligence));
        SetValue(vitalityText, source.Vitality, GetPendingTrait(SecondaryTrait.Vitality));
        SetValue(focusText, source.Focus, GetPendingTrait(SecondaryTrait.Focus));
        SetValue(enduranceText, source.Endurance, GetPendingTrait(SecondaryTrait.Endurance));
        SetValue(agilityText, source.Agility, GetPendingTrait(SecondaryTrait.Agility));
        SetValue(vigorText, source.Vigor, GetPendingTrait(SecondaryTrait.Vigor));
        SetValue(hasteText, source.Haste, GetPendingTrait(SecondaryTrait.Haste));
        SetValue(attunementText, source.Attunement, GetPendingTrait(SecondaryTrait.Attunement));
        SetValue(mundaneText, source.Mundane, GetPendingTrait(SecondaryTrait.Mundane));
        SetValue(arcaneText, source.Arcane, GetPendingTrait(SecondaryTrait.Arcane));
        SetValue(elementalText, source.Elemental, GetPendingTrait(SecondaryTrait.Elemental));
        SetValue(precisionText, source.Precision, GetPendingTrait(SecondaryTrait.Precision));
        SetValue(fortitudeText, source.Fortitude, GetPendingTrait(SecondaryTrait.Fortitude));
        SetValue(willpowerText, source.Willpower, GetPendingTrait(SecondaryTrait.Willpower));
    }

    private static void SetValue(TMP_Text target, int currentValue, int allocatedValue)
    {
        if (target != null)
            target.text = allocatedValue > 0
                ? $"{currentValue} <color=green>+ {allocatedValue}</color>"
                : currentValue.ToString();
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
