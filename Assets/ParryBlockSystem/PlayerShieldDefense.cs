using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerStats), typeof(PlayerWASD))]
public class PlayerShieldDefense : MonoBehaviour
{
    public const float MinimumBlockChance = 0.1f;
    public const float MaximumBlockChance = 0.7f;
    public const float DefaultParryArmorMultiplier = 1.5f;
    public const float DefaultCounterOpportunityDuration = 0.6f;
    public const float DefaultCounterDamageMultiplier = 1.5f;

    [Header("Block Tuning")]
    [Min(1f)]
    [SerializeField] private float parryArmorMultiplier = DefaultParryArmorMultiplier;

    [Header("Parry Counter")]
    [Min(0f)]
    [SerializeField] private float counterOpportunityDuration =
        DefaultCounterOpportunityDuration;
    [Min(1f)]
    [SerializeField] private float counterDamageMultiplier = DefaultCounterDamageMultiplier;

    [Header("Defense SFX")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip blockedSfx;
    [SerializeField] private AudioClip successfulParrySfx;
    [SerializeField] private AudioClip successfulCounterSfx;

    [Header("Defense Feedback")]
    [Min(0.1f)]
    [SerializeField] private float feedbackDuration = 0.7f;
    [Tooltip("Displayed icon height in world units.")]
    [Min(0.05f)]
    [SerializeField] private float feedbackIconHeight = 0.35f;
    [Tooltip("Maximum position offset for icon shake, in local units.")]
    [Min(0f)]
    [SerializeField] private float feedbackShakeAmount = 0.025f;
    [Tooltip("Shake oscillations per second.")]
    [Min(0f)]
    [SerializeField] private float feedbackShakeFrequency = 22f;
    [SerializeField] private Transform headAnchor;
    [SerializeField] private Sprite parryFeedbackSprite;
    [SerializeField] private Sprite blockedFeedbackSprite;
    [SerializeField] private Sprite unsuccessfulBlockFeedbackSprite;
    [SerializeField] private Sprite counterFeedbackSprite;

    private PlayerStats stats;
    private PlayerWASD movement;
    private CharacterRenderer characterRenderer;
    private CharacterSpriteController characterSpriteController;
    private Camera mainCamera;
    private SpriteRenderer feedbackIcon;
    private bool wasGuarding;
    private float guardStartedAt;
    private float feedbackUntil;
    private float counterOpportunityUntil;
    private float counterOpportunityDamageMultiplier = 1f;

    public bool IsGuarding { get; private set; }
    public float ParryArmorMultiplier => parryArmorMultiplier;
    public float CounterOpportunityDuration => counterOpportunityDuration;
    public float BaseCounterDamageMultiplier => counterDamageMultiplier;
    public bool HasCounterOpportunity =>
        Time.time < counterOpportunityUntil;

    public static float GetBlockChance(int upgradeLevel)
    {
        float levelProgress = Mathf.Clamp(
            upgradeLevel,
            0,
            InventoryItem.MaximumUpgradeLevel
        ) / (float)InventoryItem.MaximumUpgradeLevel;
        return Mathf.Lerp(MinimumBlockChance, MaximumBlockChance, levelProgress);
    }

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
        movement = GetComponent<PlayerWASD>();
        characterRenderer = GetComponent<CharacterRenderer>();
        characterSpriteController = GetComponent<CharacterSpriteController>();
        mainCamera = Camera.main;
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        CreateFeedbackDisplay();
    }

    private void Update()
    {
        bool canGuard =
            Time.timeScale > 0f &&
            CombatInputGate.IsCombatInputAllowed &&
            !EquipmentUpgradeMerchant.IsUIOpen &&
            (GameManager.Instance == null || !GameManager.Instance.IsPaused) &&
            (PauseMenuUI.Instance == null || !PauseMenuUI.Instance.IsOpen) &&
            (stats == null || !stats.IsDead) &&
            HasEquippedShield();

        Mouse mouse = Mouse.current;
        bool guarding = canGuard && mouse != null && mouse.rightButton.isPressed;

        if (guarding && !wasGuarding)
            guardStartedAt = Time.time;

        IsGuarding = guarding;
        wasGuarding = guarding;

        UpdateFeedbackFade();
    }

    private void LateUpdate()
    {
        if (IsGuarding)
            FaceCursorWhileGuarding();
    }

    private void OnDisable()
    {
        IsGuarding = false;
        wasGuarding = false;
        ClearFeedback();
    }

    public float ModifyIncomingDamage(
        float damage,
        Vector3? damageSource,
        EnemyHealth attacker,
        out bool wasBlocked)
    {
        wasBlocked = false;

        if (damage <= 0f ||
            !IsGuarding ||
            (damageSource == null && attacker == null) ||
            !HasEquippedShield())
        {
            return damage;
        }

        InventoryItem shield = EquipmentManager.Instance.GetEquippedItem(EquipmentType.Shield);
        ItemData shieldData = shield.Data;
        Vector3 attackerPosition = attacker != null
            ? attacker.transform.position
            : damageSource.Value;
        Vector2 directionToAttacker =
            (Vector2)(attackerPosition - transform.position);

        if (directionToAttacker.sqrMagnitude <= 0.0001f)
        {
            SetFeedback(
                unsuccessfulBlockFeedbackSprite,
                Time.time + feedbackDuration
            );
            return damage;
        }

        directionToAttacker.Normalize();
        Vector2 facing = movement.FacingDirection;
        if (facing.sqrMagnitude <= 0.0001f)
        {
            SetFeedback(
                unsuccessfulBlockFeedbackSprite,
                Time.time + feedbackDuration
            );
            return damage;
        }

        float minimumDot = Mathf.Cos(shieldData.BlockArcDegrees * 0.5f * Mathf.Deg2Rad);
        if (Vector2.Dot(facing.normalized, directionToAttacker) < minimumDot)
        {
            SetFeedback(
                unsuccessfulBlockFeedbackSprite,
                Time.time + feedbackDuration
            );
            return damage;
        }

        float staminaCost =
            shieldData.BlockStaminaCost +
            damage * shieldData.BlockStaminaCostPerDamage;

        if (staminaCost > 0f && !stats.UseStamina(staminaCost))
        {
            SetFeedback(
                unsuccessfulBlockFeedbackSprite,
                Time.time + feedbackDuration
            );
            return damage;
        }

        bool isParry = Time.time - guardStartedAt <= shieldData.ParryWindow;
        if (!isParry && Random.value >= GetBlockChance(shield.UpgradeLevel))
        {
            SetFeedback(
                unsuccessfulBlockFeedbackSprite,
                Time.time + feedbackDuration
            );
            return damage;
        }
        wasBlocked = true;

        float blockArmor =
            shieldData.BlockArmor *
            shield.UpgradeMultiplier *
            (isParry ? parryArmorMultiplier : 1f);

        SetFeedback(
            isParry ? parryFeedbackSprite : blockedFeedbackSprite,
            Time.time + feedbackDuration
        );
        if (characterRenderer != null)
            characterRenderer.PlayShieldDefenseVisual(isParry);
        if (isParry && characterSpriteController != null)
            characterSpriteController.PlayWeaponAction(WeaponAttackType.Melee);
        PlayDefenseSfx(isParry);

        if (isParry)
        {
            counterOpportunityUntil =
                Time.time + counterOpportunityDuration;
            counterOpportunityDamageMultiplier =
                counterDamageMultiplier * shield.UpgradeMultiplier;

            if (attacker != null)
                StaggerEnemy(attacker, shieldData.ParryStaggerDuration);
        }

        return Mathf.Max(0f, damage - blockArmor);
    }

    public float CounterDamageMultiplier => counterOpportunityDamageMultiplier;

    public void ConsumeCounterOpportunity()
    {
        if (!HasCounterOpportunity)
            return;

        counterOpportunityUntil = 0f;
        counterOpportunityDamageMultiplier = counterDamageMultiplier;
        SetFeedback(counterFeedbackSprite, Time.time + feedbackDuration);
        if (successfulCounterSfx != null && audioSource != null)
            audioSource.PlayOneShot(successfulCounterSfx);
    }

    private bool HasEquippedShield()
    {
        return EquipmentManager.Instance != null &&
            EquipmentManager.Instance.GetEquippedItem(EquipmentType.Shield) is InventoryItem shield &&
            shield.Data != null &&
            shield.Data.BlockArmor > 0f;
    }

    private void FaceCursorWhileGuarding()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
            return;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        Vector2 mousePosition = mouse.position.ReadValue();
        Vector3 cursorWorld = mainCamera.ScreenToWorldPoint(
            new Vector3(mousePosition.x, mousePosition.y, -mainCamera.transform.position.z)
        );
        Vector2 direction = cursorWorld - transform.position;

        if (direction.sqrMagnitude > 0.0001f)
            movement.FaceDirection(direction);
    }

    private void PlayDefenseSfx(bool isParry)
    {
        AudioClip clip = isParry ? successfulParrySfx : blockedSfx;
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    private void CreateFeedbackDisplay()
    {
        if (headAnchor == null)
            headAnchor = FindHeadAnchor();

        if (headAnchor == null)
        {
            Debug.LogError(
                "PlayerShieldDefense requires a child Transform named HeadAnchor.",
                this
            );
            return;
        }

        GameObject feedback = new GameObject("Shield Defense Feedback");
        feedback.transform.SetParent(headAnchor, false);
        feedback.transform.localPosition = Vector3.zero;
        feedback.transform.localRotation = Quaternion.identity;

        feedbackIcon = feedback.AddComponent<SpriteRenderer>();
        feedbackIcon.enabled = false;
        feedbackIcon.sortingLayerName = "Player";
        feedbackIcon.sortingOrder = 100;
    }

    private Transform FindHeadAnchor()
    {
        Transform[] childTransforms = GetComponentsInChildren<Transform>(true);
        foreach (Transform childTransform in childTransforms)
        {
            if (childTransform != transform &&
                childTransform.name == "HeadAnchor")
            {
                return childTransform;
            }
        }

        return null;
    }

    private void SetFeedback(Sprite sprite, float until)
    {
        if (feedbackIcon == null)
            return;

        if (sprite == null)
        {
            Debug.LogError(
                "A shield defense feedback sprite is not assigned.",
                this
            );
            ClearFeedback();
            return;
        }

        feedbackIcon.sprite = sprite;
        feedbackIcon.color = Color.white;
        feedbackIcon.transform.localScale =
            Vector3.one * (feedbackIconHeight / sprite.bounds.size.y);
        feedbackIcon.transform.localPosition = Vector3.zero;
        feedbackIcon.transform.localRotation = Quaternion.identity;
        feedbackIcon.enabled = true;
        feedbackUntil = until;
    }

    private void ClearFeedback()
    {
        if (feedbackIcon != null)
        {
            feedbackIcon.enabled = false;
            feedbackIcon.transform.localPosition = Vector3.zero;
            feedbackIcon.transform.localRotation = Quaternion.identity;
        }
        feedbackUntil = 0f;
    }

    private void UpdateFeedbackFade()
    {
        if (feedbackIcon == null ||
            !feedbackIcon.enabled ||
            feedbackUntil <= 0f)
        {
            return;
        }

        Color color = feedbackIcon.color;
        float remaining = feedbackUntil - Time.time;
        color.a = Mathf.Clamp01(remaining / feedbackDuration);
        feedbackIcon.color = color;

        float elapsed = feedbackDuration - remaining;
        float shakeEnvelope = Mathf.Clamp01(remaining / feedbackDuration);
        float shakePhase = elapsed * feedbackShakeFrequency * Mathf.PI * 2f;
        feedbackIcon.transform.localPosition = new Vector3(
            Mathf.Sin(shakePhase) * feedbackShakeAmount * shakeEnvelope,
            Mathf.Sin(shakePhase * 1.37f) *
                feedbackShakeAmount * 0.5f * shakeEnvelope,
            0f
        );
        feedbackIcon.transform.localRotation = Quaternion.Euler(
            0f,
            0f,
            Mathf.Sin(shakePhase * 0.8f) * 3f * shakeEnvelope
        );

        if (Time.time >= feedbackUntil)
            ClearFeedback();
    }

    private static void StaggerEnemy(EnemyHealth attacker, float duration)
    {
        EnemyMelee melee = attacker.GetComponent<EnemyMelee>();
        if (melee != null)
        {
            melee.ApplyStagger(duration);
            return;
        }

        EnemyRange ranged = attacker.GetComponent<EnemyRange>();
        if (ranged != null)
            ranged.ApplyStagger(duration);
    }
}
