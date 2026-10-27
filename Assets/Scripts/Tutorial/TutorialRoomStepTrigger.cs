using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TutorialRoomStepTrigger : MonoBehaviour
{
    [SerializeField] private int stepIndex;

    private TutorialManager tutorialManager;

    private void Awake()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerStats>() == null)
            return;

        if (tutorialManager == null)
            tutorialManager = FindFirstObjectByType<TutorialManager>();

        if (tutorialManager != null)
            tutorialManager.ActivateCurrentStep(stepIndex);
    }
}
