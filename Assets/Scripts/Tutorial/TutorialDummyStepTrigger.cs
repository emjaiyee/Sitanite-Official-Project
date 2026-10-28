using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(EnemyHealth))]
public class TutorialDummyStepTrigger : MonoBehaviour
{
    [SerializeField] private int activationStepIndex = 2;
    [SerializeField] private int attackStepIndex = 2;
    [SerializeField] private int skillStepIndex = 3;

    private TutorialManager tutorialManager;
    private EnemyHealth enemyHealth;

    private void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();

        CircleCollider2D trigger = GetComponent<CircleCollider2D>();
        trigger.isTrigger = true;
    }

    private void OnEnable()
    {
        if (enemyHealth == null)
            enemyHealth = GetComponent<EnemyHealth>();

        enemyHealth.OnDamaged += HandleDummyDamaged;
    }

    private void OnDisable()
    {
        if (enemyHealth != null)
            enemyHealth.OnDamaged -= HandleDummyDamaged;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerStats>() == null)
            return;

        TutorialManager manager = GetTutorialManager();
        if (manager != null)
            manager.ActivateCurrentStep(activationStepIndex);
    }

    private void HandleDummyDamaged(
        EnemyHealth health,
        Vector3? damageSource)
    {
        if (!health.IsReceivingPlayerDamage)
            return;

        TutorialManager manager = GetTutorialManager();
        if (manager == null || !manager.IsCurrentStepActive)
            return;

        if (manager.CurrentStepIndex == attackStepIndex)
        {
            manager.CompleteCurrentStep();
        }
        else if (
            manager.CurrentStepIndex == skillStepIndex &&
            health.IsReceivingPlayerSkillDamage
        )
        {
            manager.CompleteCurrentStep();
        }
    }

    private TutorialManager GetTutorialManager()
    {
        if (tutorialManager == null)
            tutorialManager = FindFirstObjectByType<TutorialManager>();

        return tutorialManager;
    }
}
