using System.Collections.Generic;
using UnityEngine;

public class EnemyRangeChaseState : EnemyRangeState
{
    private float repathTimer;
    private const float RepathInterval = 0.15f;

    public EnemyRangeChaseState(EnemyRange enemy) : base(enemy)
    {
    }

    public override void Enter()
    {
        repathTimer = 0f;
        CalculatePath();
    }

    public override void Tick()
    {
        if (Enemy.Player == null)
        {
            Enemy.StopMoving();
            return;
        }

        if (!Enemy.IsPlayerDetected())
        {
            Enemy.ChangeState(EnemyRange.EnemyState.Search);
            return;
        }

        if (Enemy.IsPlayerWithinAttackRange() && Enemy.IsPlayerRayCasted())
        {
            Enemy.StopMoving();
            if (Enemy.TryShootProjectile())
                return;

            return;
        }

        repathTimer += Time.deltaTime;
        if (repathTimer >= RepathInterval)
        {
            repathTimer = 0f;
            CalculatePath();
        }

        Enemy.FollowCurrentPath();
    }

    private void CalculatePath()
    {
        if (AStarManager.Instance == null || Enemy.Player == null)
            return;

        List<Vector3> path = AStarManager.Instance.FindPath(
            Enemy.transform.position,
            Enemy.Player.position);

        if (path == null || path.Count == 0)
        {
            Enemy.StopMoving();
            return;
        }

        Enemy.SetPath(path);
    }

    public override void Exit()
    {
        Enemy.StopMoving();
    }
}