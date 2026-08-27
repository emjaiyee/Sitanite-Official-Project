using UnityEngine;
using UnityEngine.UI;

public class CharacterPortrait : MonoBehaviour
{
    [Header("Portrait Renderers")]
    [SerializeField] private Image bodyPortrait;
    [SerializeField] private Image eyesPortrait;
    [SerializeField] private Image hairPortrait;
    [SerializeField] private Image torsoPortrait;
    [SerializeField] private Image headwearPortrait;
    [SerializeField] private Image weaponPortrait;
    [SerializeField] private Image shieldPortrait;
    [SerializeField] private Image classPortrait;

    [Header("Class Icons")]
    [SerializeField] private Sprite warriorIcon;
    [SerializeField] private Sprite rangerIcon;
    [SerializeField] private Sprite mageIcon;

    [Header("Portrait Buttons")]
    [SerializeField] private Button inventoryButton;
    [SerializeField] private Button statsButton;

    [Header("Class Tooltip")]
    [SerializeField] private UIHoverTooltip classTooltip;
    [TextArea(2, 6)]
    [SerializeField] private string warriorDescription;
    [TextArea(2, 6)]
    [SerializeField] private string rangerDescription;
    [TextArea(2, 6)]
    [SerializeField] private string mageDescription;

    private CharacterCustomizationController customizationController;
    private PlayerInventory playerInventory;
    private PlayerStatsUI playerStatsUI;

    private void Start()
    {
        if (classTooltip == null && classPortrait != null)
            classTooltip = classPortrait.GetComponent<UIHoverTooltip>();

        FindCustomizationController();

        if (customizationController == null)
        {
            Debug.LogError(
                "CharacterPortrait: Could not find CharacterCustomizationController.",
                this
            );

            return;
        }

        customizationController.OnAppearanceChanged += Refresh;
        FindPlayerUIReferences();
        BindButtons();

        Refresh();
    }

    private void OnDestroy()
    {
        if (customizationController != null)
        {
            customizationController.OnAppearanceChanged -= Refresh;
        }

        UnbindButtons();
    }

    private void FindCustomizationController()
    {
        if (Player.Instance == null)
        {
            Debug.LogError(
                "CharacterPortrait: Player.Instance is NULL!",
                this
            );

            return;
        }

        customizationController =
            Player.Instance.GetComponent<CharacterCustomizationController>();

        if (customizationController == null)
        {
            Debug.LogError(
                "CharacterPortrait: CharacterCustomizationController not found on Player!",
                Player.Instance
            );
        }
    }

    private void FindPlayerUIReferences()
    {
        if (Player.Instance == null)
            return;

        playerInventory = Player.Instance.GetComponent<PlayerInventory>();
        playerStatsUI = FindFirstObjectByType<PlayerStatsUI>(
            FindObjectsInactive.Include
        );
    }

    private void BindButtons()
    {
        if (inventoryButton != null && playerInventory != null)
            inventoryButton.onClick.AddListener(playerInventory.ToggleInventory);

        if (statsButton != null && playerStatsUI != null)
            statsButton.onClick.AddListener(playerStatsUI.ToggleStatsWindow);
    }

    private void UnbindButtons()
    {
        if (inventoryButton != null && playerInventory != null)
            inventoryButton.onClick.RemoveListener(playerInventory.ToggleInventory);

        if (statsButton != null && playerStatsUI != null)
            statsButton.onClick.RemoveListener(playerStatsUI.ToggleStatsWindow);
    }

    public void Refresh()
    {
        if (customizationController == null)
            return;

        CharacterAppearance appearance =
            customizationController.GetAppearance();

        SetPortrait(bodyPortrait, appearance.body);
        SetPortrait(eyesPortrait, appearance.eyes);
        SetPortrait(torsoPortrait, appearance.torso);
        SetPortrait(weaponPortrait, appearance.weapon);
        SetPortrait(shieldPortrait, appearance.shield);
        UpdateClassPortrait(appearance.playerClass);
        UpdateClassTooltip(appearance.playerClass);

        UpdateHairPortrait(appearance);
        UpdateHeadwearPortrait(appearance);
    }

    private void UpdateClassPortrait(PlayerClass playerClass)
    {
        if (classPortrait == null)
            return;

        Sprite icon = null;

        switch (playerClass)
        {
            case PlayerClass.Warrior:
                icon = warriorIcon;
                break;

            case PlayerClass.Ranger:
                icon = rangerIcon;
                break;

            case PlayerClass.Mage:
                icon = mageIcon;
                break;
        }

        classPortrait.sprite = icon;
        classPortrait.enabled = icon != null;
    }

    private void UpdateClassTooltip(PlayerClass playerClass)
    {
        if (classTooltip == null)
            return;

        string classDescription = playerClass switch
        {
            PlayerClass.Warrior => warriorDescription,
            PlayerClass.Ranger => rangerDescription,
            PlayerClass.Mage => mageDescription,
            _ => string.Empty
        };

        classTooltip.SetDescription(classDescription);
    }

    private void SetHairPortrait(CharacterAppearance appearance)
    {
        if (hairPortrait == null)
            return;

        // Hide portrait hair if the current headwear hides hair.
        if (appearance.headwear != null &&
            appearance.headwear.hidesHair)
        {
            hairPortrait.sprite = null;
            hairPortrait.enabled = false;
            return;
        }

        // Otherwise, display the currently selected hair.
        SetPortrait(hairPortrait, appearance.hair);
    }

    private void SetPortrait(
        Image image,
        CharacterPartDefinition definition)
    {
        if (image == null)
            return;

        if (definition == null || definition.portrait == null)
        {
            image.sprite = null;
            image.enabled = false;
            return;
        }

        image.sprite = definition.portrait;
        image.enabled = true;
    }
    private void UpdateHairPortrait(CharacterAppearance appearance)
    {
        if (hairPortrait == null)
            return;

        if (appearance.headwear != null &&
            appearance.headwear.hidesHair &&
            !appearance.hideHeadwear)
        {
            hairPortrait.sprite = null;
            hairPortrait.enabled = false;
            return;
        }

        SetPortrait(hairPortrait, appearance.hair);
    }
    private void UpdateHeadwearPortrait(CharacterAppearance appearance)
    {
        if (headwearPortrait == null)
            return;

        if (appearance.hideHeadwear)
        {
            headwearPortrait.sprite = null;
            headwearPortrait.enabled = false;
            return;
        }

        SetPortrait(
            headwearPortrait,
            appearance.headwear
        );
    }
}