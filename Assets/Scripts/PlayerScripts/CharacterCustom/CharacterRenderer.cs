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
    [SerializeField] private SpriteRenderer shieldRenderer;

    private CharacterDirection currentDirection = CharacterDirection.SouthWest;

    public CharacterDirection CurrentDirection
    {
        get => currentDirection;
        set
        {
            currentDirection = value;
            UpdateAppearance();
        }
    }

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
        UpdateShield();
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
        if (hairRenderer == null)
            return;

        // If headwear is hidden, it should NOT hide the hair.
        if (appearance.headwear != null &&
            appearance.headwear.hidesHair &&
            !appearance.hideHeadwear)
        {
            hairRenderer.sprite = null;
            hairRenderer.enabled = false;
            return;
        }

        SetSprite(hairRenderer, appearance.hair);
    }

    private void UpdateHeadwear()
    {
        if (headwearRenderer == null)
            return;

        if (appearance.headwear == null ||
            appearance.hideHeadwear)
        {
            headwearRenderer.sprite = null;
            headwearRenderer.enabled = false;
            return;
        }

        headwearRenderer.enabled = true;
        headwearRenderer.sprite =
            appearance.headwear.GetSprite(currentDirection);
    }

    private void UpdateWeapon()
    {
        bool weaponIsUnder = currentDirection == CharacterDirection.SouthWest ||
            currentDirection == CharacterDirection.South ||
            currentDirection == CharacterDirection.SouthEast ||
            currentDirection == CharacterDirection.East ||
            currentDirection == CharacterDirection.West;

        SetSprite(
            weaponUnderRenderer,
            weaponIsUnder ? appearance.weapon : null
        );

        SetSprite(
            weaponOverRenderer,
            weaponIsUnder ? null : appearance.weapon
        );
    }

    private void UpdateShield()
    {
        SetSprite(shieldRenderer, appearance.shield);
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