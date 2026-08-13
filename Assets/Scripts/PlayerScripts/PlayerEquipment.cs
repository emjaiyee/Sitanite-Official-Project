using UnityEngine;

// Attach to the player prefab, alongside PlayerActions.
// Drag the sword GameObject (with MeleeWeapon) into the slot below.
public class PlayerEquipment : MonoBehaviour
{
    [Header("Weapon Object (child of the player)")]
    [SerializeField] private GameObject swordObject;

    public IWeapon CurrentWeapon { get; private set; }

    void Start()
    {
        EquipSword();
    }

    private void EquipSword()
    {
        if (swordObject != null)
        {
            swordObject.SetActive(true);
            CurrentWeapon = swordObject.GetComponent<IWeapon>();
        }
        else
        {
            Debug.LogWarning("PlayerEquipment: swordObject not assigned.");
        }
    }
}