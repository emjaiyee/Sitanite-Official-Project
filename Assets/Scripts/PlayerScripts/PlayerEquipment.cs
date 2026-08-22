using UnityEngine;

// Handles the player's currently equipped weapon.
//
// Weapon GameObjects should be children of the player and contain
// a component implementing IWeapon.
//
// Example:
// Player
// ├── LongSword
// │   └── Sword : MeleeWeapon
// └── BattleAxe
//     └── Axe : MeleeWeapon
public class PlayerEquipment : MonoBehaviour
{
    [Header("Weapon Objects (children of the player)")]

    [SerializeField]
    private GameObject swordObject;

    [SerializeField]
    private GameObject battleAxeObject;

    public IWeapon CurrentWeapon { get; private set; }

    private void Start()
    {
        // Default weapon.
        EquipWeapon("LongSword");
    }

    /// <summary>
    /// Equips a weapon using its WeaponId.
    /// </summary>
    public void EquipWeapon(string weaponId)
    {
        if (string.IsNullOrEmpty(weaponId))
        {
            Debug.LogWarning(
                "PlayerEquipment: Weapon ID is empty."
            );

            return;
        }

        // Disable all currently equipped weapon objects.
        DisableAllWeapons();

        // Find the requested weapon.
        GameObject weaponObject =
            GetWeaponObject(weaponId);

        if (weaponObject == null)
        {
            Debug.LogWarning(
                $"PlayerEquipment: No weapon found for ID '{weaponId}'."
            );

            CurrentWeapon = null;
            return;
        }

        // Make sure the weapon actually implements IWeapon.
        IWeapon weapon =
            weaponObject.GetComponent<IWeapon>();

        if (weapon == null)
        {
            Debug.LogError(
                $"PlayerEquipment: '{weaponObject.name}' does not " +
                "contain a component implementing IWeapon."
            );

            CurrentWeapon = null;
            return;
        }

        weaponObject.SetActive(true);

        CurrentWeapon = weapon;

        Debug.Log(
            $"PlayerEquipment: Equipped {weaponId}."
        );
    }

    // -------------------------------------------------
    // WEAPON LOOKUP
    // -------------------------------------------------

    private GameObject GetWeaponObject(string weaponId)
    {
        switch (weaponId)
        {
            case "LongSword":
                return swordObject;

            case "BattleAxe":
                return battleAxeObject;

            default:
                return null;
        }
    }

    // -------------------------------------------------
    // DISABLE WEAPONS
    // -------------------------------------------------

    private void DisableAllWeapons()
    {
        if (swordObject != null)
            swordObject.SetActive(false);

        if (battleAxeObject != null)
            battleAxeObject.SetActive(false);
    }
}