using UnityEngine;

public class EquipmentCharacterVisualSync : MonoBehaviour
{
    private CharacterCustomizationController customizationController;
    private EquipmentManager boundEquipmentManager;

    private void Awake()
    {
        customizationController = GetComponent<CharacterCustomizationController>();
    }

    private void Update()
    {
        if (boundEquipmentManager == EquipmentManager.Instance)
            return;

        Unbind();
        Bind(EquipmentManager.Instance);
    }

    private void OnDestroy()
    {
        Unbind();
    }

    private void Bind(EquipmentManager equipmentManager)
    {
        if (equipmentManager == null || customizationController == null)
            return;

        boundEquipmentManager = equipmentManager;
        boundEquipmentManager.OnEquipmentChanged += HandleEquipmentChanged;
        customizationController.ReapplyEquipmentVisuals();
    }

    private void Unbind()
    {
        if (boundEquipmentManager == null)
            return;

        boundEquipmentManager.OnEquipmentChanged -= HandleEquipmentChanged;
        boundEquipmentManager = null;
    }

    private void HandleEquipmentChanged(EquipmentType equipmentType, InventoryItem item)
    {
        if (customizationController == null || item == null || item.Data == null)
        {
            customizationController?.SetEquipmentVisual(equipmentType, null);
            return;
        }

        customizationController.SetEquipmentVisual(equipmentType, item.Data.CharacterDefinition);
    }
}