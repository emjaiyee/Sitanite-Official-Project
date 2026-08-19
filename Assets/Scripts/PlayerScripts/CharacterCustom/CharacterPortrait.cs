using UnityEngine;
using UnityEngine.UI;

public class CharacterPortrait : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterCustomizationController customizationController;

    [Header("Portrait Renderers")]
    [SerializeField] private Image bodyPortrait;
    [SerializeField] private Image eyesPortrait;
    [SerializeField] private Image hairPortrait;
    [SerializeField] private Image torsoPortrait;
    [SerializeField] private Image legsPortrait;
    [SerializeField] private Image headwearPortrait;

    private void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (customizationController == null)
        {
            Debug.LogError(
                "CharacterPortrait: CharacterCustomizationController is not assigned.",
                this
            );

            return;
        }

        CharacterAppearance appearance =
            customizationController.GetAppearance();

        SetPortrait(bodyPortrait, appearance.body);
        SetPortrait(eyesPortrait, appearance.eyes);
        SetPortrait(hairPortrait, appearance.hair);
        SetPortrait(torsoPortrait, appearance.torso);
        SetPortrait(legsPortrait, appearance.legs);
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
    private void OnEnable()
    {
        if (customizationController != null)
        {
            customizationController.OnAppearanceChanged += Refresh;
        }
    }

    private void OnDisable()
    {
        if (customizationController != null)
        {
            customizationController.OnAppearanceChanged -= Refresh;
        }
    }
}