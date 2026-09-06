using UnityEngine;


// Handles the player's currently equipped weapon.
public class PlayerEquipment : MonoBehaviour
{
    [Header("Weapon Setup")]
    [SerializeField] private ItemData defaultWeapon;
    [SerializeField] private WeaponController weaponController;


    public IWeapon CurrentWeapon { get; private set; }

    public ItemData CurrentWeaponData { get; private set; }


    private PlayerAnimationController animationController;



    private void Awake()
    {
        if (weaponController == null)
        {
            weaponController =
                GetComponentInChildren<WeaponController>(true);
        }


        animationController =
            GetComponent<PlayerAnimationController>();


        if (weaponController == null)
        {
            Debug.LogWarning(
                "PlayerEquipment: WeaponController not found."
            );
        }


        if (animationController == null)
        {
            Debug.LogWarning(
                "PlayerEquipment: PlayerAnimationController not found."
            );
        }
    }



    private void Start()
    {
        SubscribeToEquipmentChanges();


        if (EquipmentManager.Instance != null)
        {
            InventoryItem equippedItem =
                EquipmentManager.Instance
                .GetEquippedItem(EquipmentType.Weapon);


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
        {
            EquipmentManager.Instance
                .OnEquipmentChanged -= HandleEquipmentChanged;
        }
    }



    private void SubscribeToEquipmentChanges()
    {
        if (EquipmentManager.Instance == null)
            return;


        EquipmentManager.Instance
            .OnEquipmentChanged -= HandleEquipmentChanged;


        EquipmentManager.Instance
            .OnEquipmentChanged += HandleEquipmentChanged;
    }





    private void HandleEquipmentChanged(
        EquipmentType changedType,
        InventoryItem newItem)
    {
        if (changedType != EquipmentType.Weapon)
            return;


        EquipWeapon(
            newItem == null
            ? null
            : newItem.Data
        );
    }






    public void EquipWeapon(
        ItemData weaponData)
    {
        // No weapon equipped
        if (weaponData == null ||
            weaponData.EquipmentType != EquipmentType.Weapon)
        {
            Debug.Log(
                "PlayerEquipment: Weapon removed."
            );


            CurrentWeapon = null;
            CurrentWeaponData = null;


            // Reset animator weapon type
            if (animationController != null)
            {
                animationController.SetWeaponType(
                    WeaponAttackType.Melee
                );
            }


            return;
        }





        if (weaponController == null)
        {
            Debug.LogError(
                "PlayerEquipment: WeaponController missing."
            );

            return;
        }





        // Configure weapon
        weaponController.Configure(
            weaponData
        );



        CurrentWeapon =
            weaponController;


        CurrentWeaponData =
            weaponData;





        // Update Animator WeaponT
        if (animationController != null)
        {
            animationController.SetWeaponType(
                weaponData.WeaponAttackType
            );
        }





        Debug.Log(
            $"PlayerEquipment: Equipped {weaponData.WeaponId}. " +
            $"Type: {weaponData.WeaponAttackType}"
        );
    }
}