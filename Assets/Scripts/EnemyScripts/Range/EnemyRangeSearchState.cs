using System.Collections.Generic;
using UnityEngine;

public class EnemyRangeSearchState : EnemyRangeState
{
    private float searchTimer;
    private const float SearchDuration = 3.5f;

    public EnemyRangeSearchState(EnemyRange enemy) : base(enemy)
    {
    }

    public override void Enter()
    {
        if (Enemy.LastKnownPlayerPosition.HasValue &&
            AStarManager.Instance != null)
        {
            List<Vector3> path = AStarManager.Instance.FindPath(
                Enemy.transform.position,
                Enemy.LastKnownPlayerPosition.Value,
                Enemy.ElevationLevel
            );

            Enemy.SetPath(path);
        }
        else if (!Enemy.IsOnStairLink)
        {
            Enemy.StopMoving();
        }

        searchTimer = 0f;
    }

    public override void Tick()
    {
        if (Enemy.HasPath)
        {
            Enemy.FollowCurrentPath();
            return;
        }

        if (Enemy.Player == null)
        {
            FinishSearch();
            return;
        }

        if (Enemy.IsPlayerDetected())
        {
            Enemy.ChangeState(EnemyRange.EnemyState.Chase);
            return;
        }

        searchTimer += Time.deltaTime;
        if (searchTimer >= SearchDuration)
            FinishSearch();
    }

    private void FinishSearch()
    {
        Enemy.ChangeState(EnemyRange.EnemyState.Idle);
    }

    public override void Exit()
    {
        if (!Enemy.IsOnStairLink)
            Enemy.StopMoving();
    }
}