using UnityEngine;


public enum AttackEnchantment
{
    none,           // No Enchantment (Normal Damage)
    fire,           // Fire Damage over time (Burn Big Damage but does not remain - 3-5 seconds long) 
    ice,            // Ice Damage slows down player movement speed and attack speed
    electric,       // Electric Damage stacks with the the third hit paralyzing players 
    poison,         // Poison Damage over time (Unlike fire poison last longer and needs antidote to cure)
    miasma          // Dark Magic (Make player weak Ex. Low Damage, Takes More Damage, etc.)
}

public enum AttackTpye
{
    Slash,
    Blunt,
    Pierce
}



[CreateAssetMenu(fileName = "EnemyStats", menuName = "EnemyStats/Data")]
public class EnemyStats_Data : ScriptableObject
{
    [TextArea(1, 1)]
    public string Name;

    [TextArea(3, 8)]
    public string Info;

    public AttackEnchantment Enchantment;

    public AttackTpye Tpye;



    //========================================================================//
    // Detection Variables
    //========================================================================//


    [Tooltip("Detection distance measured in A* grid cells.")]
 
    [Range(0, 20)] 
    [SerializeField] private int detectionRadius;

    [Tooltip("Attack Detection distance measured in A* grid cells.")]
  
    [Range(0, 20)] 
    [SerializeField] private int detectionAttackRadius;


    public int DetectionRadius => detectionRadius;
    public int DetectionAttackRadius => detectionAttackRadius;


    //========================================================================//
    // Enemy Variables
    //========================================================================//

    [Header("Enemy Stats")]
    [Min(0)]
    [SerializeField] private int enemyMaxHealth = 100;

    [Min(0)]
    [SerializeField] private int enemyMaxAttack = 10;

    [Range(0f, 5f)]
    [SerializeField] private float enemyMaxSpeed = 2f;

    public int EnemyMaxHealth => enemyMaxHealth;
    public int EnemyMaxAttack => enemyMaxAttack;
    public float EnemyMaxSpeed => enemyMaxSpeed;


    //========================================================================//
    // Projectile Prefab
    //========================================================================//

    [Header("Projectile Prefab")]
    [Tooltip("This part is only use for Range and Mage it also include there own damage")]
    [SerializeField] private GameObject enemyProjectilePrefab;
    public GameObject EnemyProjectilePrefab => enemyProjectilePrefab;


    //========================================================================//
    // Enhantment Variables
    //========================================================================//

    [SerializeField] private float effectDuration;
    [SerializeField] private float effectVariable;

    public float EffectDuration => effectDuration;
    public float EffectVariable => effectVariable;


    #region Enemy Elemental Properties (WIP Phase) Not working as intended yet dont touch
    public void SetElementProperties(AttackEnchantment Enchantment)
    {
        switch (Enchantment)
        {

            case AttackEnchantment.fire:
                effectDuration = 3f;
                effectVariable = 10f;
                break;

            case AttackEnchantment.ice:
                effectDuration = 20f;
                effectVariable = -12f;
                break;

            case AttackEnchantment.electric:
                effectDuration = 5f;
                effectVariable = 0f;
                break;

            case AttackEnchantment.poison:
                effectDuration = 120f;
                effectVariable = 3f;
                break;

            case AttackEnchantment.miasma:
                effectDuration = 30f;
                effectVariable = 5f;
                break;

            default:
                effectDuration = 0f;
                effectVariable = 0f;
                break;
        }

    }
    #endregion



}

