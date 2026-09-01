using System.Collections.Generic;
using UnityEngine;

public class EnemyRangeIdleState : EnemyRangeState
{
    private bool hasValidSpawnTile;
    private bool waitingForNewDestination;
    private float waitTimer;
    private const float WaitDuration = 0.5f;

    public EnemyRangeIdleState(EnemyRange enemy) : base(enemy)
    {
    }

    public override void Enter()
    {
        if (!Enemy.IsOnStairLink)
            Enemy.StopMoving();

        if (AStarManager.Instance == null)
        {
            hasValidSpawnTile = false;
            return;
        }

        hasValidSpawnTile =
            AStarManager.Instance.IsPositionWalkable(Enemy.transform.position) ||
            Enemy.IsOnStairLink;

        if (!hasValidSpawnTile)
            return;

        waitingForNewDestination = true;
        waitTimer = 0f;
    }

    public override void Tick()
    {
        if (Enemy.IsPlayerDetected())
        {
            Enemy.ChangeState(EnemyRange.EnemyState.Chase);
            return;
        }

        if (!hasValidSpawnTile)
            return;

        if (Enemy.HasPath)
        {
            Enemy.FollowCurrentPath();
            return;
        }

        if (waitingForNewDestination)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= WaitDuration)
            {
                waitingForNewDestination = false;
                ChooseNewDestination();
            }

            return;
        }

        waitingForNewDestination = true;
        waitTimer = 0f;
    }

    private void ChooseNewDestination()
    {
        if (AStarManager.Instance == null)
            return;

        Vector3? destination = AStarManager.Instance.GetRandomWalkablePositionNear(
            Enemy.SpawnPosition,
            Enemy.IdleWanderRadius);

        if (!destination.HasValue)
        {
            waitingForNewDestination = true;
            waitTimer = 0f;
            return;
        }

        List<Vector3> path = AStarManager.Instance.FindPath(
            Enemy.transform.position,
            destination.Value);

        if (path == null || path.Count == 0)
        {
            waitingForNewDestination = true;
            waitTimer = 0f;
            return;
        }

        if (!Enemy.SetPath(path))
        {
            waitingForNewDestination = true;
            waitTimer = 0f;
        }
    }
}