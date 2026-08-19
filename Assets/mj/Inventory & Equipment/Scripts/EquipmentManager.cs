using System;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance { get; private set; }

    [SerializeField] private GameObject itemPrefab;

    public GameObject ItemPrefab => itemPrefab;

    private readonly Dictionary<EquipmentType, InventoryItem> currentEquipment = new();

    public event Action<EquipmentType, InventoryItem> OnEquipmentChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public InventoryItem GetEquippedItem(EquipmentType type)
    {
        currentEquipment.TryGetValue(type, out var item);
        return item;
    }

    public bool Equip(EquipmentType type, InventoryItem newItem, out InventoryItem previousItem)
    {
        previousItem = GetEquippedItem(type);

        if (newItem == null || newItem.Data == null || newItem.Data.EquipmentType != type)
        {
            return false;
        }

        currentEquipment[type] = newItem;
        OnEquipmentChanged?.Invoke(type, newItem);
        return true;
    }

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

    public bool IsEquipped(InventoryItem item)
    {
        if (item == null) return false;
        return currentEquipment.ContainsValue(item);
    }
}