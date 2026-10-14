using System.Collections;
using UnityEngine;

public class CharacterRenderer : MonoBehaviour
{
    private static readonly Color CounterAttackColor = new Color(1f, 0.12f, 0.12f);

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
    [SerializeField] private GameObject meleeCounterFireVisualPrefab;
    [SerializeField] private GameObject blockedVisualPrefab;
    [SerializeField] private GameObject parryVisualPrefab;
    [SerializeField] private GameObject criticalHitVisualPrefab;

    public GameObject CriticalHitVisualPrefab => criticalHitVisualPrefab;

    private CharacterDirection currentDirection = CharacterDirection.SouthWest;
    private CharacterAnimationState currentAnimationState = CharacterAnimationState.Idle;
    private int currentAnimationFrame;
    private Coroutine counterAttackFlashRoutine;
    private Color originalWeaponUnderColor;
    private Color originalWeaponOverColor;
    private bool hasCounterAttackFlashColors;

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

    private void OnDisable()
    {
        StopCounterAttackFlash();
    }

    public void PlayCounterAttackFlash(float duration)
    {
        StopCounterAttackFlash();

        if (weaponUnderRenderer == null && weaponOverRenderer == null)
            return;

        originalWeaponUnderColor = weaponUnderRenderer != null
            ? weaponUnderRenderer.color
            : Color.white;
        originalWeaponOverColor = weaponOverRenderer != null
            ? weaponOverRenderer.color
            : Color.white;
        hasCounterAttackFlashColors = true;
        counterAttackFlashRoutine = StartCoroutine(
            FadeCounterAttackFlash(Mathf.Max(0.01f, duration))
        );
    }

    private IEnumerator FadeCounterAttackFlash(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            ApplyCounterAttackFlash(1f - elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        ApplyCounterAttackFlash(0f);
        hasCounterAttackFlashColors = false;
        counterAttackFlashRoutine = null;
    }

    private void StopCounterAttackFlash()
    {
        if (counterAttackFlashRoutine != null)
        {
            StopCoroutine(counterAttackFlashRoutine);
            counterAttackFlashRoutine = null;
        }

        if (hasCounterAttackFlashColors)
        {
            ApplyCounterAttackFlash(0f);
            hasCounterAttackFlashColors = false;
        }
    }

    private void ApplyCounterAttackFlash(float amount)
    {
        if (!hasCounterAttackFlashColors)
            return;

        if (weaponUnderRenderer != null)
        {
            Color color = Color.Lerp(
                originalWeaponUnderColor,
                CounterAttackColor,
                amount
            );
            color.a = weaponUnderRenderer.color.a;
            weaponUnderRenderer.color = color;
        }

        if (weaponOverRenderer != null)
        {
            Color color = Color.Lerp(
                originalWeaponOverColor,
                CounterAttackColor,
                amount
            );
            color.a = weaponOverRenderer.color.a;
            weaponOverRenderer.color = color;
        }
    }

    public void PlayCounterAttackWeaponVisual()
    {
        PlayAttachedVisual(meleeCounterFireVisualPrefab, GetAttackWeaponRenderer());
    }

    private SpriteRenderer GetAttackWeaponRenderer()
    {
        bool weaponIsUnder = currentDirection == CharacterDirection.SouthWest ||
            currentDirection == CharacterDirection.South ||
            currentDirection == CharacterDirection.SouthEast ||
            currentDirection == CharacterDirection.East ||
            currentDirection == CharacterDirection.West;
        weaponIsUnder = !weaponIsUnder;

        return weaponIsUnder
            ? weaponUnderRenderer
            : weaponOverRenderer;
    }

    public void PlayShieldDefenseVisual(bool isParry)
    {
        PlayAttachedVisual(
            isParry ? parryVisualPrefab : blockedVisualPrefab,
            shieldRenderer
        );
    }

    private void PlayAttachedVisual(
        GameObject visualPrefab,
        SpriteRenderer anchorRenderer)
    {
        if (visualPrefab == null || anchorRenderer == null)
            return;

        GameObject visual = Instantiate(visualPrefab, anchorRenderer.transform, false);
        visual.transform.localPosition =
            anchorRenderer.transform.InverseTransformPoint(anchorRenderer.bounds.center);
        visual.transform.localRotation = Quaternion.identity;

        SpriteRenderer[] visualRenderers =
            visual.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer visualRenderer in visualRenderers)
        {
            visualRenderer.sortingLayerID = anchorRenderer.sortingLayerID;
            visualRenderer.sortingOrder = anchorRenderer.sortingOrder + 1;
        }

        SpriteArrayVisual spriteArrayVisual =
            visual.GetComponentInChildren<SpriteArrayVisual>();
        if (spriteArrayVisual == null)
        {
            Debug.LogWarning(
                $"{visualPrefab.name} needs a SpriteArrayVisual component.",
                this
            );
            Destroy(visual);
            return;
        }

        visual.transform.localScale = spriteArrayVisual.CounterEffectScale;
        StartCoroutine(FollowAttachedVisual(visual, anchorRenderer));
    }

    private IEnumerator FollowAttachedVisual(
        GameObject visual,
        SpriteRenderer anchorRenderer)
    {
        while (visual != null && anchorRenderer != null)
        {
            visual.transform.localPosition =
                anchorRenderer.transform.InverseTransformPoint(
                    anchorRenderer.bounds.center
                );
            yield return null;
        }
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