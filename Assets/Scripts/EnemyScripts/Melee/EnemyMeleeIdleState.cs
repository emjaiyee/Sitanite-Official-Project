using System.Collections.Generic;
using UnityEngine;

public class EnemyMeleeIdleState : EnemyMeleeState
{
    private bool hasValidSpawnTile;

    private bool waitingForNewDestination;

    private float waitTimer;

    private const float WaitDuration = 0.5f;


    public EnemyMeleeIdleState(
        EnemyMelee enemy)
        : base(enemy)
    {
    }


    public override void Enter()
    {
        if (!Enemy.IsOnStairLink)
            Enemy.StopMoving();

        Debug.Log(
            $"[IDLE] {Enemy.name} ENTERED IDLE."
        );


        if (AStarManager.Instance == null)
        {
            Debug.LogError(
                $"[IDLE] {Enemy.name}: " +
                "AStarManager.Instance is NULL!"
            );

            hasValidSpawnTile = false;

            return;
        }


        hasValidSpawnTile =
            AStarManager.Instance.IsPositionWalkable(
                Enemy.transform.position) ||
            Enemy.IsOnStairLink;


        Debug.Log(
            $"[IDLE] {Enemy.name}: " +
            $"Spawn tile walkable = {hasValidSpawnTile}"
        );


        if (!hasValidSpawnTile)
        {
            Debug.LogError(
                $"[IDLE] {Enemy.name}: " +
                "Enemy is NOT standing on a walkable A* tile."
            );

            return;
        }


        waitingForNewDestination = true;
        waitTimer = 0f;
    }


    public override void Tick()
    {
        // =====================================================
        // PLAYER DETECTION
        // =====================================================

        if (Enemy.IsPlayerDetected())
        {
            Debug.Log(
                $"[IDLE] {Enemy.name}: " +
                "Player detected! Switching to Chase."
            );

            Enemy.ChangeState(
                EnemyMelee.EnemyState.Chase
            );

            return;
        }


        // =====================================================
        // VALID SPAWN TILE
        // =====================================================

        if (!hasValidSpawnTile)
            return;


        // =====================================================
        // FOLLOW EXISTING PATH
        // =====================================================

        if (Enemy.HasPath)
        {
            Enemy.FollowCurrentPath();

            return;
        }


        // =====================================================
        // WAIT BEFORE PICKING DESTINATION
        // =====================================================

        if (waitingForNewDestination)
        {
            waitTimer += Time.deltaTime;


            if (waitTimer >= WaitDuration)
            {
                waitingForNewDestination = false;

                Debug.Log(
                    $"[IDLE] {Enemy.name}: " +
                    "Choosing new destination..."
                );

                ChooseNewDestination();
            }

            return;
        }


        // =====================================================
        // PATH FINISHED
        // =====================================================

        waitingForNewDestination = true;
        waitTimer = 0f;


        Debug.Log(
            $"[IDLE] {Enemy.name}: " +
            "Reached destination."
        );
    }


    private void ChooseNewDestination()
    {
        // =====================================================
        // FIND RANDOM TILE
        // =====================================================

        Vector3? destination =
            AStarManager.Instance
                .GetRandomWalkablePositionNear(
                    Enemy.SpawnPosition,
                    Enemy.IdleWanderRadius
                );


        if (!destination.HasValue)
        {
            Debug.LogError(
                $"[IDLE] {Enemy.name}: " +
                "COULD NOT FIND A RANDOM WALKABLE DESTINATION!"
            );

            waitingForNewDestination = true;
            waitTimer = 0f;

            return;
        }


        Debug.Log(
            $"[IDLE] {Enemy.name}: " +
            $"Destination found at {destination.Value}"
        );


        // =====================================================
        // FIND A* PATH
        // =====================================================

        List<Vector3> path =
            AStarManager.Instance.FindPath(
                Enemy.transform.position,
                destination.Value
            );


        if (path == null)
        {
            Debug.LogError(
                $"[IDLE] {Enemy.name}: " +
                "A* RETURNED NULL PATH!"
            );

            waitingForNewDestination = true;
            waitTimer = 0f;

            return;
        }


        if (path.Count == 0)
        {
            Debug.LogWarning(
                $"[IDLE] {Enemy.name}: " +
                "A* returned an EMPTY path."
            );

            waitingForNewDestination = true;
            waitTimer = 0f;

            return;
        }


        Debug.Log(
            $"[IDLE] {Enemy.name}: " +
            $"A* path found! " +
            $"Length = {path.Count}"
        );


        // =====================================================
        // GIVE PATH TO ENEMY
        // =====================================================

        bool pathAccepted =
            Enemy.SetPath(path);


        if (!pathAccepted)
        {
            Debug.LogError(
                $"[IDLE] {Enemy.name}: " +
                "Enemy rejected the A* path!"
            );

            waitingForNewDestination = true;
            waitTimer = 0f;

            return;
        }


        Debug.Log(
            $"[IDLE] {Enemy.name}: " +
            "Started following path."
        );
    }


    public override void Exit()
    {
        Enemy.StopMoving();

        Debug.Log(
            $"[IDLE] {Enemy.name} EXITED IDLE."
        );
    }
}