using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class TargetDummy : MonoBehaviour, IDamageable
{
    [Header("Target Dummy")]
    [Min(1)]
    [SerializeField] private int maxHealth = 1000;
    [Min(0f)]
    [SerializeField] private float respawnDelay = 1f;
    [SerializeField] private bool respawnOnDeath = true;
    [SerializeField] private bool showDamageNumbers = true;

    [Header("Attack")]
    [SerializeField] private bool canAttack = true;
    [Min(0f)]
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private DamageType attackDamageType = DamageType.Slash;
    [Min(0.01f)]
    [SerializeField] private float attackRange = 1.25f;
    [Min(0.01f)]
    [SerializeField] private float attackCooldown = 1.5f;
    [Min(0f)]
    [SerializeField] private float attackWindup = 0.6f;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsDead { get; private set; }

    public event Action<TargetDummy> OnHealthChanged;

    private Coroutine respawnRoutine;
    private Coroutine attackRoutine;
    private PlayerStats player;
    private float nextAttackTime;

    private void Awake()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        CurrentHealth = maxHealth;
        player = FindFirstObjectByType<PlayerStats>();

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer < 0)
        {
            Debug.LogWarning("[TargetDummy] The Enemy layer is not defined; ranged weapons using that layer mask will not target this dummy.", this);
            return;
        }

        Transform[] targetHierarchy = GetComponentsInChildren<Transform>(true);
        foreach (Transform targetTransform in targetHierarchy)
            targetTransform.gameObject.layer = enemyLayer;
    }

    private void OnEnable()
    {
        if (IsDead && respawnOnDeath)
            ScheduleRespawn();
    }

    private void Update()
    {
        if (!canAttack || IsDead)
            return;

        if (player == null)
            player = FindFirstObjectByType<PlayerStats>();

        if (player == null ||
            player.IsDead ||
            Vector2.Distance(transform.position, player.transform.position) > attackRange ||
            Time.time < nextAttackTime ||
            attackRoutine != null)
        {
            return;
        }

        nextAttackTime = Time.time + attackCooldown;
        attackRoutine = StartCoroutine(AttackAfterWindup(player));
    }

    private void OnDisable()
    {
        if (respawnRoutine != null)
        {
            StopCoroutine(respawnRoutine);
            respawnRoutine = null;
        }

        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        respawnDelay = Mathf.Max(0f, respawnDelay);
        attackDamage = Mathf.Max(0f, attackDamage);
        attackRange = Mathf.Max(0.01f, attackRange);
        attackCooldown = Mathf.Max(0.01f, attackCooldown);
        attackWindup = Mathf.Max(0f, attackWindup);
    }

    private IEnumerator AttackAfterWindup(PlayerStats target)
    {
        yield return new WaitForSeconds(attackWindup);

        if (!IsDead &&
            target != null &&
            !target.IsDead &&
            Vector2.Distance(transform.position, target.transform.position) <= attackRange)
        {
            target.TakeDamage(
                attackDamage,
                attackDamageType,
                transform.position,
                GetComponent<EnemyHealth>()
            );
        }

        attackRoutine = null;
    }

    public void TakeDamage(
        int amount,
        DamageType damageType = DamageType.Slash,
        Vector3? damageSource = null)
    {
        if (amount <= 0 || IsDead)
            return;

        int appliedDamage = Mathf.Min(amount, CurrentHealth);
        CurrentHealth -= appliedDamage;

        if (showDamageNumbers)
            DamagePopupSpawner.Spawn(transform, appliedDamage, damageType);

        OnHealthChanged?.Invoke(this);
        Debug.Log($"[TargetDummy] {name} took {appliedDamage} {damageType} damage. HP: {CurrentHealth}/{maxHealth}");

        if (CurrentHealth > 0)
            return;

        IsDead = true;

        if (respawnOnDeath && isActiveAndEnabled)
            ScheduleRespawn();
    }

    public void ResetDummy()
    {
        if (respawnRoutine != null)
        {
            StopCoroutine(respawnRoutine);
            respawnRoutine = null;
        }

        IsDead = false;
        CurrentHealth = maxHealth;
        OnHealthChanged?.Invoke(this);
    }

    private void ScheduleRespawn()
    {
        if (respawnRoutine == null)
            respawnRoutine = StartCoroutine(RespawnAfterDelay());
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        respawnRoutine = null;
        ResetDummy();
    }
}
