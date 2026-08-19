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

    private void Start()
    {
        SetGender(startingGender);
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
        characterRenderer.Appearance.headwear = headwear;
        RefreshAppearance();
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