using System.Collections.Generic;
using UnityEngine;

public class EnemyRangeChaseState : EnemyRangeState
{
    private float repathTimer;

    private const float RepathInterval = 0.15f;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public EnemyRangeChaseState(
        EnemyRange enemy) : base(enemy)
    {
    }


    // =========================================================
    // ENTER
    // =========================================================

    public override void Enter()
    {
        repathTimer = 0f;

        CalculatePath();
    }


    // =========================================================
    // TICK
    // =========================================================

    public override void Tick()
    {
        // -----------------------------------------------------
        // NO PLAYER
        // -----------------------------------------------------

        if (Enemy.Player == null)
        {
            Enemy.StopMoving();
            return;
        }


        // -----------------------------------------------------
        // DETECTION
        // -----------------------------------------------------
        //
        // The player must remain inside the A* detection
        // radius while the enemy is chasing.
        //
        // If the player leaves detection range, stop chasing
        // and transition to Search.
        // -----------------------------------------------------

        if (!Enemy.IsPlayerDetected())
        {
            Enemy.StopMoving();

            Enemy.ChangeState(
                EnemyRange.EnemyState.Search
            );

            return;
        }


        // -----------------------------------------------------
        // ATTACK RANGE
        // -----------------------------------------------------
        //
        // IMPORTANT:
        //
        // Attack range is ONLY checked here, inside Chase.
        //
        // IsPlayerWithinAttackRange() uses the same
        // world-space calculation as EnemyMelee.
        // -----------------------------------------------------

        if (Enemy.IsPlayerWithinAttackRange())
        {
            Enemy.StopMoving();

            Enemy.TryShootProjectile();

            return;
        }


        // -----------------------------------------------------
        // REPATH
        // -----------------------------------------------------

        repathTimer +=
            Time.deltaTime;


        if (repathTimer >= RepathInterval)
        {
            repathTimer = 0f;

            CalculatePath();
        }


        // -----------------------------------------------------
        // FOLLOW PATH
        // -----------------------------------------------------

        Enemy.FollowCurrentPath();
    }


    // =========================================================
    // PATHFINDING
    // =========================================================

    private void CalculatePath()
    {
        if (AStarManager.Instance == null)
            return;


        if (Enemy.Player == null)
            return;


        List<Vector3> path =
            AStarManager.Instance.FindPath(
                Enemy.transform.position,
                Enemy.Player.position
            );


        if (path == null ||
            path.Count == 0)
        {
            Enemy.StopMoving();
            return;
        }


        Enemy.SetPath(path);
    }


    // =========================================================
    // EXIT
    // =========================================================

    public override void Exit()
    {
        Enemy.StopMoving();
    }
}
