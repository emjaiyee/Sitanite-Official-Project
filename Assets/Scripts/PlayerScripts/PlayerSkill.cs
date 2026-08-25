using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSkill : MonoBehaviour
{
    [Header("Skill Settings")]
    [SerializeField] private float skillDuration = 0.5f;
    [SerializeField] private float skillRadius = 2f;
    [SerializeField] private int skillDamage = 50;

    [Header("Stamina")]
    [SerializeField] private int skillCost = 25;

    [Header("Input")]
    [SerializeField] private InputActionReference skillAction;

    [Header("Layers")]
    [SerializeField] private LayerMask hittableLayers;

    [Header("Visual Feedback")]
    [SerializeField] private GameObject swordVisualPrefab;    // red circle for Long Sword
    [SerializeField] private GameObject battleAxeVisualPrefab; // unique prefab for Battle Axe

    private PlayerStats stats;
    private PlayerEquipment equipment;
    private bool isUsingSkill;
    private float skillTime;
    private GameObject activeVisual;

    public bool IsUsingSkill => isUsingSkill;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
        equipment = GetComponent<PlayerEquipment>();

        if (stats == null)
            Debug.LogError("PlayerSkill requires a PlayerStats component.");
        if (equipment == null)
            Debug.LogError("PlayerSkill requires a PlayerEquipment component.");
    }

    private void OnEnable()
    {
        if (skillAction == null)
        {
            Debug.LogWarning("PlayerSkill has no Skill InputActionReference assigned.");
            return;
        }

        skillAction.action.Enable();
        skillAction.action.performed += OnSkillPerformed;
    }

    private void OnDisable()
    {
        if (skillAction == null) return;

        skillAction.action.performed -= OnSkillPerformed;
        skillAction.action.Disable();
    }

    private void Update()
    {
        if (isUsingSkill && Time.time >= skillTime + skillDuration)
            EndSkill();
    }

    private void OnSkillPerformed(InputAction.CallbackContext context)
    {
        StartSkill();
    }

    private void StartSkill()
    {
        if (isUsingSkill) return;

        if (stats != null && !stats.UseStamina(skillCost))
        {
            Debug.Log("[PlayerSkill] Not enough stamina to use skill.");
            return;
        }

        isUsingSkill = true;
        skillTime = Time.time;

        if (equipment.CurrentWeapon.WeaponId == "LongSword")
            PerformLongSwordSkill();
        else if (equipment.CurrentWeapon.WeaponId == "BattleAxe")
            PerformBattleAxeSkill();
    }

    private void PerformLongSwordSkill()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, skillRadius, hittableLayers);
        foreach (Collider2D hit in hits)
        {
            IDamageable target = hit.GetComponent<IDamageable>();
            if (target != null)
            {
                target.TakeDamage(skillDamage, DamageType.Physical);
                Debug.Log($"[PlayerSkill] Long Sword skill hit {hit.name} for {skillDamage} damage.");
            }
        }

        if (swordVisualPrefab != null)
        {
            activeVisual = Instantiate(swordVisualPrefab, transform.position, Quaternion.identity);
            activeVisual.transform.localScale = new Vector3(skillRadius * 2, skillRadius * 2, 1);
        }

        Debug.Log("[PlayerSkill] Long Sword skill triggered!");
    }

    private void PerformBattleAxeSkill()
    {
        float slamRadius = skillRadius * 0.75f;
        int slamDamage = skillDamage + 30;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, slamRadius, hittableLayers);
        foreach (Collider2D hit in hits)
        {
            IDamageable target = hit.GetComponent<IDamageable>();
            if (target != null)
            {
                target.TakeDamage(slamDamage, DamageType.Physical);
                Debug.Log($"[PlayerSkill] Battle Axe slam hit {hit.name} for {slamDamage} damage.");
            }
        }

        if (battleAxeVisualPrefab != null)
        {
            activeVisual = Instantiate(battleAxeVisualPrefab, transform.position, Quaternion.identity);
            activeVisual.transform.localScale = new Vector3(slamRadius * 2, slamRadius * 2, 1);
        }

        Debug.Log("[PlayerSkill] Battle Axe skill triggered!");
    }

    private void EndSkill()
    {
        isUsingSkill = false;

        if (activeVisual != null)
        {
            Destroy(activeVisual);
            activeVisual = null;
        }

        Debug.Log("[PlayerSkill] Skill ended.");
    }
}