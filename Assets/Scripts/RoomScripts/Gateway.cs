using UnityEngine;

public class Gateway : MonoBehaviour
{
    [SerializeField] private Transform destination;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        RoomTransitionManager.Instance.TransitionPlayer(
            other.transform,
            destination
        );
    }
}