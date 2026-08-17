using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class RampElevationTrigger : MonoBehaviour
{
    [Header("Elevation")]
    [SerializeField] private int targetLevel;


    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }


    private void OnTriggerEnter2D(
        Collider2D other)
    {
        // =====================================================
        // PLAYER
        // =====================================================

        PlayerElevationLevel playerElevation =
            other.GetComponent<PlayerElevationLevel>();


        if (playerElevation != null)
        {
            playerElevation.SetLevel(
                targetLevel
            );

            return;
        }


        // =====================================================
        // ENEMY
        // =====================================================

        EnemyElevationLevel enemyElevation =
            other.GetComponent<EnemyElevationLevel>();


        if (enemyElevation != null)
        {
            enemyElevation.SetLevel(
                targetLevel
            );
        }
    }


#if UNITY_EDITOR

    private void OnDrawGizmos()
    {
        Gizmos.color =
            Color.yellow;


        Collider2D collider =
            GetComponent<Collider2D>();


        if (collider != null)
        {
            Gizmos.DrawWireCube(
                collider.bounds.center,
                collider.bounds.size
            );
        }
    }

#endif
}