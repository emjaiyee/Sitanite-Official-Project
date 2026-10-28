using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TutorialSlimeStepTrigger : MonoBehaviour
{
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private int fightStepIndex = 4;

    private TutorialManager tutorialManager;

    private void Awake()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;

        if (enemyHealth == null)
            enemyHealth = GetComponentInParent<EnemyHealth>();
    }

    private void OnEnable()
    {
        if (enemyHealth != null)
            enemyHealth.OnEnemyDied += HandleEnemyDied;
    }

    private void OnDisable()
    {
        if (enemyHealth != null)
            enemyHealth.OnEnemyDied -= HandleEnemyDied;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerStats>() == null)
            return;

        TutorialManager manager = GetTutorialManager();
        if (manager != null && manager.CurrentStepIndex == fightStepIndex)
            manager.HideCurrentPrompt();
    }

    private void HandleEnemyDied(GameObject deadEnemy)
    {
        TutorialManager manager = GetTutorialManager();
        if (
            manager != null &&
            manager.IsCurrentStepActive &&
            manager.CurrentStepIndex == fightStepIndex
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
