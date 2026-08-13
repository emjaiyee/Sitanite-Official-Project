using UnityEngine;

[RequireComponent(typeof(PlayerStats), typeof(Rigidbody2D))]
public class PlayerGameplayFeatures : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerWASD movement;

    [Header("Stamina")]
    [Min(0)]
    [SerializeField] private float moveCostPerSecond = 4f;

    [Min(0)]
    [SerializeField] private float staminaRegenPerSecond = 12f;

    [Range(0f, 1f)]
    [SerializeField] private float exhaustedSpeedMultiplier = 0.35f;

    [Header("Health")]
    [Min(0)]
    [SerializeField] private float healthRegenPerSecond = 2f;

    [Header("Mana")]
    [Min(0)]
    [SerializeField] private int skillManaCost = 25;

    [Min(0.01f)]
    [SerializeField] private float manaRegenPerSecond = 2f;

    [Header("Skill Input")]
    [SerializeField] private UnityEngine.InputSystem.InputActionReference skillAction;

    [Header("Death Visual")]
    [SerializeField] private float deadPlayerOpacity = 0.5f;

    private PlayerStats stats;

    private float staminaDrainBuffer;
    private float staminaRegenBuffer;
    private float healthRegenBuffer;
    private float manaRegenBuffer;

    private bool playerDead;


    // -------------------------------------------------
    // UNITY
    // -------------------------------------------------

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();

        if (movement == null)
            movement = GetComponent<PlayerWASD>();
    }


    private void OnEnable()
    {
        if (stats != null)
            stats.Died += HandlePlayerDied;

        if (skillAction != null)
        {
            skillAction.action.Enable();
            skillAction.action.performed += OnSkillPerformed;
        }
    }


    private void OnDisable()
    {
        if (stats != null)
            stats.Died -= HandlePlayerDied;

        if (skillAction != null)
        {
            skillAction.action.performed -= OnSkillPerformed;
            skillAction.action.Disable();
        }
    }


    private void Update()
    {
        if (stats == null)
            return;

        if (playerDead || stats.IsDead)
        {
            if (movement != null)
                movement.SpeedMultiplier = 0f;

            return;
        }

        UpdateMovementMultiplier();
        UpdateStamina();
        UpdateHealth();
        UpdateMana();
    }


    // -------------------------------------------------
    // MOVEMENT / STAMINA
    // -------------------------------------------------

    private void UpdateMovementMultiplier()
    {
        if (movement == null)
            return;

        if (stats.CurrentStamina > 0)
        {
            movement.SpeedMultiplier = 1f;
        }
        else
        {
            movement.SpeedMultiplier = exhaustedSpeedMultiplier;
        }
    }


    private void UpdateStamina()
    {
        bool isMoving =
            movement != null &&
            movement.MoveDirection.sqrMagnitude > 0.01f;


        // ---------------------------------------------
        // Stamina drain while moving
        // ---------------------------------------------

        if (isMoving)
        {
            staminaDrainBuffer +=
                moveCostPerSecond * Time.deltaTime;

            if (staminaDrainBuffer >= 1f)
            {
                int drainAmount =
                    Mathf.FloorToInt(staminaDrainBuffer);

                staminaDrainBuffer -= drainAmount;

                stats.UseStamina(drainAmount);
            }

            // Don't regenerate while moving.
            staminaRegenBuffer = 0f;
        }


        // ---------------------------------------------
        // Stamina regeneration while stationary
        // ---------------------------------------------

        else if (stats.CurrentStamina < stats.maxStamina)
        {
            staminaRegenBuffer +=
                staminaRegenPerSecond * Time.deltaTime;

            if (staminaRegenBuffer >= 1f)
            {
                int restoreAmount =
                    Mathf.FloorToInt(staminaRegenBuffer);

                staminaRegenBuffer -= restoreAmount;

                stats.RestoreStamina(restoreAmount);
            }
        }
        else
        {
            staminaRegenBuffer = 0f;
        }
    }


    // -------------------------------------------------
    // HEALTH
    // -------------------------------------------------

    private void UpdateHealth()
    {
        if (stats.CurrentHealth <= 0)
            return;

        if (stats.CurrentHealth >= stats.maxHealth)
        {
            healthRegenBuffer = 0f;
            return;
        }


        healthRegenBuffer +=
            healthRegenPerSecond * Time.deltaTime;


        if (healthRegenBuffer >= 1f)
        {
            int restoreAmount =
                Mathf.FloorToInt(healthRegenBuffer);

            healthRegenBuffer -= restoreAmount;

            stats.Heal(restoreAmount);
        }
    }


    // -------------------------------------------------
    // MANA
    // -------------------------------------------------

    private void UpdateMana()
    {
        if (stats.CurrentMana >= stats.maxMana)
        {
            manaRegenBuffer = 0f;
            return;
        }


        manaRegenBuffer +=
            manaRegenPerSecond * Time.deltaTime;


        if (manaRegenBuffer >= 1f)
        {
            int restoreAmount =
                Mathf.FloorToInt(manaRegenBuffer);

            manaRegenBuffer -= restoreAmount;

            stats.RestoreMana(restoreAmount);
        }
    }


    // -------------------------------------------------
    // SKILL
    // -------------------------------------------------

    private void OnSkillPerformed(
        UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        UseSkill();
    }


    private void UseSkill()
    {
        if (playerDead || stats.IsDead)
            return;

        if (stats.UseMana(skillManaCost))
        {
            Debug.Log("Skill activated!");
        }
    }


    // -------------------------------------------------
    // PLAYER DEATH
    // -------------------------------------------------

    private void HandlePlayerDied()
    {
        playerDead = true;

        if (movement != null)
            movement.SpeedMultiplier = 0f;


        SpriteRenderer[] renderers =
            GetComponentsInChildren<SpriteRenderer>(true);


        foreach (SpriteRenderer renderer in renderers)
        {
            Color color = renderer.color;

            color.a = deadPlayerOpacity;

            renderer.color = color;
        }


        Debug.Log(
            "[PlayerGameplayFeatures] Player has died. Gameplay disabled."
        );
    }
}