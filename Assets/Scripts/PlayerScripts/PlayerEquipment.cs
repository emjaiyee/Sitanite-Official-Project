using UnityEngine;

// Handles the player's currently equipped weapon.
//
// Weapon GameObjects should be children of the player
// and contain a component implementing IWeapon.
//
// Example:
// Player
// ├── LongSword
// │   └── LongSword : MeleeWeapon
// ├── BattleAxe
// │   └── BattleAxe : MeleeWeapon
// ├── Dagger
// │   └── Dagger : MeleeWeapon
// ├── ShortBow
// │   └── ShortBow : RangedWeapon
// └── LongBow
//     └── LongBow : RangedWeapon

public class PlayerEquipment : MonoBehaviour
{
    [Header("Melee Weapons")]

    [SerializeField]
    private GameObject swordObject;

    [SerializeField]
    private GameObject battleAxeObject;

    [SerializeField]
    private GameObject daggerObject;


    [Header("Ranged Weapons")]

    [SerializeField]
    private GameObject shortBowObject;

    [SerializeField]
    private GameObject longBowObject;


    public IWeapon CurrentWeapon { get; private set; }


    // =========================================================
    // UNITY
    // =========================================================

    private void Start()
{
    Debug.Log("[PlayerEquipment] Starting...");

    Debug.Log(
        $"[PlayerEquipment] Dagger Object = {daggerObject}"
    );

    EquipWeapon("Dagger");

    Debug.Log(
        $"[PlayerEquipment] Current Weapon = {CurrentWeapon}"
    );
}


    // =========================================================
    // EQUIP WEAPON
    // =========================================================

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


        // Disable currently equipped weapon.
        DisableAllWeapons();


        // Find requested weapon.
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


        // Make sure the GameObject contains IWeapon.
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


        // Enable weapon.
        weaponObject.SetActive(true);


        // Store currently equipped weapon.
        CurrentWeapon = weapon;


        Debug.Log(
            $"PlayerEquipment: Equipped {weaponId}."
        );
    }


    // =========================================================
    // WEAPON LOOKUP
    // =========================================================

    private GameObject GetWeaponObject(string weaponId)
    {
        switch (weaponId)
        {
            case "LongSword":
                return swordObject;

            case "BattleAxe":
                return battleAxeObject;

            case "Dagger":
                return daggerObject;

            case "ShortBow":
                return shortBowObject;

            case "LongBow":
                return longBowObject;

            default:
                return null;
        }
    }


    // =========================================================
    // DISABLE ALL WEAPONS
    // =========================================================

    private void DisableAllWeapons()
    {
        if (swordObject != null)
            swordObject.SetActive(false);

        if (battleAxeObject != null)
            battleAxeObject.SetActive(false);

        if (daggerObject != null)
            daggerObject.SetActive(false);

        if (shortBowObject != null)
            shortBowObject.SetActive(false);

        if (longBowObject != null)
            longBowObject.SetActive(false);
    }
}