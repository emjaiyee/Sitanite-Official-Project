using UnityEngine;

// Handles the player's currently equipped weapon.
public class PlayerEquipment : MonoBehaviour
{
    [Header("Weapon Setup")]
    [SerializeField] private ItemData defaultWeapon;
    [SerializeField] private WeaponController weaponController;

    public IWeapon CurrentWeapon { get; private set; }
    public ItemData CurrentWeaponData { get; private set; }

    private void Awake()
    {
        if (weaponController == null)
            weaponController = GetComponentInChildren<WeaponController>(true);
    }

    private void Start()
    {
        SubscribeToEquipmentChanges();

        if (EquipmentManager.Instance != null)
        {
            InventoryItem equippedItem =
                EquipmentManager.Instance.GetEquippedItem(EquipmentType.Weapon);

            if (equippedItem != null)
            {
                EquipWeapon(equippedItem.Data);
                return;
            }
        }

        EquipWeapon(defaultWeapon);
    }

    private void OnEnable()
    {
        SubscribeToEquipmentChanges();
    }

    private void OnDisable()
    {
        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.OnEquipmentChanged -= HandleEquipmentChanged;
    }

    private void SubscribeToEquipmentChanges()
    {
        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.OnEquipmentChanged -= HandleEquipmentChanged;
            EquipmentManager.Instance.OnEquipmentChanged += HandleEquipmentChanged;
        }
    }

    private void HandleEquipmentChanged(EquipmentType changedType, InventoryItem newItem)
    {
        if (changedType != EquipmentType.Weapon)
            return;

        EquipWeapon(newItem == null ? null : newItem.Data);
    }

    public void EquipWeapon(ItemData weaponData)
    {
        if (weaponData == null || weaponData.EquipmentType != EquipmentType.Weapon)
        {
            Debug.LogWarning("PlayerEquipment: ItemData is not a valid weapon.");
            CurrentWeapon = null;
            CurrentWeaponData = null;
            return;
        }

        if (weaponController == null)
        {
            Debug.LogError("PlayerEquipment: WeaponController is not assigned.");
            CurrentWeapon = null;
            CurrentWeaponData = null;
            return;
        }

        weaponController.Configure(weaponData);
        CurrentWeapon = weaponController;
        CurrentWeaponData = weaponData;
        Debug.Log($"PlayerEquipment: Equipped {weaponData.WeaponId}.");
    }
}