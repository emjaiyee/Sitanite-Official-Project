using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyHealth))]
public class EnemyLevelXP : MonoBehaviour
{
    [Header("Level")]
    [Min(1)]
    [SerializeField] private int level = 1;

    [Header("Experience Reward")]
    [Min(0f)]
    [SerializeField] private float experienceReward = 10f;

    [SerializeField] private bool grantExperienceOnDeath = true;

    private EnemyHealth enemyHealth;
    private EnemyMelee enemyMelee;
    private EnemyRange enemyRange;
    private bool subscribed;

    public int Level => level;
    public float ExperienceReward => GetScaledExperienceReward();

    private void Awake()
    {
        ResolveReferences();
        ApplyLevelScaling();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (!subscribed && enemyHealth != null)
        {
            enemyHealth.OnEnemyDied += HandleEnemyDied;
            subscribed = true;
        }

        ApplyLevelScaling();
    }

    private void Start()
    {
        ApplyLevelScaling();
    }

    private void OnDisable()
    {
        if (!subscribed || enemyHealth == null)
            return;

        enemyHealth.OnEnemyDied -= HandleEnemyDied;
        subscribed = false;
    }

    private void OnValidate()
    {
        level = Mathf.Max(1, level);
        experienceReward = Mathf.Max(0f, experienceReward);

        ResolveReferences();

        if (Application.isPlaying)
            ApplyLevelScaling();
    }

    public void SetLevel(int newLevel)
    {
        level = Mathf.Max(1, newLevel);
        ApplyLevelScaling();
    }

    public void SetExperienceReward(float reward)
    {
        experienceReward = Mathf.Max(0f, reward);
    }

    public void AddExperienceReward(float modifier)
    {
        experienceReward = Mathf.Max(0f, experienceReward + modifier);
    }

    private void ResolveReferences()
    {
        if (enemyHealth == null)
            enemyHealth = GetComponent<EnemyHealth>();

        if (enemyMelee == null)
            enemyMelee = GetComponent<EnemyMelee>();

        if (enemyRange == null)
            enemyRange = GetComponent<EnemyRange>();
    }

    private void ApplyLevelScaling()
    {
        ResolveReferences();

        if (enemyHealth != null)
            enemyHealth.ApplyLevelScaling(level);

        if (enemyMelee != null)
            enemyMelee.ApplyLevelScaling(level);

        if (enemyRange != null)
            enemyRange.ApplyLevelScaling(level);
    }

    private void HandleEnemyDied(GameObject deadEnemy)
    {
        if (!grantExperienceOnDeath)
            return;

        if (Player.Instance == null)
            return;

        PlayerStats playerStats = Player.Instance.GetComponent<PlayerStats>();
        if (playerStats == null)
            return;

        float reward = GetScaledExperienceReward();
        if (reward <= 0f)
            return;

        playerStats.AddExperience(reward);
    }

    private float GetScaledExperienceReward()
    {
        if (experienceReward <= 0f)
            return 0f;

        int effectiveLevel = Mathf.Max(1, level);
        return experienceReward * Mathf.Pow(2f, effectiveLevel - 1);
    }
}