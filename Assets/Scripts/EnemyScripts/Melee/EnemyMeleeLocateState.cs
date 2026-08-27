using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The enemy was damaged from somewhere it can't see.
/// It paths to the damage origin, lingers there briefly,
/// then gives up (Idle) or engages (Chase) if the player is detected.
/// </summary>
public class EnemyMeleeLocateState : EnemyMeleeState
{
    private float waitTimer;
    private bool waitingAtOrigin;

    public EnemyMeleeLocateState(EnemyMelee enemy)
        : base(enemy)
    {
    }

    // =========================================================
    // ENTER
    // =========================================================

    public override void Enter()
    {
        waitTimer = 0f;
        waitingAtOrigin = false;

        Debug.Log(
            $"[Locate] {Enemy.name}: investigating damage origin " +
            $"{Enemy.DamageSourcePosition}."
        );

        if (!Enemy.DamageSourcePosition.HasValue ||
            AStarManager.Instance == null)
        {
            // Nothing to investigate: fall back to Search behavior.
            Enemy.ChangeState(EnemyMelee.EnemyState.Search);
            return;
        }

        List<Vector3> path =
            AStarManager.Instance.FindPath(
                Enemy.transform.position,
                Enemy.DamageSourcePosition.Value
            );

        if (!Enemy.SetPath(path))
        {
            // Can't reach it: stand still and wait instead.
            waitingAtOrigin = true;
        }
    }

    // =========================================================
    // TICK
    // =========================================================

    public override void Tick()
    {
        if (Enemy.Player == null)
        {
            FinishLocate();
            return;
        }

        // Spotted the player on the way: engage.
        if (Enemy.IsPlayerDetected())
        {
            Debug.Log(
                $"[Locate] {Enemy.name}: player detected while locating!"
            );

            Enemy.ChangeState(EnemyMelee.EnemyState.Chase);
            return;
        }

        // Still traveling to the damage origin.
        if (!waitingAtOrigin)
        {
            if (Enemy.HasPath)
            {
                Enemy.FollowCurrentPath();
                return;
            }

            // Arrived: begin the linger.
            waitingAtOrigin = true;
            waitTimer = 0f;
        }

        // Linger at the origin, looking for the player.
        waitTimer += Time.deltaTime;

        if (waitTimer >= Enemy.LocateWaitDuration)
            FinishLocate();
    }

    // =========================================================
    // FINISH
    // =========================================================

    private void FinishLocate()
    {
        Debug.Log(
            $"[Locate] {Enemy.name}: nothing found. Returning to Idle."
        );

        Enemy.ChangeState(EnemyMelee.EnemyState.Idle);
    }

    // =========================================================
    // EXIT
    // =========================================================

    public override void Exit()
    {
        Enemy.StopMoving();
    }
}
