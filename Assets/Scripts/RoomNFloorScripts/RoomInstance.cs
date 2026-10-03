using UnityEngine;

public class RoomInstance : MonoBehaviour
{
    [Header("Breakable Pots")]
    [Min(0)] [SerializeField] private int minimumPots = 0;
    [Min(0)] [SerializeField] private int maximumPots = 10;

    private int roomNumber;

    public int RoomNumber => roomNumber;

    public bool ContainsWorldPosition(Vector3 worldPosition)
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);

        foreach (Collider2D collider in colliders)
        {
            if (collider != null && collider.OverlapPoint(worldPosition))
                return true;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer roomRenderer in renderers)
        {
            if (roomRenderer != null &&
                roomRenderer.bounds.Contains(worldPosition))
                return true;
        }

        return false;
    }

    public void Initialize(int number)
    {
        roomNumber = number;
        RandomizeBreakablePots();
    }

    private void RandomizeBreakablePots()
    {
        BreakablePot[] pots = GetComponentsInChildren<BreakablePot>(true);
        int minimum = Mathf.Min(minimumPots, maximumPots);
        int maximum = Mathf.Max(minimumPots, maximumPots);
        int potCountToKeep = Random.Range(
            Mathf.Min(minimum, pots.Length),
            Mathf.Min(maximum, pots.Length) + 1
        );

        for (int index = pots.Length - 1; index > 0; index--)
        {
            int randomIndex = Random.Range(0, index + 1);
            BreakablePot temporary = pots[index];
            pots[index] = pots[randomIndex];
            pots[randomIndex] = temporary;
        }

        for (int index = potCountToKeep; index < pots.Length; index++)
            Destroy(pots[index].gameObject);
    }
}