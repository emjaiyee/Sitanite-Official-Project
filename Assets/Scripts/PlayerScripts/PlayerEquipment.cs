using UnityEngine;

// Handles the player's currently equipped weapon.
public class PlayerEquipment : MonoBehaviour
{
    [Header("Weapon Setup")]
    [SerializeField] private ItemData defaultWeapon;
    [SerializeField] private WeaponController weaponController;

    public IWeapon CurrentWeapon { get; private set; }

    private void Awake()
    {
        if (weaponController == null)
            weaponController = GetComponentInChildren<WeaponController>(true);
    }

    private void Start()
    {
        EquipWeapon(defaultWeapon);
    }

    public void EquipWeapon(ItemData weaponData)
    {
        if (weaponData == null || weaponData.EquipmentType != EquipmentType.Weapon)
        {
            Debug.LogWarning("PlayerEquipment: ItemData is not a valid weapon.");
            CurrentWeapon = null;
            return;
        }

        if (weaponController == null)
        {
            Debug.LogError("PlayerEquipment: WeaponController is not assigned.");
            CurrentWeapon = null;
            return;
        }

        weaponController.Configure(weaponData);
        CurrentWeapon = weaponController;
        Debug.Log($"PlayerEquipment: Equipped {weaponData.WeaponId}.");
    }
}