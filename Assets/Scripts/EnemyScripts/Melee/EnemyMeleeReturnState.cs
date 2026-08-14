using UnityEngine;

public class EnemyMeleeReturnState : EnemyMeleeState
{
    public EnemyMeleeReturnState(
        EnemyMelee enemy)
        : base(enemy)
    {
    }


    public override void Enter()
    {
        Debug.Log(
            $"[EnemyMelee] {Enemy.name} entered Return state."
        );

        if (Enemy.Agent == null)
            return;

        Enemy.Agent.isStopped = false;

        if (Enemy.Agent.isOnNavMesh)
        {
            Enemy.Agent.SetDestination(
                Enemy.SpawnPosition
            );
        }
    }


    public override void Tick()
    {
        // -------------------------------------------------
        // PLAYER DETECTED
        // -------------------------------------------------

        if (Enemy.IsPlayerDetected())
        {
            Enemy.ChangeState(
                EnemyMelee.EnemyState.Chase
            );

            return;
        }


        // -------------------------------------------------
        // RETURN TO SPAWN
        // -------------------------------------------------

        float distanceToSpawn =
            Vector2.Distance(
                Enemy.transform.position,
                Enemy.SpawnPosition
            );


        if (
            distanceToSpawn <=
            Enemy.ReturnThreshold
        )
        {
            Enemy.StopMoving();

            Enemy.ChangeState(
                EnemyMelee.EnemyState.Idle
            );
        }
    }


    public override void Exit()
    {
        Enemy.StopMoving();
    }
}