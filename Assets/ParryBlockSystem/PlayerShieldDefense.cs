using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerStats), typeof(PlayerWASD))]
public class PlayerShieldDefense : MonoBehaviour
{
    [Header("Blocking Skill")]
    [Range(0f, 100f)]
    [SerializeField] private float blockingSkill;
    [Min(0f)]
    [SerializeField] private float skillExperience;

    [Header("Block Tuning")]
    [Range(0.1f, 1f)]
    [SerializeField] private float minimumSkillArmorMultiplier = 0.5f;
    [Min(1f)]
    [SerializeField] private float parryArmorMultiplier = 1.5f;

    [Header("Parry Counter")]
    [Min(0f)]
    [SerializeField] private float counterOpportunityDuration = 0.6f;
    [Min(1f)]
    [SerializeField] private float counterDamageMultiplier = 1.5f;

    [Header("Defense SFX")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip blockedSfx;
    [SerializeField] private AudioClip successfulParrySfx;
    [SerializeField] private AudioClip successfulCounterSfx;

    [Header("Debug Feedback")]
    [Min(0f)]
    [SerializeField] private float feedbackHeight = 1.1f;
    [Min(0.1f)]
    [SerializeField] private float feedbackDuration = 0.7f;

    private static readonly Color GuardColor = new Color(0.45f, 0.85f, 1f);
    private static readonly Color BlockColor = new Color(0.5f, 0.9f, 1f);
    private static readonly Color ParryColor = new Color(1f, 0.85f, 0.25f);

    private PlayerStats stats;
    private PlayerWASD movement;
    private Camera mainCamera;
    private TextMeshPro feedbackText;
    private bool wasGuarding;
    private float guardStartedAt;
    private float feedbackUntil;
    private float counterOpportunityUntil;

    public bool IsGuarding { get; private set; }
    public float BlockingSkill => blockingSkill;
    public bool HasCounterOpportunity =>
        Time.time < counterOpportunityUntil;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
        movement = GetComponent<PlayerWASD>();
        mainCamera = Camera.main;
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        CreateFeedbackText();
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

        if (guarding)
        {
            if (Time.time >= feedbackUntil)
                SetFeedback("GUARD", GuardColor, float.PositiveInfinity);
        }
        else if (feedbackText.text == "GUARD")
        {
            ClearFeedback();
        }

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
        EnemyHealth attacker)
    {
        if (damage <= 0f ||
            !IsGuarding ||
            damageSource == null ||
            !HasEquippedShield())
        {
            return damage;
        }

        InventoryItem shield = EquipmentManager.Instance.GetEquippedItem(EquipmentType.Shield);
        ItemData shieldData = shield.Data;
        Vector2 directionToAttacker =
            (Vector2)(damageSource.Value - transform.position);

        if (directionToAttacker.sqrMagnitude <= 0.0001f)
            return damage;

        directionToAttacker.Normalize();
        Vector2 facing = movement.FacingDirection;
        if (facing.sqrMagnitude <= 0.0001f)
            return damage;

        float minimumDot = Mathf.Cos(shieldData.BlockArcDegrees * 0.5f * Mathf.Deg2Rad);
        if (Vector2.Dot(facing.normalized, directionToAttacker) < minimumDot)
            return damage;

        float staminaCost =
            shieldData.BlockStaminaCost +
            damage * shieldData.BlockStaminaCostPerDamage;

        if (staminaCost > 0f && !stats.UseStamina(staminaCost))
            return damage;

        float skillFactor = Mathf.Lerp(
            minimumSkillArmorMultiplier,
            1f,
            blockingSkill / 100f
        );
        bool isParry = Time.time - guardStartedAt <= shieldData.ParryWindow;
        float blockArmor =
            shieldData.BlockArmor *
            shield.UpgradeMultiplier *
            skillFactor *
            (isParry ? parryArmorMultiplier : 1f);

        GainBlockingExperience(Mathf.Min(damage, blockArmor));
        SetFeedback(
            isParry ? "PARRY! LEFT CLICK" : "BLOCK",
            isParry ? ParryColor : BlockColor,
            Time.time + feedbackDuration
        );
        PlayDefenseSfx(isParry);

        if (isParry)
        {
            counterOpportunityUntil =
                Time.time + counterOpportunityDuration;

            if (attacker != null)
                StaggerEnemy(attacker, shieldData.ParryStaggerDuration);
        }

        return Mathf.Max(0f, damage - blockArmor);
    }

    public float CounterDamageMultiplier => counterDamageMultiplier;

    public void ConsumeCounterOpportunity()
    {
        if (!HasCounterOpportunity)
            return;

        counterOpportunityUntil = 0f;
        SetFeedback(
            "COUNTER!",
            ParryColor,
            Time.time + feedbackDuration
        );
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

    private void GainBlockingExperience(float blockedDamage)
    {
        if (blockingSkill >= 100f)
            return;

        skillExperience += Mathf.Max(0.1f, blockedDamage * 0.1f);

        while (blockingSkill < 100f)
        {
            float nextLevelCost = 10f + blockingSkill * 2f;
            if (skillExperience < nextLevelCost)
                break;

            skillExperience -= nextLevelCost;
            blockingSkill = Mathf.Min(100f, blockingSkill + 1f);
        }
    }

    private void PlayDefenseSfx(bool isParry)
    {
        AudioClip clip = isParry ? successfulParrySfx : blockedSfx;
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    private void CreateFeedbackText()
    {
        GameObject label = new GameObject("Shield Debug Feedback");
        label.transform.SetParent(transform, false);
        label.transform.localPosition = Vector3.up * feedbackHeight;
        label.transform.localRotation = Quaternion.identity;

        feedbackText = label.AddComponent<TextMeshPro>();
        feedbackText.alignment = TextAlignmentOptions.Center;
        feedbackText.fontSize = 3f;
        feedbackText.fontStyle = FontStyles.Bold;
        feedbackText.textWrappingMode = TextWrappingModes.NoWrap;
        feedbackText.rectTransform.sizeDelta = new Vector2(3f, 1f);
        feedbackText.text = string.Empty;
        feedbackText.alpha = 0f;

        MeshRenderer textRenderer = feedbackText.GetComponent<MeshRenderer>();
        if (textRenderer != null)
        {
            textRenderer.sortingLayerName = "Effects";
            textRenderer.sortingOrder = 10001;
        }
    }

    private void SetFeedback(string message, Color color, float until)
    {
        feedbackText.text = message;
        feedbackText.color = color;
        feedbackText.alpha = 1f;
        feedbackUntil = until;
    }

    private void ClearFeedback()
    {
        if (feedbackText == null)
            return;

        feedbackText.text = string.Empty;
        feedbackText.alpha = 0f;
        feedbackUntil = 0f;
    }

    private void UpdateFeedbackFade()
    {
        if (feedbackText == null ||
            feedbackText.text == "GUARD")
        {
            return;
        }

        if (feedbackText.text == "PARRY! LEFT CLICK" &&
            !HasCounterOpportunity)
        {
            feedbackText.text = "PARRY!";
            feedbackUntil = Mathf.Min(
                feedbackUntil,
                Time.time + feedbackDuration * 0.5f
            );
        }

        if (feedbackUntil <= 0f)
            return;

        feedbackText.alpha = Mathf.Clamp01(
            (feedbackUntil - Time.time) / feedbackDuration
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
