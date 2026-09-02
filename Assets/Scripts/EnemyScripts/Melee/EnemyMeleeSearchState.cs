using UnityEngine;

public class EnemyMeleeSearchState : EnemyMeleeState
{
    private float searchTimer;

    private const float SearchDuration = 3.5f;


    public EnemyMeleeSearchState(
        EnemyMelee enemy)
        : base(enemy)
    {
    }


    // =========================================================
    // ENTER
    // =========================================================

    public override void Enter()
    {
        // -----------------------------------------------------
        // STOP AT LAST KNOWN POSITION
        // -----------------------------------------------------

            if (!Enemy.IsOnStairLink)
                Enemy.StopMoving();

        searchTimer = 0f;

        Debug.Log(
            $"[Search] {Enemy.name}: " +
            "Player lost. Searching..."
        );
    }


    // =========================================================
    // TICK
    // =========================================================

    public override void Tick()
    {
        if (Enemy.IsOnStairLink && Enemy.HasPath)
        {
            Enemy.FollowCurrentPath();
            return;
        }

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
                EnemyMelee.EnemyState.Chase
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


    // =========================================================
    // FINISH SEARCH
    // =========================================================

    private void FinishSearch()
    {
        Debug.Log(
            $"[Search] {Enemy.name}: " +
            "Search finished. Returning to Idle."
        );

        Enemy.ChangeState(
            EnemyMelee.EnemyState.Idle
        );
    }


    // =========================================================
    // EXIT
    // =========================================================

    public override void Exit()
    {
        if (!Enemy.IsOnStairLink)
            Enemy.StopMoving();

        Debug.Log(
            $"[Search] {Enemy.name}: " +
            "Exited Search."
        );
    }
}