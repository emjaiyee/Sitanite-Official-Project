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

    private CharacterCustomizationController customizationController;

    private void Start()
    {
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

        Refresh();
    }

    private void OnDestroy()
    {
        if (customizationController != null)
        {
            customizationController.OnAppearanceChanged -= Refresh;
        }
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

        UpdateHairPortrait(appearance);
        UpdateHeadwearPortrait(appearance);
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