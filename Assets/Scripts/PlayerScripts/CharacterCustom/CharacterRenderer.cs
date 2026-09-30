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
    private CharacterAnimationState currentAnimationState = CharacterAnimationState.Idle;
    private int currentAnimationFrame;

    public CharacterDirection CurrentDirection
    {
        get => currentDirection;
        set
        {
            if (currentDirection == value)
                return;

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
        headwearRenderer.sprite = appearance.headwear.GetSprite(
            currentDirection,
            currentAnimationState,
            currentAnimationFrame
        );
    }

    private void UpdateWeapon()
    {
        bool weaponIsUnder = currentDirection == CharacterDirection.SouthWest ||
            currentDirection == CharacterDirection.South ||
            currentDirection == CharacterDirection.SouthEast ||
            currentDirection == CharacterDirection.East ||
            currentDirection == CharacterDirection.West;

        if (currentAnimationState == CharacterAnimationState.Melee ||
            currentAnimationState == CharacterAnimationState.Cast ||
            currentAnimationState == CharacterAnimationState.Ranged)
        {
            weaponIsUnder = !weaponIsUnder;
        }

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
        renderer.sprite = definition.GetSprite(
            currentDirection,
            currentAnimationState,
            currentAnimationFrame
        );
    }

    public void Refresh()
    {
        UpdateAppearance();
    }

    public Sprite GetPreviewSprite(SpriteRenderer renderer, CharacterDirection direction)
    {
        if (renderer == null || appearance == null)
            return null;

        if (renderer == weaponUnderRenderer || renderer == weaponOverRenderer)
        {
            bool weaponIsUnder = direction == CharacterDirection.SouthWest ||
                direction == CharacterDirection.South ||
                direction == CharacterDirection.SouthEast ||
                direction == CharacterDirection.East ||
                direction == CharacterDirection.West;

            if (currentAnimationState == CharacterAnimationState.Melee ||
                currentAnimationState == CharacterAnimationState.Cast ||
                currentAnimationState == CharacterAnimationState.Ranged)
            {
                weaponIsUnder = !weaponIsUnder;
            }

            bool rendererShowsWeapon = renderer == weaponUnderRenderer
                ? weaponIsUnder
                : !weaponIsUnder;
            return rendererShowsWeapon
                ? GetPreviewSprite(appearance.weapon, direction)
                : null;
        }

        if (renderer == bodyRenderer)
            return GetPreviewSprite(appearance.body, direction);
        if (renderer == legsRenderer)
            return GetPreviewSprite(appearance.legs, direction);
        if (renderer == torsoRenderer)
            return GetPreviewSprite(appearance.torso, direction);
        if (renderer == eyesRenderer)
            return GetPreviewSprite(appearance.eyes, direction);
        if (renderer == hairRenderer)
        {
            bool hairHiddenByHeadwear = appearance.headwear != null &&
                appearance.headwear.hidesHair &&
                !appearance.hideHeadwear;
            return hairHiddenByHeadwear ? null : GetPreviewSprite(appearance.hair, direction);
        }
        if (renderer == headwearRenderer)
        {
            return appearance.headwear == null || appearance.hideHeadwear
                ? null
                : appearance.headwear.GetSprite(direction, currentAnimationState, currentAnimationFrame);
        }
        if (renderer == shieldRenderer)
            return GetPreviewSprite(appearance.shield, direction);

        return renderer.enabled ? renderer.sprite : null;
    }

    public void SetAnimationState(
        CharacterAnimationState animationState,
        int frame)
    {
        if (currentAnimationState == animationState &&
            currentAnimationFrame == frame)
            return;

        currentAnimationState = animationState;
        currentAnimationFrame = frame;
        UpdateAppearance();
    }

    private Sprite GetPreviewSprite(CharacterPartDefinition definition, CharacterDirection direction)
    {
        return definition != null
            ? definition.GetSprite(direction, currentAnimationState, currentAnimationFrame)
            : null;
    }
}