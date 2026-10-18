using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyHealth), typeof(Rigidbody2D), typeof(EnemyElevationLevel))]
[RequireComponent(typeof(FallenDescenderLevelXP))]
public class FallenDescender : MonoBehaviour
{
    public enum BrainState { Idle, Chase, Attack, Recover, Dead, Skill }

    [Header("References")]
    [SerializeField] private FallenDescenderRenderer characterRenderer;
    [SerializeField] private Transform firePoint;

    [Header("Brain")]
    [Min(0f)] [SerializeField] private float detectionRange = 12f;
    [Min(0.05f)] [SerializeField] private float repathInterval = 0.2f;
    [Min(0f)] [SerializeField] private float attackWindup = 0.3f;
    [Min(0f)] [SerializeField] private float recoveryDuration = 0.25f;
    [Min(0f)] [SerializeField] private float deathFadeDuration = 0.75f;

    [Header("Previous Run Base Stats")]
    [SerializeField] private float baseHealth;
    [SerializeField] private float sprintSpeed;
    [SerializeField] private int previousLevel;
    [SerializeField] private List<DungeonMemory.DamageStat> baseStats = new List<DungeonMemory.DamageStat>();

    [Header("Balanced Stats (Runtime)")]
    [SerializeField] private int level;
    [SerializeField] private BrainState state;
    [SerializeField] private List<DungeonMemory.DamageStat> balancedStats = new List<DungeonMemory.DamageStat>();

    private EnemyHealth health;
    private Rigidbody2D body;
    private EnemyElevationLevel elevation;
    private DungeonMemory memory;
    private DungeonMemory.SavedDeath savedDeath;
    private RoomInstance room;
    private RoomManager roomManager;
    private PlayerStats target;
    private ItemData weapon;
    private WeaponController weaponSkills;
    private readonly List<DungeonMemory.DamageStat> attackHits = new List<DungeonMemory.DamageStat>();
    private List<Vector3> path;
    private int waypoint;
    private float nextRepath;
    private float stateUntil;
    private float nextAttack;
    private Vector2 attackDirection;
    private Vector2 previousAnimationPosition;
    private bool configured;

    public BrainState State => state;
    public ItemData Weapon => weapon;
    public int Level => level;
    public string ConfigurationError { get; private set; }
    public event Action<GameObject> CorpseCreated;

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
        body = GetComponent<Rigidbody2D>();
        elevation = GetComponent<EnemyElevationLevel>();
        if (characterRenderer == null)
            characterRenderer = GetComponentInChildren<FallenDescenderRenderer>(true);
        body.gravityScale = 0f;
        body.constraints |= RigidbodyConstraints2D.FreezeRotation;
    }

    private void OnEnable()
    {
        if (health != null)
            health.OnEnemyDied += HandleDied;
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnEnemyDied -= HandleDied;
        if (body != null)
            body.linearVelocity = Vector2.zero;
    }

    public bool Configure(DungeonMemory.SavedDeath death, DungeonMemory source, RoomInstance encounterRoom, PlayerStats player, BossBalance.SpawnModifiers modifiers)
    {
        ConfigurationError = null;
        if (death?.combat == null || death.combat.equipment == null || death.combat.stats == null)
            return RejectConfiguration("The saved character has no complete combat snapshot.");
        if (source == null)
            return RejectConfiguration("DungeonMemory is missing.");
        if (player == null)
            return RejectConfiguration("The current PlayerStats is missing.");
        if (characterRenderer == null)
            return RejectConfiguration("Add FallenDescenderRenderer to the prefab or assign Character Renderer.");
        if (!characterRenderer.CanRender(death.gender))
            return RejectConfiguration($"Assign Body Renderer, Eyes Renderer and the {death.gender} rotting body/eyes on FallenDescenderRenderer.");

        DungeonMemory.SavedEquipment savedWeapon = death.combat.equipment.Find(item => item != null && item.slot == EquipmentType.Weapon);
        weapon = savedWeapon != null ? source.ResolveItem(savedWeapon.itemId) : null;
        if (savedWeapon == null || weapon == null)
            return RejectConfiguration("The saved weapon is missing or is not in DungeonMemory's Item Catalog.");
        if (weapon.EquipmentType != EquipmentType.Weapon || weapon.AttackRange <= 0f)
            return RejectConfiguration($"Saved weapon '{weapon.name}' must be a Weapon with Attack Range greater than zero.");

        if (weapon.WeaponAttackType != WeaponAttackType.Melee &&
            (weapon.ProjectilePrefab == null ||
             (weapon.ProjectilePrefab.GetComponent<ProjectileBase>() == null && weapon.ProjectilePrefab.GetComponent<BaseArrow>() == null)))
            return RejectConfiguration($"Saved weapon '{weapon.name}' needs a projectile prefab with ProjectileBase or BaseArrow on its root.");

        savedDeath = death;
        memory = source;
        room = encounterRoom;
        roomManager = FindFirstObjectByType<RoomManager>();
        target = player;
        baseHealth = death.combat.health;
        sprintSpeed = death.combat.sprintSpeed;
        previousLevel = death.combat.level;
        level = Mathf.Max(1, previousLevel + modifiers.level);
        baseStats = new List<DungeonMemory.DamageStat>(death.combat.stats);
        balancedStats.Clear();
        attackHits.Clear();
        foreach (DungeonMemory.DamageStat stat in baseStats)
        {
            float damage = BossBalance.AdjustBase(stat.damage, previousLevel, player.Level);
            damage = BossBalance.ApplyHalfGear(damage, death.combat.equipment, StatType.Damage, stat.type);
            float resistance = BossBalance.AdjustBase(stat.resistance, previousLevel, player.Level);
            resistance = BossBalance.ApplyHalfGear(resistance, death.combat.equipment, StatType.BaseDamageResistance, stat.type);
            resistance = BossBalance.ApplyHalfGear(resistance, death.combat.equipment, StatType.DamageResistance, stat.type);
            balancedStats.Add(new DungeonMemory.DamageStat
            {
                type = stat.type,
                damage = Mathf.Max(0f, damage + modifiers.damage),
                resistance = Mathf.Max(0f, resistance + modifiers.resistance)
            });
        }

        HashSet<DamageType> weaponTypes = new HashSet<DamageType>();
        foreach (WeaponDamage damage in savedWeapon.weaponDamages ?? new List<WeaponDamage>())
        {
            if (damage.damageType == DamageType.None || !weaponTypes.Add(damage.damageType))
                continue;

            DungeonMemory.DamageStat stat = balancedStats.Find(entry => entry.type == damage.damageType);
            if (stat != null)
                attackHits.Add(new DungeonMemory.DamageStat { type = stat.type, damage = stat.damage });
        }
        if (attackHits.Count == 0)
            return RejectConfiguration($"Saved weapon '{weapon.name}' has no damage types matching the character's saved stats.");

        float maxHealth = BossBalance.Health(baseHealth, player.MaxHealth);
        maxHealth = BossBalance.ApplyHalfGear(maxHealth, death.combat.equipment, StatType.Health);
        string characterName = string.IsNullOrWhiteSpace(death.characterName) ? "Unknown" : death.characterName;
        health.SetEnemyName($"Fallen Descender {characterName}");
        health.ConfigureBossStats(Mathf.Max(1f, maxHealth + modifiers.health), balancedStats);

        FallenDescenderLevelXP experience = GetComponent<FallenDescenderLevelXP>();
        if (experience == null)
            experience = gameObject.AddComponent<FallenDescenderLevelXP>();
        experience.SetLevel(level);
        experience.AddExperienceReward(modifiers.experience);
        weaponSkills = GetComponent<WeaponController>();
        if (weaponSkills == null)
            weaponSkills = gameObject.AddComponent<WeaponController>();
        weaponSkills.ConfigureForEnemy(weapon, balancedStats, elevation.CurrentLevel, transform, firePoint);
        characterRenderer.Configure(death, source);
        configured = true;
        previousAnimationPosition = body.position;
        characterRenderer.SetState(CharacterAnimationState.Idle);
        ChangeState(BrainState.Idle);
        return true;
    }

    private bool RejectConfiguration(string reason)
    {
        ConfigurationError = reason;
        return false;
    }

    private void Update()
    {
        if (!configured || state == BrainState.Dead)
            return;

        if (target == null || target.IsDead || !IsPlayerInRoom())
        {
            weaponSkills.CancelEnemyActions();
            ChangeState(BrainState.Idle);
            return;
        }

        switch (state)
        {
            case BrainState.Idle:
                if (Vector2.Distance(transform.position, target.transform.position) <= detectionRange)
                    ChangeState(BrainState.Chase);
                break;
            case BrainState.Chase:
                Vector2 direction = target.transform.position - transform.position;
                characterRenderer.Face(direction);
                bool skillReady = weapon.WeaponSkillType != WeaponSkillType.None && weaponSkills.CanUseSkill &&
                    direction.magnitude <= SkillReach;
                if ((direction.magnitude <= weapon.AttackRange || skillReady) && PlayerElevationLevel.CanAffectTarget(transform))
                {
                    body.linearVelocity = Vector2.zero;
                    if (skillReady || Time.time >= nextAttack)
                    {
                        attackDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
                        ChangeState(skillReady ? BrainState.Skill : BrainState.Attack);
                    }
                }
                else if (Time.time >= nextRepath && (AStarManager.Instance == null ||
                    AStarManager.Instance.GetStairLinkAtPosition(transform.position) == null))
                {
                    nextRepath = Time.time + repathInterval;
                    path = AStarManager.Instance != null ? AStarManager.Instance.FindPath(
                        transform.position, target.transform.position, elevation.CurrentLevel,
                        PlayerElevationLevel.Instance != null ? PlayerElevationLevel.Instance.CurrentLevel : elevation.CurrentLevel
                    ) : null;
                    waypoint = 0;
                }
                break;
            case BrainState.Attack:
                if (Time.time >= stateUntil)
                {
                    ExecuteAttack();
                    nextAttack = Time.time + weaponSkills.AttackCooldownSeconds;
                    ChangeState(BrainState.Recover);
                }
                break;
            case BrainState.Skill:
                if (Time.time >= stateUntil)
                {
                    weaponSkills.UseSkillForEnemy(attackDirection, target.transform.position, elevation.CurrentLevel);
                    ChangeState(BrainState.Recover);
                }
                break;
            case BrainState.Recover:
                if (Time.time >= stateUntil && !weaponSkills.IsPerformingEnemySkill)
                    ChangeState(BrainState.Chase);
                break;
        }
    }

    private float SkillReach => weapon.WeaponSkillType switch
    {
        WeaponSkillType.AreaDamage or WeaponSkillType.Slash or WeaponSkillType.SpinAxe or WeaponSkillType.VeilOfFire
            => Mathf.Max(0.1f, weapon.SkillRadius * weapon.SkillRadiusMultiplier),
        WeaponSkillType.FireBalls or WeaponSkillType.RapidFireBalls => weapon.AttackRange,
        _ => Mathf.Max(0.1f, weapon.SkillRange)
    };

    private bool IsPlayerInRoom()
    {
        return roomManager == null || roomManager.CurrentPlayerRoom == null || roomManager.CurrentPlayerRoom == room;
    }

    private void FixedUpdate()
    {
        if (!configured)
            return;

        UpdateMovementAnimation();
        if (state != BrainState.Chase || target == null || !IsPlayerInRoom() ||
            (Vector2.Distance(transform.position, target.transform.position) <= weapon.AttackRange && PlayerElevationLevel.CanAffectTarget(transform)))
            return;

        while (path != null && waypoint < path.Count && Vector2.Distance(body.position, path[waypoint]) <= 0.1f)
            waypoint++;

        if (path == null || waypoint >= path.Count)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }
        Vector2 direction = ((Vector2)path[waypoint] - body.position).normalized;
        characterRenderer.Face(direction);
        body.linearVelocity = Vector2.zero;
        body.MovePosition(Vector2.MoveTowards(body.position, path[waypoint], sprintSpeed * Time.fixedDeltaTime));

        if (AStarManager.Instance != null && AStarManager.Instance.GetStairLinkAtPosition(transform.position) == null)
        {
            UnityEngine.Tilemaps.Tilemap map = AStarManager.Instance.GetWalkableTilemapAtPosition(transform.position);
            if (map != null && map.TryGetComponent(out AStarWalkableMap walkable))
                elevation.SetLevel(walkable.ElevationLevel);
        }
    }

    private void UpdateMovementAnimation()
    {
        bool moving = (body.position - previousAnimationPosition).sqrMagnitude > 0.000001f;
        previousAnimationPosition = body.position;
        if (state == BrainState.Dead || state == BrainState.Attack || state == BrainState.Skill ||
            (weaponSkills != null && weaponSkills.IsPerformingEnemySkill))
            return;

        characterRenderer.SetState(moving ? CharacterAnimationState.Running : CharacterAnimationState.Idle);
    }

    private void ChangeState(BrainState next)
    {
        state = next;
        if (next != BrainState.Chase)
            body.linearVelocity = Vector2.zero;
        if (next == BrainState.Chase)
        {
            nextRepath = 0f;
        }
        else if (next == BrainState.Attack)
        {
            stateUntil = Time.time + attackWindup;
            characterRenderer.SetState(weapon.WeaponAttackType == WeaponAttackType.Melee ? CharacterAnimationState.Melee : CharacterAnimationState.Cast);
        }
        else if (next == BrainState.Skill)
        {
            stateUntil = Time.time + attackWindup;
            bool meleeSkill = weapon.WeaponSkillType == WeaponSkillType.Stab || weapon.WeaponSkillType == WeaponSkillType.Slash ||
                weapon.WeaponSkillType == WeaponSkillType.SpinAxe || weapon.WeaponSkillType == WeaponSkillType.AreaDamage;
            characterRenderer.SetState(meleeSkill ? CharacterAnimationState.Melee : CharacterAnimationState.Cast);
        }
        else
        {
            stateUntil = Time.time + recoveryDuration;
            if (next == BrainState.Dead)
                characterRenderer.SetState(CharacterAnimationState.Idle);
        }
    }

    private void ExecuteAttack()
    {
        if (target == null || target.IsDead || !PlayerElevationLevel.CanAffectTarget(transform))
            return;

        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        if (weapon.AttackVisualPrefab != null)
        {
            GameObject visual = Instantiate(weapon.AttackVisualPrefab, origin + (Vector3)(attackDirection * weapon.AttackRange),
                Quaternion.Euler(0f, 0f, Mathf.Atan2(attackDirection.y, attackDirection.x) * Mathf.Rad2Deg));
            Destroy(visual, 0.5f);
        }

        if (weapon.WeaponAttackType == WeaponAttackType.Melee)
        {
            Vector2 center = (Vector2)transform.position + attackDirection * weapon.AttackRange;
            foreach (Collider2D hit in Physics2D.OverlapCircleAll(center, weapon.AttackRange))
            {
                if (hit.GetComponentInParent<PlayerStats>() != target)
                    continue;

                BossBalance.HitPlayer(target, attackHits, elevation.CurrentLevel);
                break;
            }
        }
        else
        {
            GameObject projectile = Instantiate(weapon.ProjectilePrefab, origin,
                Quaternion.Euler(0f, 0f, Mathf.Atan2(attackDirection.y, attackDirection.x) * Mathf.Rad2Deg));
            float speed = weapon.WeaponAttackType == WeaponAttackType.Spell ? weapon.SpellProjectileSpeed : weapon.ProjectileSpeed;
            if (projectile.TryGetComponent(out ProjectileBase playerProjectile))
                playerProjectile.InitializeForEnemy(attackHits, Mathf.Max(0.01f, speed), weapon.AttackRange, weapon.Homing, elevation.CurrentLevel);
            else if (projectile.TryGetComponent(out BaseArrow enemyProjectile))
                enemyProjectile.LaunchForEnemy(attackDirection, attackHits, speed, weapon.AttackRange, weapon.Homing, elevation.CurrentLevel);
        }
    }

    private void HandleDied(GameObject deadEnemy)
    {
        if (!configured || state == BrainState.Dead)
            return;

        ChangeState(BrainState.Dead);
        if (weaponSkills != null)
        {
            weaponSkills.CancelEnemyActions();
            weaponSkills.enabled = false;
        }
        foreach (Collider2D collider in GetComponentsInChildren<Collider2D>())
            collider.enabled = false;
        StartCoroutine(FadeAndLeaveBody());
    }

    private IEnumerator FadeAndLeaveBody()
    {
        float elapsed = 0f;
        while (elapsed < deathFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            characterRenderer.SetOpacity(1f - Mathf.Clamp01(elapsed / deathFadeDuration));
            yield return null;
        }
        characterRenderer.SetOpacity(0f);
        memory.CompleteFallenEncounter(savedDeath, room, transform.position);
        CorpseCreated?.Invoke(gameObject);
        Destroy(gameObject);
    }
}