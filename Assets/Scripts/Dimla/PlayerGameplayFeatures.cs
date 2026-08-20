using UnityEngine;

using System.Collections;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerStats), typeof(Rigidbody2D))]
public class PlayerGameplayFeatures : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerWASD movement;
    [SerializeField] private Transform actionOrigin;

    [Header("Stamina Costs")]
    [Min(0)][SerializeField] private float moveCostPerSecond = 4f;
    [Min(0)][SerializeField] private float staminaRegenPerSecond = 12f;
    [Min(0)][SerializeField] private float healthRegenPerSecond = 2f;
    [Min(0)][SerializeField] private int attackCost = 10;
    [Min(0)][SerializeField] private int dashCost = 20;
    [Min(0)][SerializeField] private float dashDistance = 1.5f;
    [Min(0.01f)][SerializeField] private float dashDuration = 0.12f;
    [Range(0f, 1f)][SerializeField] private float exhaustedSpeedMultiplier = 0.35f;

    [Header("Mana")]
    [Min(0)][SerializeField] private int skillManaCost = 25;
    [Min(0.01f)][SerializeField] private float manaRegenPerSecond = 2f;

    [Header("Attack Prototype")]
    [Min(0)][SerializeField] private int attackDamage = 4;
    [Min(0.01f)][SerializeField] private float attackRange = 0.65f;
    [Min(0.01f)][SerializeField] private float attackWidth = 0.8f;
    [SerializeField] private LayerMask attackLayers = Physics2D.AllLayers;

    [Header("Scene Prototype")]
    [SerializeField] private bool drawActionPrototypes = true;
    [SerializeField] private Color attackColor = new Color(1f, 0.2f, 0.1f, 0.35f);
    [SerializeField] private Color dashColor = new Color(0.1f, 0.6f, 1f, 0.35f);

    private PlayerStats stats;
    private Rigidbody2D body;
    private Vector2 facing = Vector2.right;
    private bool dashing;
    private float staminaDrainBuffer;
    private float staminaRegenBuffer;
    private float healthRegenBuffer;
    private float manaRegenBuffer;
    private const float DeadPlayerOpacity = 0.5f;
    private bool playerDead;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
        body = GetComponent<Rigidbody2D>();
        if (movement == null) movement = GetComponent<PlayerWASD>();
        if (actionOrigin == null) actionOrigin = transform;
    }

    private void OnEnable()
    {
        if (stats != null)
            stats.Died += HandlePlayerDied;
    }

    private void OnDisable()
    {
        if (stats != null)
            stats.Died -= HandlePlayerDied;
    }

    private void Update()
    {
        if (playerDead || stats.IsDead)
        {
            if (movement != null)
                movement.SpeedMultiplier = 0f;
            return;
        }

        UpdateMovementMultiplier();
        if (movement != null && movement.MoveDirection.sqrMagnitude > 0.01f) facing = movement.MoveDirection.normalized;
        bool leftClick = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool rightClick = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
        bool attackPressed = leftClick || rightClick;
        bool dashPressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        bool skillPressed = Keyboard.current != null && Keyboard.current.zKey.wasPressedThisFrame;
        if (!dashing && attackPressed) Attack();
        if (!dashing && dashPressed) Dash();
        if (skillPressed) UseSkill();
        manaRegenBuffer += manaRegenPerSecond * Time.deltaTime;
        if (stats.CurrentMana < stats.maxMana && manaRegenBuffer >= 1f)
        {
            int restoreAmount = Mathf.FloorToInt(manaRegenBuffer);
            manaRegenBuffer -= restoreAmount;
            stats.RestoreMana(restoreAmount);
        }
        bool isMoving = movement != null && movement.MoveDirection.sqrMagnitude > 0.01f;
        if (isMoving)
        {
            staminaDrainBuffer += moveCostPerSecond * Time.deltaTime;
            if (staminaDrainBuffer >= 1f)
            {
                int drainAmount = Mathf.FloorToInt(staminaDrainBuffer);
                staminaDrainBuffer -= drainAmount;
                stats.UseStamina(drainAmount);
            }
        }

        if (!isMoving && stats.CurrentStamina < stats.maxStamina)
        {
            staminaRegenBuffer += staminaRegenPerSecond * Time.deltaTime;
            if (staminaRegenBuffer >= 1f)
            {
                int restoreAmount = Mathf.FloorToInt(staminaRegenBuffer);
                staminaRegenBuffer -= restoreAmount;
                stats.RestoreStamina(restoreAmount);
            }
        }

        if (stats.CurrentHealth > 0 && stats.CurrentHealth < stats.maxHealth)
        {
            healthRegenBuffer += healthRegenPerSecond * Time.deltaTime;
            if (healthRegenBuffer >= 1f)
            {
                int restoreAmount = Mathf.FloorToInt(healthRegenBuffer);
                healthRegenBuffer -= restoreAmount;
                stats.Heal(restoreAmount);
            }
        }

    }

    private void UpdateMovementMultiplier()
    {
        if (movement == null) return;
        movement.SpeedMultiplier = stats.CurrentStamina > 0 ? 1f : exhaustedSpeedMultiplier;
    }

    private void Attack()
    {
        if (playerDead || stats.IsDead) return;
        if (Mouse.current == null || Camera.main == null) return;

        Vector2 screenPosition = Mouse.current.position.ReadValue();
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(
            screenPosition.x,
            screenPosition.y,
            Mathf.Abs(Camera.main.transform.position.z)));
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPosition, attackLayers);
        BreakablePot potTarget = null;
        float closestPotDistanceSqr = float.MaxValue;
        EnemyHealth enemyTarget = null;
        float closestEnemyDistanceSqr = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            BreakablePot pot = hit.GetComponentInParent<BreakablePot>();
            if (pot != null && !pot.IsBroken)
            {
                float distanceSqr = ((Vector2)pot.transform.position - (Vector2)actionOrigin.position).sqrMagnitude;
                if (distanceSqr < closestPotDistanceSqr)
                {
                    potTarget = pot;
                    closestPotDistanceSqr = distanceSqr;
                }
                continue;
            }

            EnemyHealth enemy = hit.GetComponentInParent<EnemyHealth>();
            if (enemy == null) continue;

            float enemyDistanceSqr = ((Vector2)enemy.transform.position - (Vector2)actionOrigin.position).sqrMagnitude;
            if (enemyDistanceSqr < closestEnemyDistanceSqr)
            {
                enemyTarget = enemy;
                closestEnemyDistanceSqr = enemyDistanceSqr;
            }
        }

        if (potTarget != null)
        {
            TryBreakPot(potTarget);
            return;
        }

        EnemyHealth target = enemyTarget;
        if (target == null) return;

        float maxAttackDistance = attackRange + attackWidth * 0.5f;
        if (!stats.UseStamina(attackCost)) return;

        Vector2 targetDirection = (Vector2)target.transform.position - (Vector2)actionOrigin.position;
        if (targetDirection.sqrMagnitude > 0.01f)
            facing = targetDirection.normalized;

        Debug.Log($"MOUSE CLICK - ATTACK {target.name} for {attackDamage} damage");
        target.ApplyDamage(attackDamage);
    }

    /// <summary>Attempts to break a nearby pot using the player's attack stamina cost.</summary>
    public bool TryBreakPot(BreakablePot pot)
    {
        if (playerDead || stats.IsDead || pot == null || pot.IsBroken)
            return false;

        Transform origin = actionOrigin == null ? transform : actionOrigin;
        float range = Mathf.Max(attackRange + attackWidth * 0.5f, pot.InteractionRange);
        Vector2 offset = (Vector2)pot.transform.position - (Vector2)origin.position;
        if (offset.sqrMagnitude > range * range)
            return false;

        if (!stats.UseStamina(attackCost))
            return false;

        if (offset.sqrMagnitude > 0.01f)
            facing = offset.normalized;

        bool broke = pot.TryBreak();
        if (broke)
            Debug.Log($"MOUSE CLICK - BROKE {pot.name}");
        return broke;
    }


    private void Dash()
    {
        if (!stats.UseStamina(dashCost)) return;
        Debug.Log("is dashing");
        StartCoroutine(DashRoutine());
    }

    private IEnumerator DashRoutine()
    {
        dashing = true;
        Vector2 start = body.position;
        Vector2 end = start + (facing.sqrMagnitude > 0.01f ? facing : Vector2.right) * dashDistance;
        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            elapsed += Time.fixedDeltaTime;
            body.MovePosition(Vector2.Lerp(start, end, elapsed / dashDuration));
            yield return new WaitForFixedUpdate();
        }
        body.MovePosition(end);
        dashing = false;
    }

    private void HandlePlayerDied()
    {
        playerDead = true;
        dashing = false;
        if (movement != null)
            movement.SpeedMultiplier = 0f;

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer renderer in renderers)
        {
            Color color = renderer.color;
            color.a = DeadPlayerOpacity;
            renderer.color = color;
        }

        Debug.Log("[PlayerGameplayFeatures] Player attack disabled because player is dead.");
    }

    private void UseSkill()
    {
        if (stats.UseMana(skillManaCost)) Debug.Log("used z skill");
    }

    private void OnDrawGizmos()
    {
        if (!drawActionPrototypes) return;
        Transform origin = actionOrigin == null ? transform : actionOrigin;
        Vector3 direction = facing.sqrMagnitude > 0.01f ? (Vector3)facing : Vector3.right;
        Gizmos.color = attackColor;
        Gizmos.DrawCube(origin.position + direction * attackRange, new Vector3(attackWidth, attackWidth, 0.05f));
        Gizmos.color = dashColor;
        Gizmos.DrawWireCube(origin.position + direction * (dashDistance * 0.5f), new Vector3(dashDistance, 0.15f, 0.05f));
    }
}
