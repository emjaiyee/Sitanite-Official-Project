using UnityEngine;

public class EnemyRangeSearchState : EnemyRangeState
{
    #region Variables Part
    private float searchTimer;

    private const float SearchDuration = 3.5f;

    public EnemyRangeSearchState(
       EnemyRange enemy)
       : base(enemy)
    {
    }
    #endregion


    public override void Enter()
    {
        // -----------------------------------------------------
        // STOP AT LAST KNOWN POSITION
        // -----------------------------------------------------

        Enemy.StopMoving();

        searchTimer = 0f;

        Debug.Log(
            $"[Search] {Enemy.name}: " +
            "Player lost. Searching..."
        );
    }


    public override void Tick()
    {
        // -----------------------------------------------------
        // PLAYER EXISTS?
        // -----------------------------------------------------

        if (Enemy.Player == null)
        {
            FinishSearch();
            return;
        }


        // -----------------------------------------------------
        // PLAYER FOUND AGAIN?
        // -----------------------------------------------------

        if (Enemy.IsPlayerDetected())
        {
            Debug.Log(
                $"[Search] {Enemy.name}: " +
                "Player detected again!"
            );

            Enemy.ChangeState(
                EnemyRange.EnemyState.Chase
            );

            return;
        }


        // -----------------------------------------------------
        // SEARCH TIMER
        // -----------------------------------------------------

        searchTimer += Time.deltaTime;


        if (searchTimer >= SearchDuration)
        {
            FinishSearch();
        }
    }


    private void FinishSearch()
    {
        Debug.Log(
            $"[Search] {Enemy.name}: " +
            "Search finished. Returning to Idle."
        );

        Enemy.ChangeState(
            EnemyRange.EnemyState.Idle
        );
    }


    public override void Exit()
    {
        Enemy.StopMoving();

        Debug.Log(
            $"[Search] {Enemy.name}: " +
            "Exited Search."
        );
    }
}
