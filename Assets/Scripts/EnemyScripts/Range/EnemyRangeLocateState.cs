using System.Collections.Generic;
using UnityEngine;

public class EnemyRangeLocateState : EnemyRangeState
{
    private float waitTimer;
    private bool waitingAtOrigin;

    public EnemyRangeLocateState(EnemyRange enemy) : base(enemy)
    {
    }

    public override void Enter()
    {
        waitTimer = 0f;
        waitingAtOrigin = false;
        SetDestinationFromLatestDamage();
    }

    public void RefreshDestination()
    {
        waitTimer = 0f;
        waitingAtOrigin = false;
        SetDestinationFromLatestDamage();
    }

    public override void Tick()
    {
        if (Enemy.Player == null)
        {
            FinishLocate();
            return;
        }

        if (Enemy.IsPlayerDetected())
        {
            Enemy.ChangeState(EnemyRange.EnemyState.Chase);
            return;
        }

        if (!waitingAtOrigin)
        {
            if (Enemy.HasPath)
            {
                Enemy.FollowCurrentPath();
                return;
            }

            waitingAtOrigin = true;
            waitTimer = 0f;
        }

        waitTimer += Time.deltaTime;

        if (waitTimer >= Enemy.LocateWaitDuration)
            FinishLocate();
    }

    public override void Exit()
    {
        Enemy.StopMoving();
    }

    private void SetDestinationFromLatestDamage()
    {
        Enemy.StopMoving();

        if (!Enemy.DamageSourcePosition.HasValue ||
            AStarManager.Instance == null)
        {
            waitingAtOrigin = true;
            return;
        }

        List<Vector3> path = AStarManager.Instance.FindPath(
            Enemy.transform.position,
            Enemy.DamageSourcePosition.Value,
            Enemy.ElevationLevel
        );

        if (!Enemy.SetPath(path))
            waitingAtOrigin = true;
    }

    private void FinishLocate()
    {
        Enemy.ChangeState(EnemyRange.EnemyState.Idle);
    }
}