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

        currentEquipment[type] = newItem;
        OnEquipmentChanged?.Invoke(type, newItem);
        return true;
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
            return item;
        }

        return null;
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
    #endregion
}