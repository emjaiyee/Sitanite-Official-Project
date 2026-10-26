using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterRenderer : MonoBehaviour
{
    private static readonly Color CounterAttackColor = new Color(1f, 0.12f, 0.12f);
    private const int DashAfterimageCount = 8;
    private const float DashAfterimageInterval = 0.025f;
    private const float DashAfterimageFadeDuration = 0.18f;
    private static readonly Color DashAfterimageTint =
        new Color(1.5f, 1.5f, 1.5f, 1.5f);

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

    [Header("Level Up Animation")]
    [SerializeField] private Sprite[] levelUpAnimationFrames = new Sprite[8];
    [Min(1f)] [SerializeField] private float levelUpAnimationFramesPerSecond = 12f;
    [Min(0.01f)] [SerializeField] private float levelUpAnimationScale = 0.5f;
    [Range(0f, 1f)] [SerializeField] private float levelUpAnimationOpacity = 0.5f;

    public GameObject CriticalHitVisualPrefab => criticalHitVisualPrefab;

    private CharacterDirection currentDirection = CharacterDirection.SouthWest;
    private CharacterAnimationState currentAnimationState = CharacterAnimationState.Idle;
    private int currentAnimationFrame;
    private Coroutine counterAttackFlashRoutine;
    private Coroutine dashAfterimageRoutine;
    private Coroutine levelUpAnimationRoutine;
    private readonly List<GameObject> activeDashAfterimages = new List<GameObject>();
    private SpriteRenderer levelUpAnimationRenderer;
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
        CreateLevelUpAnimationRenderer();
    }

    private void OnEnable()
    {
        PlayerStats playerStats = GetComponent<PlayerStats>();
        if (playerStats != null)
            playerStats.LeveledUp += PlayLevelUpAnimation;
    }

    private void OnDisable()
    {
        StopCounterAttackFlash();
        StopDashAfterimages();

        PlayerStats playerStats = GetComponent<PlayerStats>();
        if (playerStats != null)
            playerStats.LeveledUp -= PlayLevelUpAnimation;

        if (levelUpAnimationRoutine != null)
        {
            StopCoroutine(levelUpAnimationRoutine);
            levelUpAnimationRoutine = null;
        }

        if (levelUpAnimationRenderer != null)
        {
            levelUpAnimationRenderer.sprite = null;
            levelUpAnimationRenderer.enabled = false;
        }
    }

    private void CreateLevelUpAnimationRenderer()
    {
        GameObject animationObject = new GameObject("LevelUpAnimation");
        animationObject.transform.SetParent(transform, false);
        animationObject.transform.localScale = Vector3.one * levelUpAnimationScale;
        levelUpAnimationRenderer = animationObject.AddComponent<SpriteRenderer>();
        levelUpAnimationRenderer.color = new Color(1f, 1f, 1f, levelUpAnimationOpacity);
        levelUpAnimationRenderer.enabled = false;

        SpriteRenderer sortingReference = bodyRenderer != null
            ? bodyRenderer
            : GetComponentInChildren<SpriteRenderer>();
        if (sortingReference != null)
        {
            levelUpAnimationRenderer.sortingLayerID = sortingReference.sortingLayerID;
            levelUpAnimationRenderer.sortingOrder = sortingReference.sortingOrder;

            foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>())
            {
                if (renderer != levelUpAnimationRenderer &&
                    renderer.sortingLayerID == sortingReference.sortingLayerID)
                {
                    levelUpAnimationRenderer.sortingOrder = Mathf.Max(
                        levelUpAnimationRenderer.sortingOrder,
                        renderer.sortingOrder
                    );
                }
            }

            levelUpAnimationRenderer.sortingOrder++;
        }
    }

    private void PlayLevelUpAnimation()
    {
        if (levelUpAnimationRenderer == null ||
            levelUpAnimationFrames == null ||
            levelUpAnimationFrames.Length == 0)
        {
            return;
        }

        if (levelUpAnimationRoutine != null)
            StopCoroutine(levelUpAnimationRoutine);

        levelUpAnimationRoutine = StartCoroutine(PlayLevelUpAnimationRoutine());
    }

    private IEnumerator PlayLevelUpAnimationRoutine()
    {
        float frameDuration = 1f / Mathf.Max(1f, levelUpAnimationFramesPerSecond);
        foreach (Sprite frame in levelUpAnimationFrames)
        {
            levelUpAnimationRenderer.sprite = frame;
            levelUpAnimationRenderer.enabled = frame != null;
            yield return new WaitForSeconds(frameDuration);
        }

        levelUpAnimationRenderer.sprite = null;
        levelUpAnimationRenderer.enabled = false;
        levelUpAnimationRoutine = null;
    }

    public void PlayDashAfterimages()
    {
        if (dashAfterimageRoutine != null)
            StopCoroutine(dashAfterimageRoutine);

        dashAfterimageRoutine = StartCoroutine(SpawnDashAfterimages());
    }

    public void PlayRangedShotFlash(Transform firePoint, Vector2 direction)
    {
        AttackHitIconVisual.PlayMuzzleFlash(
            firePoint,
            GetAttackWeaponRenderer(),
            direction
        );
    }

    public void PlayMageCastFlash(Transform firePoint, Vector2 direction)
    {
        AttackHitIconVisual.PlaySpellCastFlash(
            firePoint,
            GetAttackWeaponRenderer(),
            direction
        );
    }

    private IEnumerator SpawnDashAfterimages()
    {
        for (int index = 0; index < DashAfterimageCount; index++)
        {
            CreateDashAfterimage();
            yield return new WaitForSeconds(DashAfterimageInterval);
        }

        dashAfterimageRoutine = null;
    }

    private void CreateDashAfterimage()
    {
        GameObject afterimage = new GameObject("DashAfterimage");
        afterimage.transform.SetPositionAndRotation(transform.position, transform.rotation);
        activeDashAfterimages.Add(afterimage);

        SpriteRenderer[] sourceRenderers =
        {
            weaponUnderRenderer,
            bodyRenderer,
            legsRenderer,
            torsoRenderer,
            eyesRenderer,
            hairRenderer,
            headwearRenderer,
            weaponOverRenderer,
            shieldRenderer
        };
        List<SpriteRenderer> afterimageRenderers = new List<SpriteRenderer>();

        foreach (SpriteRenderer source in sourceRenderers)
        {
            if (source == null || !source.enabled || source.sprite == null)
                continue;

            GameObject part = new GameObject(source.name);
            part.layer = source.gameObject.layer;
            part.transform.SetParent(afterimage.transform, false);
            part.transform.localPosition =
                transform.InverseTransformPoint(source.transform.position);
            part.transform.localRotation =
                Quaternion.Inverse(transform.rotation) * source.transform.rotation;
            part.transform.localScale = source.transform.lossyScale * 1.1f;

            SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = source.sprite;
            renderer.sharedMaterial = source.sharedMaterial;
            renderer.color = new Color(
                DashAfterimageTint.r,
                DashAfterimageTint.g,
                DashAfterimageTint.b,
                source.color.a * DashAfterimageTint.a
            );
            renderer.flipX = source.flipX;
            renderer.flipY = source.flipY;
            renderer.sortingLayerID = source.sortingLayerID;
            renderer.sortingOrder = source.sortingOrder - 1;
            renderer.maskInteraction = source.maskInteraction;
            renderer.drawMode = source.drawMode;
            renderer.size = source.size;
            afterimageRenderers.Add(renderer);
        }

        if (afterimageRenderers.Count == 0)
        {
            activeDashAfterimages.Remove(afterimage);
            Destroy(afterimage);
            return;
        }

        StartCoroutine(FadeDashAfterimage(afterimage, afterimageRenderers));
    }

    private IEnumerator FadeDashAfterimage(
        GameObject afterimage,
        List<SpriteRenderer> renderers)
    {
        List<Color> initialColors = new List<Color>(renderers.Count);
        foreach (SpriteRenderer renderer in renderers)
            initialColors.Add(renderer.color);

        float elapsed = 0f;
        while (elapsed < DashAfterimageFadeDuration)
        {
            float alpha = 1f - elapsed / DashAfterimageFadeDuration;
            for (int index = 0; index < renderers.Count; index++)
            {
                if (renderers[index] == null)
                    continue;

                Color color = initialColors[index];
                color.a *= alpha;
                renderers[index].color = color;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        activeDashAfterimages.Remove(afterimage);
        Destroy(afterimage);
    }

    private void StopDashAfterimages()
    {
        if (dashAfterimageRoutine != null)
        {
            StopCoroutine(dashAfterimageRoutine);
            dashAfterimageRoutine = null;
        }

        foreach (GameObject afterimage in activeDashAfterimages)
        {
            if (afterimage != null)
                Destroy(afterimage);
        }

        activeDashAfterimages.Clear();
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

    public bool TryGetWeaponTipPosition(
        Vector2 direction,
        out Vector3 tipPosition)
    {
        SpriteRenderer renderer =
            weaponUnderRenderer != null &&
            weaponUnderRenderer.enabled &&
            weaponUnderRenderer.sprite != null
                ? weaponUnderRenderer
                : weaponOverRenderer;

        if (renderer == null ||
            !renderer.enabled ||
            renderer.sprite == null ||
            direction.sqrMagnitude <= 0.0001f)
        {
            tipPosition = transform.position;
            return false;
        }

        direction.Normalize();
        Bounds weaponBounds = renderer.bounds;
        float extentAlongDirection =
            Mathf.Abs(direction.x) * weaponBounds.extents.x +
            Mathf.Abs(direction.y) * weaponBounds.extents.y;

        tipPosition =
            weaponBounds.center + (Vector3)(direction * extentAlongDirection);
        return true;
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