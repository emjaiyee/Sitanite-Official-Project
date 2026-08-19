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
        SetPortrait(hairPortrait, appearance.hair);
        SetPortrait(torsoPortrait, appearance.torso);
        SetPortrait(headwearPortrait, appearance.headwear);
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
}