using UnityEngine;

public class CharacterRenderer : MonoBehaviour
{
    [Header("Character Appearance")]
    [SerializeField] private CharacterAppearance appearance;

    public CharacterAppearance Appearance => appearance;

    [Header("Sprite Renderers")]
    [SerializeField] private SpriteRenderer weaponUnderRenderer;
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private SpriteRenderer legsRenderer;
    [SerializeField] private SpriteRenderer torsoRenderer;
    [SerializeField] private SpriteRenderer eyesRenderer;
    [SerializeField] private SpriteRenderer hairRenderer;
    [SerializeField] private SpriteRenderer headwearRenderer;
    [SerializeField] private SpriteRenderer weaponOverRenderer;

    private CharacterDirection currentDirection = CharacterDirection.SouthWest;

    private void Awake()
    {
        Debug.Log("CharacterRenderer Awake!");
    }

    public void UpdateAppearance()
    {
        UpdateBody();
        UpdateLegs();
        UpdateTorso();
        UpdateEyes();
        UpdateHair();
        UpdateHeadwear();
        UpdateWeapon();
    }

    private void UpdateBody()
    {
        SetSprite(bodyRenderer, appearance.body);
    }

    private void UpdateLegs()
    {
        SetSprite(legsRenderer, appearance.legs);
    }

    private void UpdateTorso()
    {
        SetSprite(torsoRenderer, appearance.torso);
    }

    private void UpdateEyes()
    {
        SetSprite(eyesRenderer, appearance.eyes);
    }

    private void UpdateHair()
    {
        if (appearance.headwear != null && appearance.headwear.hidesHair)
        {
            hairRenderer.enabled = false;
            return;
        }

        hairRenderer.enabled = true;
        SetSprite(hairRenderer, appearance.hair);
    }

    private void UpdateHeadwear()
    {
        SetSprite(headwearRenderer, appearance.headwear);
    }

    private void UpdateWeapon()
    {
        // We'll handle weapon layering separately.
    }

    private void SetSprite(
        SpriteRenderer renderer,
        CharacterPartDefinition definition)
    {
        if (renderer == null)
            return;

        if (definition == null)
        {
            renderer.sprite = null;
            renderer.enabled = false;
            return;
        }

        renderer.enabled = true;
        renderer.sprite = definition.GetSprite(currentDirection);
    }
    public void Refresh()
    {
        UpdateAppearance();
    }
}