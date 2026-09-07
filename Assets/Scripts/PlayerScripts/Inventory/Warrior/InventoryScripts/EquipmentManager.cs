using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton manager handling character equipment slots, equip/unequip state,
/// and equipment change event notification.
/// </summary>
public class EquipmentManager : MonoBehaviour
{
    #region Singleton
    /// <summary>Gets the active global singleton instance of the EquipmentManager.</summary>
    public static EquipmentManager Instance { get; private set; }
    #endregion

    #region Serialized Fields
    [Header("Prefab References")]
    [Tooltip("UI item visual prefab instantiated inside equipment slot.")]
    [SerializeField] private GameObject itemPrefab;
    #endregion

    #region Private Fields
    private readonly Dictionary<EquipmentType, InventoryItem> currentEquipment = new();
    #endregion

    #region Properties
    /// <summary>Get assigned UI item visual prefab reference.</summary>
    public GameObject ItemPrefab => itemPrefab;
    #endregion

    #region Events
    /// <summary>Fired when an equipment slot's assigned item is updated or cleared.</summary>
    public event Action<EquipmentType, InventoryItem> OnEquipmentChanged;
    #endregion

    #region Lifecycle
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
    #endregion

    #region Public API
    /// <summary>
    /// Retrieves the active item equipped in the specified equipment slot type.
    /// </summary>
    /// <param name="type">Equipment slot type.</param>
    /// <returns>Equipped inventory item instance if occupied; otherwise, null.</returns>
    public InventoryItem GetEquippedItem(EquipmentType type)
    {
        currentEquipment.TryGetValue(type, out var item);
        return item;
    }

    /// <summary>
    /// Assigns an item to the specified equipment slot after validating equipment type compatibility.
    /// </summary>
    /// <param name="type">Equipment slot type.</param>
    /// <param name="newItem">Item instance attempting to equip.</param>
    /// <param name="previousItem">Outputs item previously occupying slot.</param>
    /// <returns>True if equip requirements were met and state updated; otherwise, false.</returns>
    public bool Equip(EquipmentType type, InventoryItem newItem, out InventoryItem previousItem)
    {
        previousItem = GetEquippedItem(type);

        // Validate item data existence and slot type compatibility
        if (newItem == null || newItem.Data == null || newItem.Data.EquipmentType != type)
        {
            return false;
        }

        if (!CanEquip(newItem.Data))
        {
            return false;
        }

        currentEquipment[type] = newItem;
        OnEquipmentChanged?.Invoke(type, newItem);
        NotifyPlayerStatsChanged();
        return true;
    }

    public bool CanEquip(ItemData itemData)
    {
        if (itemData == null)
            return false;

        PlayerAttributesNTraits attributes = FindFirstObjectByType<PlayerAttributesNTraits>();
        return itemData.MeetsStatCap(attributes);
    }

    /// <summary>
    /// Clears equipment slot and returns the unequipped item instance.
    /// </summary>
    /// <param name="type">Equipment slot type to clear.</param>
    /// <returns>Unequipped item instance if slot was occupied; otherwise, null.</returns>
    public InventoryItem Unequip(EquipmentType type)
    {
        if (currentEquipment.TryGetValue(type, out var item) && item != null)
        {
            currentEquipment.Remove(type);
            OnEquipmentChanged?.Invoke(type, null);
            NotifyPlayerStatsChanged();
            return item;
        }

        return null;
    }

    private void NotifyPlayerStatsChanged()
    {
        PlayerAttributesNTraits attributes = FindFirstObjectByType<PlayerAttributesNTraits>();
        attributes?.NotifyEquipmentChanged();

        PlayerStats playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats != null && attributes == null)
            playerStats.NotifyStatsChanged();
    }

    /// <summary>
    /// Checks whether a specific item is currently equipped in a slot.
    /// </summary>
    /// <param name="item">Target item</param>
    /// <returns>True if item instance is tracked in equipment memory; otherwise, false.</returns>
    public bool IsEquipped(InventoryItem item)
    {
        if (item == null) return false;
        return currentEquipment.ContainsValue(item);
    }

    public float GetModifiedStat(float baseValue, StatType statType, DamageType damageType = DamageType.None)
    {
        float flat = 0f;
        float percent = 0f;

        foreach (InventoryItem item in currentEquipment.Values)
        {
            if (item == null || item.Data == null)
                continue;

            foreach (EquipmentStat modifier in item.Data.StatModifiers)
            {
                if (modifier.statType != statType)
                    continue;

                if (statType != StatType.MoveSpeed &&
                    modifier.damageType != DamageType.None &&
                    (modifier.damageType & damageType) == 0)
                    continue;

                if (modifier.modifierType == StatModifierType.Percent)
                    percent += modifier.value;
                else
                    flat += modifier.value;
            }
        }

        return (baseValue + flat) * (1f + percent / 100f);
    }

    /// <summary>
    /// Same as GetModifiedStat, but skips the given item's modifiers.
    /// Used by charged skills so the weapon's own damage modifier
    /// (added separately) isn't double-counted while other equipped
    /// gear still contributes.
    /// </summary>
    public float GetModifiedStatExcluding(float baseValue, StatType statType, DamageType damageType, ItemData excluded)
    {
        float flat = 0f;
        float percent = 0f;

        foreach (InventoryItem item in currentEquipment.Values)
        {
            if (item == null || item.Data == null || item.Data == excluded)
                continue;

            foreach (EquipmentStat modifier in item.Data.StatModifiers)
            {
                if (modifier.statType != statType)
                    continue;

                if (statType != StatType.MoveSpeed &&
                    modifier.damageType != DamageType.None &&
                    (modifier.damageType & damageType) == 0)
                    continue;

                if (modifier.modifierType == StatModifierType.Percent)
                    percent += modifier.value;
                else
                    flat += modifier.value;
            }
        }

        return (baseValue + flat) * (1f + percent / 100f);
    }

    public float GetAttributeReduction(PrimaryAttribute attribute, float baseValue)
    {
        return ApplyReduction(baseValue, StatType.AttributeReduction, modifier => modifier.reducedAttribute == attribute);
    }

    public float GetTraitReduction(SecondaryTrait trait, float baseValue)
    {
        return ApplyReduction(baseValue, StatType.TraitReduction, modifier => modifier.reducedTrait == trait);
    }

    private float ApplyReduction(float baseValue, StatType statType, Func<EquipmentStat, bool> targetFilter)
    {
        float flat = 0f;
        float percent = 0f;

        foreach (InventoryItem item in currentEquipment.Values)
        {
            if (item == null || item.Data == null)
                continue;

            foreach (EquipmentStat modifier in item.Data.StatModifiers)
            {
                if (modifier.statType != statType || !targetFilter(modifier))
                    continue;

                if (modifier.modifierType == StatModifierType.Percent)
                    percent += modifier.value;
                else
                    flat += modifier.value;
            }
        }

        return Mathf.Max(0f, (baseValue - flat) * (1f - percent / 100f));
    }

    #endregion
}