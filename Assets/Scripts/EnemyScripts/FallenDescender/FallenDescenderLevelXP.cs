using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyHealth))]
public class FallenDescenderLevelXP : MonoBehaviour
{
    [Header("Previous Run Level")]
    [Min(1)] [SerializeField] private int level = 1;

    [Header("Experience Reward")]
    [Min(0f)] [SerializeField] private float experienceReward = 10f;
    [SerializeField] private bool grantExperienceOnDeath = true;

    private EnemyHealth enemyHealth;
    private bool subscribed;
    private bool rewardGranted;

    public int Level => level;
    public float ExperienceReward => experienceReward + Mathf.Max(0, level - 1) * 20f;

    private void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
    }

    private void OnEnable()
    {
        if (enemyHealth == null)
            enemyHealth = GetComponent<EnemyHealth>();

        if (!subscribed && enemyHealth != null)
        {
            enemyHealth.OnEnemyDied += HandleEnemyDied;
            subscribed = true;
        }
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
    }

    public void SetLevel(int previousRunLevel)
    {
        level = Mathf.Max(1, previousRunLevel);
    }

    public void SetExperienceReward(float reward)
    {
        experienceReward = Mathf.Max(0f, reward);
    }

    public void AddExperienceReward(float modifier)
    {
        experienceReward = Mathf.Max(0f, experienceReward + modifier);
    }

    private void HandleEnemyDied(GameObject deadEnemy)
    {
        if (!grantExperienceOnDeath || rewardGranted || Player.Instance == null)
            return;

        PlayerStats playerStats = Player.Instance.GetComponent<PlayerStats>();
        if (playerStats == null || ExperienceReward <= 0f)
            return;

        rewardGranted = true;
        playerStats.AddExperience(ExperienceReward);
    }
}