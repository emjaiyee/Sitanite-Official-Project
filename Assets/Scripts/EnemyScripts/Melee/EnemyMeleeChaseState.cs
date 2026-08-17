using System.Collections.Generic;
using UnityEngine;

public class EnemyMeleeChaseState : EnemyMeleeState
{
    private float repathTimer;

    private const float RepathInterval = 0.15f;


    public EnemyMeleeChaseState(
        EnemyMelee enemy)
        : base(enemy)
    {
    }


    // =========================================================
    // ENTER
    // =========================================================

    public override void Enter()
    {
        repathTimer = 0f;

        Debug.Log(
            $"[Chase] {Enemy.name} started chasing."
        );

        CalculatePath();
    }


    // =========================================================
    // TICK
    // =========================================================

    public override void Tick()
    {
        // -----------------------------------------------------
        // PLAYER EXISTS?
        // -----------------------------------------------------

        if (Enemy.Player == null)
        {
            Enemy.StopMoving();
            return;
        }


        // -----------------------------------------------------
        // PLAYER STILL DETECTED?
        // -----------------------------------------------------

        if (!Enemy.IsPlayerDetected())
        {
            Debug.Log(
                $"[Chase] {Enemy.name}: " +
                "Lost the player. Entering Search."
            );

            Enemy.ChangeState(
                EnemyMelee.EnemyState.Search
            );

            return;
        }


        // -----------------------------------------------------
        // REPATH
        // -----------------------------------------------------

        repathTimer += Time.deltaTime;

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
    // CALCULATE PATH
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

            Debug.LogWarning(
                $"[Chase] {Enemy.name}: " +
                "Could not find path to player."
            );

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

        Debug.Log(
            $"[Chase] {Enemy.name} stopped chasing."
        );
    }
}