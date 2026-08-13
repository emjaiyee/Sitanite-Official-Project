using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class RampElevationTrigger : MonoBehaviour
{
    [SerializeField] private int targetLevel;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerElevationLevel elevation =
            other.GetComponent<PlayerElevationLevel>();

        if (elevation == null)
            return;

        elevation.SetLevel(targetLevel);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(
            transform.position,
            GetComponent<Collider2D>().bounds.size
        );
    }
#endif
}