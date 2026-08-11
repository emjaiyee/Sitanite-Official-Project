using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PoisonArea : MonoBehaviour
{
    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && GameManager.Instance != null)
            GameManager.Instance.StartPoison();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && GameManager.Instance != null)
            GameManager.Instance.StopPoison();
    }
}