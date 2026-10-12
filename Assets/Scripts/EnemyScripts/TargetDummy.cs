using System;
using System.Collections;
using System.Collections.Generic;
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
    [Min(0.01f)]
    [SerializeField] private float attackHitWindow = 0.25f;
    [SerializeField] private SpriteArrayVisual attackVisualPrefab;

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
        attackHitWindow = Mathf.Max(0.01f, attackHitWindow);
    }

    private IEnumerator AttackAfterWindup(PlayerStats target)
    {
        yield return new WaitForSeconds(attackWindup);

        if (!IsDead &&
            target != null &&
            !target.IsDead &&
            Vector2.Distance(transform.position, target.transform.position) <= attackRange)
        {
            yield return ShowAttackAndDamage(target);
        }

        attackRoutine = null;
    }

    private IEnumerator ShowAttackAndDamage(PlayerStats target)
    {
        if (attackVisualPrefab == null)
        {
            Debug.LogWarning(
                "[TargetDummy] Assign an attack visual prefab before the dummy can damage the player.",
                this
            );
            yield break;
        }

        Vector2 direction = (Vector2)target.transform.position - (Vector2)transform.position;
        if (direction.sqrMagnitude <= 0.0001f)
            direction = Vector2.right;
        else
            direction.Normalize();

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Vector3 visualPosition = transform.position + (Vector3)(direction * (attackRange * 0.5f));
        SpriteArrayVisual visual = Instantiate(
            attackVisualPrefab,
            visualPosition,
            Quaternion.Euler(0f, 0f, angle)
        );

        SpriteRenderer spriteRenderer = visual.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            Debug.LogWarning(
                "[TargetDummy] The attack visual prefab needs an active SpriteRenderer with a sprite.",
                visual
            );
            Destroy(visual.gameObject);
            yield break;
        }

        PolygonCollider2D hitbox = spriteRenderer.GetComponent<PolygonCollider2D>();
        if (hitbox == null)
            hitbox = spriteRenderer.gameObject.AddComponent<PolygonCollider2D>();

        hitbox.isTrigger = true;
        SetHitboxToSpriteShape(hitbox, spriteRenderer.sprite);

        var overlaps = new List<Collider2D>();
        ContactFilter2D filter = new ContactFilter2D();
        filter.NoFilter();
        filter.useTriggers = true;

        float hitWindowEnd = Time.time + attackHitWindow;
        bool hasHitPlayer = false;
        while (!hasHitPlayer && visual != null && Time.time <= hitWindowEnd)
        {
            Sprite currentSprite = spriteRenderer.sprite;
            if (currentSprite != null)
            {
                SetHitboxToSpriteShape(hitbox, currentSprite);
                Physics2D.SyncTransforms();
                overlaps.Clear();
                hitbox.Overlap(filter, overlaps);

                foreach (Collider2D overlap in overlaps)
                {
                    PlayerStats hitPlayer = overlap.GetComponentInParent<PlayerStats>();
                    if (hitPlayer == null || hitPlayer != target || hitPlayer.IsDead)
                        continue;

                    hitPlayer.TakeDamage(
                        attackDamage,
                        attackDamageType,
                        transform.position,
                        GetComponent<EnemyHealth>()
                    );
                    hasHitPlayer = true;
                    break;
                }
            }

            if (!hasHitPlayer && Time.time < hitWindowEnd)
                yield return null;
        }
    }

    private static void SetHitboxToSpriteShape(PolygonCollider2D hitbox, Sprite sprite)
    {
        hitbox.pathCount = 0;
        int shapeCount = sprite.GetPhysicsShapeCount();
        var points = new List<Vector2>();

        for (int shapeIndex = 0; shapeIndex < shapeCount; shapeIndex++)
        {
            points.Clear();
            sprite.GetPhysicsShape(shapeIndex, points);
            if (points.Count < 3)
                continue;

            int pathIndex = hitbox.pathCount;
            hitbox.pathCount = pathIndex + 1;
            hitbox.SetPath(pathIndex, points);
        }

        if (hitbox.pathCount > 0)
            return;

        Bounds bounds = sprite.bounds;
        points.Add(new Vector2(bounds.min.x, bounds.min.y));
        points.Add(new Vector2(bounds.min.x, bounds.max.y));
        points.Add(new Vector2(bounds.max.x, bounds.max.y));
        points.Add(new Vector2(bounds.max.x, bounds.min.y));
        hitbox.SetPath(0, points);
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
