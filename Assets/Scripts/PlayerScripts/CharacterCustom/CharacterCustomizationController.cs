using System;
using UnityEngine;

public class CharacterCustomizationController : MonoBehaviour
{
    public event Action OnAppearanceChanged;

    [Header("References")]
    [SerializeField] private CharacterRenderer characterRenderer;

    [Header("Starting Gender")]
    [SerializeField] private CharacterGender startingGender = CharacterGender.Male;

    [Header("Male Defaults")]
    [SerializeField] private CharacterPartDefinition maleBody;
    [SerializeField] private CharacterPartDefinition maleEyes;
    [SerializeField] private CharacterPartDefinition maleHair;

    [Header("Female Defaults")]
    [SerializeField] private CharacterPartDefinition femaleBody;
    [SerializeField] private CharacterPartDefinition femaleEyes;
    [SerializeField] private CharacterPartDefinition femaleHair;

    [Header("Starting Class")]
    [SerializeField] private PlayerClass startingClass = PlayerClass.Warrior;

    [Header("Attribute References")]
    [SerializeField] private PlayerAttributesNTraits attributes;
    [SerializeField] private PlayerStats playerStats;

    [Header("Class Starting Gear")]
    [Tooltip("Item data granted to a Warrior when the class is selected.")]
    [SerializeField] private ItemData[] warriorStartingGear;

    [Tooltip("Item data granted to a Ranger when the class is selected.")]
    [SerializeField] private ItemData[] rangerStartingGear;

    [Tooltip("Item data granted to a Mage when the class is selected.")]
    [SerializeField] private ItemData[] mageStartingGear;

    [Header("Class Starter Items")]
    [Tooltip("Non-equipment item data granted to a Warrior when the class is selected.")]
    [SerializeField] private ItemData[] warriorStartingItems;

    [Tooltip("Non-equipment item data granted to a Ranger when the class is selected.")]
    [SerializeField] private ItemData[] rangerStartingItems;

    [Tooltip("Non-equipment item data granted to a Mage when the class is selected.")]
    [SerializeField] private ItemData[] mageStartingItems;

    private PlayerClass? grantedStartingLoadoutClass;
    private bool appliedStartingAttributes;

    private void Awake()
    {
        if (attributes == null)
            attributes = GetComponent<PlayerAttributesNTraits>();

        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();
    }

    private void Start()
    {
        SetGender(startingGender);
        SetClass(startingClass);
    }

    private void Update()
    {
        if (characterRenderer != null)
            SpawnClassLoadout(characterRenderer.Appearance.playerClass);
    }

    public void SetGender(CharacterGender gender)
    {
        if (characterRenderer == null)
        {
            Debug.LogError(
                "CharacterCustomizationController: CharacterRenderer is not assigned.",
                this
            );

            return;
        }

        CharacterAppearance appearance = characterRenderer.Appearance;

        Debug.Log("========================================");
        Debug.Log($"SET GENDER CALLED: {gender}");
        Debug.Log($"Previous Gender: {appearance.gender}");

        appearance.gender = gender;

        switch (gender)
        {
            case CharacterGender.Male:

                Debug.Log($"Applying Male Body: {maleBody}");
                Debug.Log($"Applying Male Eyes: {maleEyes}");
                Debug.Log($"Applying Male Hair: {maleHair}");

                appearance.body = maleBody;
                appearance.eyes = maleEyes;
                appearance.hair = maleHair;

                break;

            case CharacterGender.Female:

                Debug.Log($"Applying Female Body: {femaleBody}");
                Debug.Log($"Applying Female Eyes: {femaleEyes}");
                Debug.Log($"Applying Female Hair: {femaleHair}");

                appearance.body = femaleBody;
                appearance.eyes = femaleEyes;
                appearance.hair = femaleHair;

                break;
        }

        Debug.Log($"New Gender: {appearance.gender}");
        Debug.Log($"New Body: {appearance.body}");
        Debug.Log($"New Eyes: {appearance.eyes}");
        Debug.Log($"New Hair: {appearance.hair}");

        RefreshAppearance();

        Debug.Log("CharacterRenderer.Refresh() called.");
        Debug.Log("========================================");
    }

    public CharacterGender GetGender()
    {
        return characterRenderer.Appearance.gender;
    }

    public PlayerClass GetClass()
    {
        return characterRenderer.Appearance.playerClass;
    }

    public void SetBody(CharacterPartDefinition body)
    {
        characterRenderer.Appearance.body = body;
        RefreshAppearance();
    }

    public void SetEyes(
        CharacterPartDefinition maleEyes,
        CharacterPartDefinition femaleEyes)
    {
        CharacterAppearance appearance = characterRenderer.Appearance;

        switch (appearance.gender)
        {
            case CharacterGender.Male:
                appearance.eyes = maleEyes;
                break;

            case CharacterGender.Female:
                appearance.eyes = femaleEyes;
                break;
        }

        RefreshAppearance();
    }

    public void SetHair(CharacterPartDefinition hair)
    {
        characterRenderer.Appearance.hair = hair;
        RefreshAppearance();
    }

    public void SetHeadwear(HeadwearDefinition headwear)
    {
        CharacterAppearance appearance =
            characterRenderer.Appearance;

        appearance.headwear = headwear;

        // Equipping new headwear shows it by default.
        appearance.hideHeadwear = false;

        RefreshAppearance();
    }

    public void SetEquipmentVisual(EquipmentType equipmentType, CharacterPartDefinition definition)
    {
        switch (equipmentType)
        {
            case EquipmentType.Helmet:
                SetHeadwear(definition as HeadwearDefinition);
                break;
            case EquipmentType.Chestplate:
                SetTorso(definition);
                break;
            case EquipmentType.Legging:
                SetLegs(definition);
                break;
            case EquipmentType.Weapon:
                characterRenderer.Appearance.weapon =
                    definition as WeaponDefinition;
                RefreshAppearance();
                break;
            case EquipmentType.Shield:
                characterRenderer.Appearance.shield = definition;
                RefreshAppearance();
                break;
        }
    }

    public void ReapplyEquipmentVisuals()
    {
        EquipmentManager equipmentManager = EquipmentManager.Instance;
        if (equipmentManager == null)
            return;

        SetEquipmentVisual(EquipmentType.Helmet, GetEquippedDefinition(equipmentManager, EquipmentType.Helmet));
        SetEquipmentVisual(EquipmentType.Chestplate, GetEquippedDefinition(equipmentManager, EquipmentType.Chestplate));
        SetEquipmentVisual(EquipmentType.Legging, GetEquippedDefinition(equipmentManager, EquipmentType.Legging));
        SetEquipmentVisual(EquipmentType.Weapon, GetEquippedDefinition(equipmentManager, EquipmentType.Weapon));
        SetEquipmentVisual(EquipmentType.Shield, GetEquippedDefinition(equipmentManager, EquipmentType.Shield));
    }

    private CharacterPartDefinition GetEquippedDefinition(EquipmentManager equipmentManager, EquipmentType equipmentType)
    {
        InventoryItem item = equipmentManager.GetEquippedItem(equipmentType);
        return item != null && item.Data != null ? item.Data.CharacterDefinition : null;
    }

    public void SetHeadwearHidden(bool hidden)
    {
        CharacterAppearance appearance =
            characterRenderer.Appearance;

        appearance.hideHeadwear = hidden;

        RefreshAppearance();
    }

    public bool IsHeadwearHidden()
    {
        return characterRenderer.Appearance.hideHeadwear;
    }

    public void SetTorso(CharacterPartDefinition torso)
    {
        characterRenderer.Appearance.torso = torso;
        RefreshAppearance();
    }

    public void SetLegs(CharacterPartDefinition legs)
    {
        characterRenderer.Appearance.legs = legs;
        RefreshAppearance();
    }

    public void SetSkinTone(
        CharacterPartDefinition maleSkinTone,
        CharacterPartDefinition femaleSkinTone)
    {
        CharacterAppearance appearance = characterRenderer.Appearance;

        switch (appearance.gender)
        {
            case CharacterGender.Male:
                appearance.body = maleSkinTone;
                break;

            case CharacterGender.Female:
                appearance.body = femaleSkinTone;
                break;
        }

        RefreshAppearance();
    }

    public void SetClass(PlayerClass playerClass)
    {
        if (characterRenderer == null)
        {
            Debug.LogError(
                "CharacterCustomizationController: CharacterRenderer is not assigned.",
                this
            );

            return;
        }

        CharacterAppearance appearance =
            characterRenderer.Appearance;

        bool classChanged = appearance.playerClass != playerClass;

        EquipmentManager equipmentManager = EquipmentManager.Instance;
        if (equipmentManager != null)
        {
            equipmentManager.Unequip(EquipmentType.Helmet);
            equipmentManager.Unequip(EquipmentType.Chestplate);
            equipmentManager.Unequip(EquipmentType.Legging);
            equipmentManager.Unequip(EquipmentType.Weapon);
            equipmentManager.Unequip(EquipmentType.Shield);
        }

        PlayerInventory playerInventory = GetComponent<PlayerInventory>();
        if (playerInventory != null && playerInventory.MainBackPack != null)
            playerInventory.MainBackPack.Clear();

        appearance.playerClass = playerClass;
        appearance.headwear = null;
        appearance.torso = null;
        appearance.legs = null;
        appearance.weapon = null;
        appearance.shield = null;

        if (attributes != null && (classChanged || !appliedStartingAttributes))
        {
            attributes.ApplyClassDefaults(playerClass);
            appliedStartingAttributes = true;

            if (playerStats != null)
                playerStats.ResetToFull();
        }

        RefreshAppearance();
        grantedStartingLoadoutClass = null;
        SpawnClassLoadout(playerClass);
    }

    public int SpawnStartingGear(PlayerClass playerClass)
    {
        ItemData[] startingGear = playerClass switch
        {
            PlayerClass.Warrior => warriorStartingGear,
            PlayerClass.Ranger => rangerStartingGear,
            PlayerClass.Mage => mageStartingGear,
            _ => null
        };

        return SpawnItemsToInventory(startingGear);
    }

    public int SpawnClassLoadout(PlayerClass playerClass)
    {
        if (grantedStartingLoadoutClass == playerClass)
            return 0;

        ItemData[] startingGear = GetStartingGear(playerClass);
        ItemData[] startingItems = GetStartingItems(playerClass);
        ItemData[] loadout = new ItemData[(startingGear?.Length ?? 0) + (startingItems?.Length ?? 0)];

        int loadoutIndex = 0;
        if (startingGear != null)
        {
            foreach (ItemData data in startingGear)
                loadout[loadoutIndex++] = data;
        }

        if (startingItems != null)
        {
            foreach (ItemData data in startingItems)
                loadout[loadoutIndex++] = data;
        }

        int spawnedCount = SpawnItemsToInventory(loadout);
        if (spawnedCount >= 0)
            grantedStartingLoadoutClass = playerClass;

        return spawnedCount;
    }

    private ItemData[] GetStartingGear(PlayerClass playerClass)
    {
        return playerClass switch
        {
            PlayerClass.Warrior => warriorStartingGear,
            PlayerClass.Ranger => rangerStartingGear,
            PlayerClass.Mage => mageStartingGear,
            _ => null
        };
    }

    private ItemData[] GetStartingItems(PlayerClass playerClass)
    {
        return playerClass switch
        {
            PlayerClass.Warrior => warriorStartingItems,
            PlayerClass.Ranger => rangerStartingItems,
            PlayerClass.Mage => mageStartingItems,
            _ => null
        };
    }

    public int SpawnItemsToInventory(ItemData[] itemData)
    {
        if (itemData == null || itemData.Length == 0)
            return 0;

        PlayerInventory playerInventory = GetComponent<PlayerInventory>();
        EquipmentManager equipmentManager = EquipmentManager.Instance;

        foreach (ItemData data in itemData)
        {
            if (data == null)
                continue;

            if (data.EquipmentType != EquipmentType.None && equipmentManager == null)
                return -1;

            if (data.EquipmentType == EquipmentType.None &&
                (playerInventory == null || playerInventory.MainBackPack == null))
            {
                return -1;
            }
        }

        int spawnedCount = 0;
        foreach (ItemData data in itemData)
        {
            if (data == null)
                continue;

            InventoryItem item = new InventoryItem(data);
            if (data.EquipmentType != EquipmentType.None)
            {
                if (equipmentManager != null &&
                    equipmentManager.Equip(data.EquipmentType, item, out _))
                {
                    spawnedCount++;
                    continue;
                }

                Debug.LogWarning(
                    $"CharacterCustomizationController: Could not equip '{data.itemName}' as starting gear.",
                    this
                );
            }
            else if (playerInventory.MainBackPack.TryAddItem(item))
            {
                spawnedCount++;
                continue;
            }
            else
                Debug.LogWarning(
                    $"CharacterCustomizationController: Could not add '{data.itemName}' to the inventory.",
                    this
                );
        }

        return spawnedCount;
    }

    public CharacterAppearance GetAppearance()
    {
        return characterRenderer.Appearance;
    }

    private void RefreshAppearance()
    {
        characterRenderer.Refresh();

        OnAppearanceChanged?.Invoke();
    }
}